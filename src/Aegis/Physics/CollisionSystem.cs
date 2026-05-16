using Microsoft.Xna.Framework;
using Aegis.Scene;

namespace Aegis.Physics;

/// <summary>
/// Grid hash espacial para aceleração de colisões em O(n) médio.
/// Sprint 4: substitui O(n²) no RebuildContactsAndFire para horde survivors.
/// Célula de 128x128 px — ajustar CellSize para objetos maiores.
/// </summary>
internal sealed class SpatialHash
{
    private const int CellSize = 128;
    private readonly Dictionary<long, List<Collider>> _cells = new();

    public void Clear() => _cells.Clear();

    private static long Key(int cx, int cy) => ((long)cx << 32) | (uint)cy;

    private static (int cx, int cy) Cell(float x, float y)
        => ((int)MathF.Floor(x / CellSize), (int)MathF.Floor(y / CellSize));

    public void Insert(Collider c)
    {
        var b = c.Bounds;
        var (x0, y0) = Cell(b.Left,  b.Top);
        var (x1, y1) = Cell(b.Right, b.Bottom);
        for (int cx = x0; cx <= x1; cx++)
        for (int cy = y0; cy <= y1; cy++)
        {
            var key = Key(cx, cy);
            if (!_cells.TryGetValue(key, out var list))
                _cells[key] = list = new List<Collider>(4);
            list.Add(c);
        }
    }

    /// <summary>Retorna candidatos próximos (pode incluir falso-positivos — filtrar com Overlaps).</summary>
    public IEnumerable<Collider> Query(Collider c)
    {
        var b = c.Bounds;
        var (x0, y0) = Cell(b.Left,  b.Top);
        var (x1, y1) = Cell(b.Right, b.Bottom);
        // HashSet evita duplicatas quando o collider cobre múltiplas células
        var seen = new HashSet<Collider>();
        for (int cx = x0; cx <= x1; cx++)
        for (int cy = y0; cy <= y1; cy++)
        {
            if (!_cells.TryGetValue(Key(cx, cy), out var list)) continue;
            foreach (var other in list)
                if (seen.Add(other)) yield return other;
        }
    }
}

/// <summary>
/// Física AABB simples e estável para jogos 2D hyper-casual/top-down/plataforma básica.
/// Regra principal:
/// - Collider sem Rigidbody = sólido estático.
/// - Collider com Rigidbody dinâmico = corpo móvel.
/// - Rigidbody kinematic = controlado por script; serve como sólido móvel simples, mas não recebe gravidade.
///
/// Esta versão evita o bug de "player preso no chão" removendo a resolução dinâmica-dinâmica
/// e usando resolução separada por eixo baseada na direção da velocidade.
/// </summary>
public sealed class CollisionSystem
{
    public static CollisionSystem Instance { get; } = new();
    private CollisionSystem() { }

    private readonly List<Collider> _colliders = new();
    public int ColliderCount => _colliders.Count;
    public IReadOnlyList<Collider> Colliders => _colliders;

    public void Register(Collider c)
    {
        if (c.Layer == 0 || c.Mask == 0)
            Aegis.Core.AegisLog.Warn("Collision", "Collider registrado com layer ou mask zero; colisões podem não ocorrer.");
        if (!_colliders.Contains(c)) _colliders.Add(c);
    }

    public void Unregister(Collider c) => _colliders.Remove(c);

    public void Reset() => _colliders.Clear();

    public void BeginContacts()
    {
        foreach (var c in _colliders)
        {
            c.PreviousContacts.Clear();
            foreach (var contact in c.CurrentContacts)
                c.PreviousContacts.Add(contact);
            c.CurrentContacts.Clear();
        }
    }

    // Spatial hash dedicado ao resolver — rebuild a cada eixo, contém só AABBs sólidos.
    private readonly SpatialHash _resolveHash = new();

    /// <summary>
    /// Reconstrói o hash de resolução com todos os AABBs e Slopes sólidos ativos.
    /// Chamado uma vez por eixo no PhysicsWorld.Step antes do loop de corpos.
    /// </summary>
    public void RebuildResolveHash()
    {
        _resolveHash.Clear();
        foreach (var c in _colliders)
            if (c.IsActive && !c.IsTrigger &&
                (c.Shape == ColliderShape.AABB || c.Shape == ColliderShape.Slope))
                _resolveHash.Insert(c);
    }

    /// <summary>
    /// Resolução de rampa (Slope) — executada APÓS IntegrateY + ResolveBodyAxis(Y).
    /// Para cada dinâmico, verifica se o ponto inferior central está abaixo da
    /// superfície da rampa e empurra o corpo para cima, ativando OnLand.
    ///
    /// Técnica: interpolação linear da superfície Y = f(X) da rampa.
    /// Suporta ângulos de 0° a ~60° sem tunelamento a 60fps.
    /// </summary>
    public void ResolveSlopes(IReadOnlyList<Rigidbody2D> bodies)
    {
        foreach (var rb in bodies)
        {
            if (rb.IsKinematic) return;

            Collider? self = null;
            foreach (var c in _colliders)
            {
                if (c.IsActive && c.Owner == rb.Owner && !c.IsTrigger &&
                    c.Shape == ColliderShape.AABB)
                { self = c; break; }
            }
            if (self is null) continue;

            var selfB = self.Bounds;
            // Ponto de teste: base central do corpo
            float footX = selfB.Left + selfB.Width * 0.5f;
            float footY = selfB.Bottom;

            foreach (var slope in _colliders)
            {
                if (!slope.IsActive || slope.IsTrigger || slope.Shape != ColliderShape.Slope) continue;
                if (!LayersMask(self, slope)) continue;

                var sb = slope.Bounds;

                // O corpo precisa estar horizontalmente sobre a rampa
                if (footX < sb.Left || footX > sb.Right) continue;

                // O corpo precisa estar dentro da AABB envolvente da rampa (±8px de tolerância)
                if (footY < sb.Top - 8f || footY > sb.Bottom + 8f) continue;

                float surfaceY = slope.GetSlopeSurfaceY(footX);
                if (surfaceY == float.MaxValue) continue;

                // O corpo está abaixo ou tocando a superfície?
                float penetration = footY - surfaceY;
                if (penetration < 0f) continue;   // acima da rampa — não resolver

                // Só resolver se o corpo estava chegando de cima (descendo ou parado)
                // Evita que o corpo seja empurrado ao pular de baixo para cima
                if (rb.VelocityY < -50f) continue;

                // Empurra o corpo para cima
                rb.Owner.Y -= penetration;
                rb.SyncPositionOnly();
                rb.OnLand();
            }
        }
    }

    /// <summary>
    /// Resolve colisões de um Rigidbody2D em um único eixo.
    ///
    /// Dinâmico-vs-estático / kinematic: corpo move-se totalmente.
    /// Dinâmico-vs-dinâmico (NOVO): split 50/50 — cada corpo recebe metade do overlap,
    /// resultado em separação posicional rígida sem tunelamento.
    /// Usa spatial hash para candidatos em O(n) médio.
    /// </summary>
    public void ResolveBodyAxis(Rigidbody2D rb, IReadOnlyList<Rigidbody2D> bodies, Axis axis)
    {
        if (rb.IsKinematic) return;

        for (int safety = 0; safety < 4; safety++)
        {
            bool moved = false;

            // Encontrar o collider AABB sólido deste rb
            Collider? self = null;
            foreach (var c in _colliders)
            {
                if (c.IsActive && c.Owner == rb.Owner && !c.IsTrigger && c.Shape == ColliderShape.AABB)
                { self = c; break; }
            }
            if (self is null) break;

            // Consulta do spatial hash — só candidatos próximos
            foreach (var other in _resolveHash.Query(self))
            {
                if (!other.IsActive || other == self || other.Owner == rb.Owner) continue;
                if (!LayersMask(self, other)) continue;
                if (other.IsTrigger || other.Shape != ColliderShape.AABB) continue;

                var otherBody = FindBody(bodies, other.Owner);
                bool otherIsDynamic = otherBody is not null && !otherBody.IsKinematic;

                var a = self.Bounds;
                var b = other.Bounds;
                if (!a.Intersects(b)) continue;

                float overlapX = MathF.Min(a.Right, b.Right) - MathF.Max(a.Left, b.Left);
                float overlapY = MathF.Min(a.Bottom, b.Bottom) - MathF.Max(a.Top, b.Top);
                if (overlapX <= 0f || overlapY <= 0f) continue;

                if (axis == Axis.X)
                {
                    if (other.IsOneWay) continue;

                    // Fator de split: 0.5 para dyn-vs-dyn (separa os dois), 1.0 para dyn-vs-static
                    float splitA = otherIsDynamic ? 0.5f : 1.0f;
                    float splitB = otherIsDynamic ? 0.5f : 0.0f;

                    float pushA, pushB, wallNormalA;
                    if (rb.VelocityX > 0f)
                    {
                        pushA = -overlapX * splitA; wallNormalA = -1f;
                        pushB =  overlapX * splitB;
                    }
                    else if (rb.VelocityX < 0f)
                    {
                        pushA =  overlapX * splitA; wallNormalA =  1f;
                        pushB = -overlapX * splitB;
                    }
                    else
                    {
                        float acx = a.Left + a.Width * 0.5f;
                        float bcx = b.Left + b.Width * 0.5f;
                        if (acx < bcx) { pushA = -overlapX * splitA; wallNormalA = -1f; pushB =  overlapX * splitB; }
                        else           { pushA =  overlapX * splitA; wallNormalA =  1f; pushB = -overlapX * splitB; }
                    }

                    rb.Owner.X += pushA;
                    rb.OnHitWall(wallNormalA);

                    if (otherIsDynamic && otherBody is not null)
                    {
                        otherBody.Owner.X += pushB;
                        otherBody.OnHitWall(-wallNormalA);
                        otherBody.SyncPositionOnly();
                        // Troca de velocidade horizontal (colisão elástica amortecida)
                        float tmp = rb.VelocityX;
                        rb.VelocityX       = otherBody.VelocityX * 0.5f;
                        otherBody.VelocityX = tmp * 0.5f;
                    }

                    moved = true;
                }
                else // Axis.Y
                {
                    if (other.IsOneWay)
                    {
                        if (rb.VelocityY < 0f) continue;
                        float bodyPrevBottom = rb.Owner.Y + self.Height - (rb.VelocityY * (1f / 60f)) * 2f;
                        if (bodyPrevBottom > b.Top + 4f) continue;
                    }

                    // Dyn-vs-dyn vertical: só separa, sem troca de velocidade vertical
                    // (evita comportamentos estranhos com gravidade compartilhada)
                    float splitA = otherIsDynamic ? 0.5f : 1.0f;
                    float splitB = otherIsDynamic ? 0.5f : 0.0f;

                    if (rb.VelocityY > 0f)
                    {
                        rb.Owner.Y -= overlapY * splitA;
                        rb.OnLand();
                        if (otherIsDynamic && otherBody is not null)
                        { otherBody.Owner.Y += overlapY * splitB; otherBody.SyncPositionOnly(); }
                    }
                    else if (rb.VelocityY < 0f)
                    {
                        rb.Owner.Y += overlapY * splitA;
                        rb.OnHitCeiling();
                        if (otherIsDynamic && otherBody is not null)
                        { otherBody.Owner.Y -= overlapY * splitB; otherBody.SyncPositionOnly(); }
                    }
                    else
                    {
                        float acy = a.Top + a.Height * 0.5f;
                        float bcy = b.Top + b.Height * 0.5f;
                        if (acy < bcy) { rb.Owner.Y -= overlapY * splitA; rb.OnLand(); }
                        else           { rb.Owner.Y += overlapY * splitA; rb.OnHitCeiling(); }
                    }

                    moved = true;
                }

                rb.SyncPositionOnly();
            }

            if (!moved) break;
        }
    }

    private readonly SpatialHash _spatialHash = new();

    public void RebuildContactsAndFire()
    {
        foreach (var c in _colliders)
            c.CurrentContacts.Clear();

        // Sprint 4: spatial hash para O(n) médio em vez de O(n²)
        _spatialHash.Clear();
        foreach (var c in _colliders)
            if (c.IsActive) _spatialHash.Insert(c);

        // Índice O(1) para evitar IndexOf O(n) dentro do loop
        var idxMap = new Dictionary<Collider, int>(_colliders.Count);
        for (int i = 0; i < _colliders.Count; i++) idxMap[_colliders[i]] = i;

        for (int i = 0; i < _colliders.Count; i++)
        {
            var a = _colliders[i];
            if (!a.IsActive) continue;

            foreach (var b in _spatialHash.Query(a))
            {
                if (!b.IsActive) continue;
                if (b == a) continue;
                // Processar cada par só uma vez (b deve ter índice > a)
                if (!idxMap.TryGetValue(b, out int bi) || bi <= i) continue;
                if (!LayersMask(a, b)) continue;
                if (!a.Overlaps(b)) continue;
                a.CurrentContacts.Add(b);
                b.CurrentContacts.Add(a);
            }
        }

        foreach (var a in _colliders)
        {
            foreach (var b in a.CurrentContacts)
            {
                if (a.PreviousContacts.Contains(b)) a.OnCollideStay?.Invoke(a, b);
                else a.OnCollideEnter?.Invoke(a, b);
            }

            foreach (var b in a.PreviousContacts)
            {
                if (!a.CurrentContacts.Contains(b)) a.OnCollideExit?.Invoke(a, b);
            }
        }
    }

    // Compatibilidade com versões antigas do PhysicsWorld.
    public void Resolve(List<Rigidbody2D> bodies, Axis axis)
    {
        foreach (var rb in bodies.ToArray())
            ResolveBodyAxis(rb, bodies, axis);
        if (axis == Axis.Y) RebuildContactsAndFire();
    }

    private static bool LayersMask(Collider a, Collider b)
        => (a.Layer & b.Mask) != 0 && (b.Layer & a.Mask) != 0;

    private static Rigidbody2D? FindBody(IReadOnlyList<Rigidbody2D> bodies, Object2D owner)
    {
        foreach (var rb in bodies)
            if (rb.Owner == owner) return rb;
        return null;
    }

    /// <summary>Sprint final: raycast AABB simples no eixo da direção.
    /// Retorna o primeiro collider sólido (não-trigger) que intersecta o segmento,
    /// ou null se nenhum for encontrado. Usado pelo audio spatial para oclusão.</summary>
    /// <summary>Raycast AABB preciso com slab test. Retorna o hit mais próximo ou null.</summary>
    public RaycastHit? Raycast(Vector2 origin, Vector2 direction, float length, int layerMask = ~0)
    {
        if (direction == Vector2.Zero) return null;
        var dir = Vector2.Normalize(direction);
        RaycastHit? closest = null;

        foreach (var c in _colliders)
        {
            if (!c.IsActive) continue;
            if ((c.Layer & layerMask) == 0) continue;
            if (!RayVsRect(origin, dir, length, c.Bounds, out float t, out var normal)) continue;
            if (closest is null || t < closest.Value.Distance)
                closest = new RaycastHit(c, origin + dir * t, normal, t);
        }
        return closest;
    }

    /// <summary>Sprint final: GetFirstCollider — retorna o primeiro AABB sólido de um Object2D.</summary>
    public Collider? GetFirstCollider(Object2D owner)
    {
        foreach (var c in _colliders)
            if (c.Owner == owner && c.IsActive && !c.IsTrigger && c.Shape == ColliderShape.AABB)
                return c;
        return null;
    }

    private static bool RayVsRect(Vector2 ro, Vector2 rd, float maxLength,
                                   RectangleF r, out float tHit, out Vector2 normal)
    {
        tHit   = 0f;
        normal = Vector2.Zero;

        float invX = rd.X == 0f ? float.MaxValue : 1f / rd.X;
        float invY = rd.Y == 0f ? float.MaxValue : 1f / rd.Y;

        float tx1 = (r.Left   - ro.X) * invX;
        float tx2 = (r.Right  - ro.X) * invX;
        float ty1 = (r.Top    - ro.Y) * invY;
        float ty2 = (r.Bottom - ro.Y) * invY;

        float tMinX = MathF.Min(tx1, tx2), tMaxX = MathF.Max(tx1, tx2);
        float tMinY = MathF.Min(ty1, ty2), tMaxY = MathF.Max(ty1, ty2);

        float tEnter = MathF.Max(tMinX, tMinY);
        float tExit  = MathF.Min(tMaxX, tMaxY);

        if (tExit < 0f || tEnter > tExit || tEnter > maxLength) return false;

        tHit = tEnter < 0f ? tExit : tEnter;
        if (tHit < 0f || tHit > maxLength) return false;

        normal = tMinX > tMinY
            ? new Vector2(-MathF.Sign(rd.X), 0f)
            : new Vector2(0f, -MathF.Sign(rd.Y));
        return true;
    }
}

public readonly struct RaycastHit
{
    public readonly Collider Collider;
    public readonly Vector2 Point;
    public readonly Vector2 Normal;
    public readonly float Distance;

    public RaycastHit(Collider collider, Vector2 point, Vector2 normal, float distance)
    {
        Collider = collider;
        Point = point;
        Normal = normal;
        Distance = distance;
    }
}
