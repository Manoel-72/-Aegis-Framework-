using Aegis.Scene;
using Microsoft.Xna.Framework;

namespace Aegis.Physics;

/// <summary>
/// Corpo dinâmico 2D com integração simplética por eixo.
///
/// Coordenadas seguem a convenção MonoGame/tela: Y cresce para baixo.
///
/// Fluxo por frame (orquestrado pelo <see cref="PhysicsWorld"/>):
/// <list type="number">
///   <item><description><see cref="SyncFromOwner"/>  — capta teleports externos</description></item>
///   <item><description><see cref="BeginFrame"/>     — decrementa coyote-timer</description></item>
///   <item><description><see cref="IntegrateX"/>     — move Owner no eixo X</description></item>
///   <item><description><see cref="IntegrateY"/>     — aplica gravidade e move Owner no eixo Y</description></item>
///   <item><description><see cref="OnHitWall"/> / <see cref="OnLand"/> / <see cref="OnHitCeiling"/>
///         — chamados pelo CollisionSystem após resolução</description></item>
/// </list>
///
/// Coyote Time:
///   <see cref="IsGrounded"/> permanece <c>true</c> por <see cref="CoyoteFrames"/> frames
///   após o body deixar o chão, permitindo pulo tardio natural de plataformer.
///
/// Max Fall Speed:
///   <see cref="MaxFallSpeed"/> limita a velocidade vertical máxima para evitar
///   tunnel através de plataformas finas em frames de baixo FPS.
/// </summary>
public sealed class Rigidbody2D
{
    // ── Referência ao objeto visual ────────────────────────────────────────────
    /// <summary>O Object2D controlado por este Rigidbody.</summary>
    public Object2D Owner { get; }

    // ── Velocidade ────────────────────────────────────────────────────────────
    /// <summary>Velocidade horizontal em pixels/segundo.</summary>
    public float VelocityX { get; set; }

    /// <summary>Velocidade vertical em pixels/segundo. Y positivo = para baixo.</summary>
    public float VelocityY { get; set; }

    // ── Parâmetros físicos ────────────────────────────────────────────────────
    /// <summary>Multiplicador de gravidade por instância. Padrão: 1.0.</summary>
    public float GravityScale { get; set; } = 1f;

    /// <summary>
    /// Atrito horizontal no apoio (0 = desligado). Usa decaimento exponencial por
    /// segundo: <c>vx *= exp(-GroundFriction * dt)</c> enquanto <see cref="_touchingGround"/>.
    /// Valores típicos: 8–24 para paragem natural em plataforma.
    /// </summary>
    public float GroundFriction { get; set; }

    /// <summary>Velocidade máxima de queda em pixels/segundo. Padrão: 1200.</summary>
    public float MaxFallSpeed { get; set; } = 1200f;

    /// <summary>
    /// Frames de graça após deixar o chão onde <see cref="IsGrounded"/> ainda
    /// retorna <c>true</c>, permitindo pulo tardio (coyote time). Padrão: 5.
    /// </summary>
    public int CoyoteFrames { get; set; } = 5;

    /// <summary>
    /// Quando <c>true</c>, o body não é afetado por gravidade nem por resolução
    /// de colisão — apenas detecta overlaps. Útil para plataformas móveis
    /// controladas por script.
    /// </summary>
    public bool IsKinematic { get; set; } = false;

    // ── Gravidade global ──────────────────────────────────────────────────────
    /// <summary>
    /// Aceleração gravitacional global em pixels/s². Positivo = para baixo.
    /// Padrão: 800. Afeta todos os Rigidbodies com <see cref="GravityScale"/> > 0.
    /// </summary>
    public static float Gravity { get; set; } = 800f;

    // ── Estado de chão ────────────────────────────────────────────────────────
    /// <summary>
    /// <c>true</c> se o body apoia o chão neste passo de física, ou ainda
    /// dentro da janela de coyote após deixar uma borda. O contato de apoio
    /// evita depender do timer em todo frame (coyote só pós-ledge).
    /// </summary>
    public bool IsGrounded => _touchingGround || _coyoteTimer > 0;

    // ── Posição interna sincronizada ──────────────────────────────────────────
    // Espelha Owner.X/Y após cada passo de integração para que o CollisionSystem
    // possa ler a posição sem precisar navegar pela hierarquia da cena.
    internal float X;
    internal float Y;

    // ── Estado privado ────────────────────────────────────────────────────────
    private int _coyoteTimer;  // janela após deixar o chão; pulo ainda permitido
    private bool _touchingGround; // contato inferior neste passo (repouso estável)
    private int _wallSideThisFrame; // -1 = parede esquerda, +1 = direita, 0 = nenhuma

    /// <summary>true se tocando uma parede lateral neste frame. Útil para wall jump/slide.</summary>
    public bool TouchingWall => _wallSideThisFrame != 0;

    /// <summary>Lado da parede: -1 = à esquerda, +1 = à direita, 0 = nenhuma.</summary>
    public int WallSide => _wallSideThisFrame;

    // ── Construtor ────────────────────────────────────────────────────────────
    public Rigidbody2D(Object2D owner)
    {
        Owner = owner;
        X     = owner.X;
        Y     = owner.Y;
    }

    // ── Sincronização ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sincroniza a posição interna com o Owner.
    /// Chamado pelo PhysicsWorld no início de cada frame para captar teleports
    /// feitos externamente (ex.: <c>aegis.setPosition</c> via Lua).
    /// Também reseta <see cref="VelocityY"/> quando detecta um deslocamento Y
    /// externo significativo, prevenindo velocidade acumulada após teleport.
    /// </summary>
    internal void SyncFromOwner()
    {
        const float TeleportThreshold = 4f;

        // Detecta teleport vertical e reseta velocidade para evitar bouncing
        if (MathF.Abs(Owner.Y - Y) > TeleportThreshold)
            VelocityY = 0f;

        X = Owner.X;
        Y = Owner.Y;
    }

    // ── Ciclo de frame ────────────────────────────────────────────────────────

    /// <summary>
    /// Chamado pelo PhysicsWorld antes de IntegrateX.
    /// O coyote só gasta fora de apoio; em repouso a perda diária (antes o timer
    /// zerava mesmo de pé) era evitada por <see cref="OnLand"/> a cada frame —
    /// mas se <c>OnLand</c> faltava (epsilon de penetração) o jogador perdia
    /// o chão. Usa-se <c>_touchingGround</c> e zera o flag antes de novo passo.
    /// </summary>
    internal void BeginFrame()
    {
        var hadSupport = _touchingGround;
        if (!hadSupport && _coyoteTimer > 0)
            _coyoteTimer--;
        _touchingGround    = false; // o passo de física a seguir pode pôr de novo
        _wallSideThisFrame = 0;     // Sprint 3: resetar side a cada passo
    }

    // ── Integração por eixo ───────────────────────────────────────────────────

    /// <summary>
    /// Passo 1 — move o Owner apenas no eixo X. Sem gravidade.
    /// </summary>
    internal void IntegrateX(float dt)
    {
        if (IsKinematic) return;

        Owner.X += VelocityX * dt;
        X = Owner.X;
    }

    /// <summary>
    /// Passo 2 — aplica gravidade e move o Owner no eixo Y.
    /// Sincroniza X antes de mover Y para refletir possíveis ajustes
    /// feitos pelo CollisionSystem durante a resolução do Passo 1.
    /// </summary>
    internal void IntegrateY(float dt)
    {
        if (IsKinematic) return;

        // Capta correções de X feitas pela resolução de colisão horizontal
        X = Owner.X;

        // Integração simplética: velocidade é atualizada antes da posição
        VelocityY  = MathF.Min(VelocityY + Gravity * GravityScale * dt, MaxFallSpeed);
        Owner.Y   += VelocityY * dt;
        Y          = Owner.Y;
    }

    // ── Callbacks de colisão (chamados pelo CollisionSystem) ──────────────────

    /// <summary>
    /// Notifica colisão horizontal. Cancela VelocityX somente se o body
    /// se movia na direção da parede.
    /// </summary>
    /// <param name="normalX">
    /// Sinal da normal de contato no eixo X:
    /// +1 = parede à esquerda do body, -1 = parede à direita.
    /// </param>
    internal void OnHitWall(float normalX)
    {
        if (normalX > 0f && VelocityX < 0f) VelocityX = 0f;
        if (normalX < 0f && VelocityX > 0f) VelocityX = 0f;

        // Sprint 3: registrar lado da parede para isTouchingWall / wallSide
        _wallSideThisFrame = normalX > 0f ? -1 : 1;

        // Sincroniza posição pós-resolução
        X = Owner.X;
        Y = Owner.Y;
    }

    /// <summary>
    /// Notifica colisão com o chão. Cancela VelocityY e ativa coyote-timer.
    /// </summary>
    internal void OnLand()
    {
        if (VelocityY > 0f)
            VelocityY = 0f;

        _touchingGround = true;
        _coyoteTimer    = CoyoteFrames;

        X = Owner.X;
        Y = Owner.Y;
    }

    /// <summary>
    /// Notifica colisão com o teto. Cancela VelocityY negativo (para cima).
    /// </summary>
    internal void OnHitCeiling()
    {
        if (VelocityY < 0f)
            VelocityY = 0f;

        X = Owner.X;
        Y = Owner.Y;
    }

    // ── API de impulso ────────────────────────────────────────────────────────
    internal void SyncPositionOnly()
    {
        X = Owner.X;
        Y = Owner.Y;
    }


    /// <summary>
    /// Aplica um impulso vertical instantâneo. Use para pulos.
    /// Valores negativos movem para cima (convenção MonoGame: Y cresce para baixo).
    /// </summary>
    public void ApplyImpulseY(float vy)
        => VelocityY = Sanitize(vy);

    /// <summary>Resets velocity and coyote state — útil ao respawnar o player.</summary>
    public void ResetState()
    {
        VelocityX       = 0f;
        VelocityY       = 0f;
        _coyoteTimer    = 0;
        _touchingGround = false;
    }

    private const float MaxSpeed = 8000f;

    private static float Sanitize(float v, float maxAbs = MaxSpeed)
    {
        if (!float.IsFinite(v)) return 0f;
        if (v >  maxAbs) return  maxAbs;
        if (v < -maxAbs) return -maxAbs;
        return v;
    }

    internal void SanitizeVelocities()
    {
        VelocityX = Sanitize(VelocityX);
        VelocityY = Sanitize(VelocityY);
    }

    /// <summary>Chamado no fim do passo de física; só com contacto sólido no chão.</summary>
    internal void ApplyGroundFriction(float dt)
    {
        if (IsKinematic || !_touchingGround || GroundFriction <= 0f) return;
        if (MathF.Abs(VelocityX) < 1e-4f)
        {
            VelocityX = 0f;
            return;
        }

        VelocityX *= MathF.Exp(-GroundFriction * dt);
    }
}
