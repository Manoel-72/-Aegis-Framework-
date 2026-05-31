using Aegis.Core;
using Aegis.Physics;
using Aegis.Scene;
using Microsoft.Xna.Framework;
using NLua;

namespace Aegis.Scripting;

public sealed partial class LuaRuntime
{
    /// aegis.addCollider(obj, w, h [, offX, offY]) → collider
    public Collider AddCollider(Object2D obj, float w, float h,
                                float offX = 0f, float offY = 0f)
    {
        Require(obj, nameof(AddCollider));
        var safeW = MathF.Max(0.001f, RequireFinite(w, nameof(w), 1f));
        var safeH = MathF.Max(0.001f, RequireFinite(h, nameof(h), 1f));
        var c = new Collider(obj, safeW, safeH,
            RequireFinite(offX, nameof(offX)),
            RequireFinite(offY, nameof(offY)));
        CollisionSystem.Instance.Register(c);
        _colliders[++_colliderIdSeq] = c;
        return c;
    }

    /// aegis.addCircleCollider(obj, radius [, offX, offY]) -> collider
    public Collider AddCircleCollider(Object2D obj, float radius,
                                      float offX = 0f, float offY = 0f)
    {
        Require(obj, nameof(AddCircleCollider));
        var r = MathF.Max(0.001f, RequireFinite(radius, nameof(radius), 1f));
        var c = new Collider(obj, r * 2f, r * 2f,
            RequireFinite(offX, nameof(offX)),
            RequireFinite(offY, nameof(offY)))
        {
            Shape = ColliderShape.Circle,
            Radius = r
        };
        CollisionSystem.Instance.Register(c);
        _colliders[++_colliderIdSeq] = c;
        return c;
    }

    /// <summary>aegis.addSlopeCollider(obj, w, h, dir [, offX, offY]) → collider
    /// dir: "right" = rampa sobe esq→dir | "left" = rampa sobe dir→esq.
    /// O collider deve ser num objeto estático (sem Rigidbody).
    ///
    /// Exemplo de tile de rampa 32×32:
    ///   local col = aegis.addSlopeCollider(tile, 32, 32, "right")
    ///   aegis.setColliderLayer(col, "WORLD")
    ///   aegis.setColliderMask(col, "PLAYER|ENEMY")
    /// </summary>
    public Collider AddSlopeCollider(Object2D obj, float w, float h,
                                     string dir = "right",
                                     float offX = 0f, float offY = 0f)
    {
        Require(obj, nameof(AddSlopeCollider));
        var c = new Collider(obj, w, h, offX, offY)
        {
            Shape          = ColliderShape.Slope,
            SlopeDirection = dir == "left" ? SlopeDir.Left : SlopeDir.Right,
            Layer          = 1,
            Mask           = ~0,
        };
        CollisionSystem.Instance.Register(c);
        _colliders[++_colliderIdSeq] = c;
        // Aviso: Slope num objeto com Rigidbody não faz sentido
        if (PhysicsWorld.Instance.Bodies.Any(rb => rb.Owner == obj))
            AegisLog.Warn("Slope", "addSlopeCollider num objeto com Rigidbody — slope deve ser estático.");
        return c;
    }

    /// <summary>aegis.setSlopeDir(collider, "left"|"right") — muda direção do slope em runtime.</summary>
    public void SetSlopeDir(Collider c, string dir)
        => c.SlopeDirection = dir == "left" ? SlopeDir.Left : SlopeDir.Right;

    public void RemoveCollider(Collider c)
    {
        CollisionSystem.Instance.Unregister(c);
        var key = _colliders.FirstOrDefault(kv => kv.Value == c).Key;
        if (key != 0) _colliders.Remove(key);
    }

    /// Layers pré-definidas acessíveis por string no Lua
    private static int ParseLayer(string name) => name.ToUpper() switch
    {
        "WORLD"  => 1,
        "PLAYER" => 2,
        "ENEMY"  => 4,
        "BULLET" => 8,
        "PICKUP" => 16,
        "UI"     => 32,
        _        => 1
    };

    /// aegis.setColliderLayer(c, "PLAYER")  ou  aegis.setColliderLayer(c, 2)
    public void SetColliderLayer(Collider c, object layer)
    {
        var value = layer is string s ? ParseLayer(s) : Convert.ToInt32(layer);
        if (value == 0) AegisLog.Warn("Collision", "Collider layer ficou 0; ele pode não colidir com nada.");
        c.Layer = value;
    }

    /// aegis.setColliderMask(c, "WORLD|ENEMY")  ou numérico
    public void SetColliderMask(Collider c, object mask)
    {
        if (mask is string s)
        {
            int m = 0;
            foreach (var part in s.Split('|'))
                m |= ParseLayer(part.Trim());
            c.Mask = m;
        }
        else c.Mask = Convert.ToInt32(mask);
        if (c.Mask == 0) AegisLog.Warn("Collision", "Collider mask ficou 0; ele não detectará colisões.");
    }

    public void SetTrigger(Collider c, bool v)  => c.IsTrigger = v;

    /// <summary>aegis.setOneWay(collider, bool) — marca collider como plataforma one-way.
    /// O corpo pode pular de baixo para cima atravessando-a; pousa normalmente ao cair.
    /// Sprint 4 (do plano do MD).</summary>
    public void SetOneWay(Collider c, bool v)   => c.IsOneWay = v;
    public void SetColliderOffset(Collider c, float ox, float oy)   { c.OffsetX = ox; c.OffsetY = oy; }

    /// Retorna o Collider anexado ao Object2D (busca por Owner)
    public Collider? GetColliderOf(Object2D obj)
    {
        foreach (var c in _colliders.Values)
            if (c.Owner == obj) return c;
        return null;
    }

    // ── Callbacks de colisão ──────────────────────────────────────────

    /// aegis.onCollide(collider, function(a, b) ... end)
    /// → dispara tanto em enter quanto em stay
    public void OnCollide(Collider c, LuaFunction cb)
    {
        c.OnCollideEnter = (a, b) => cb.Call(a, b);
        c.OnCollideStay  = (a, b) => cb.Call(a, b);
    }

    /// aegis.onCollideEnter(collider, function(a, b) ... end)
    public void OnCollideEnter(Collider c, LuaFunction cb)
        => c.OnCollideEnter = (a, b) => cb.Call(a, b);

    /// aegis.onCollideExit(collider, function(a, b) ... end)
    public void OnCollideExit(Collider c, LuaFunction cb)
        => c.OnCollideExit = (a, b) => cb.Call(a, b);

    // ════════════════════════════════════════════════════════════════
    //  v0.4 — Rigidbody2D API
    // ════════════════════════════════════════════════════════════════

    private readonly Dictionary<Object2D, Rigidbody2D> _rigidbodies = new();

    /// aegis.addRigidbody(obj) → rigidbody
    public Rigidbody2D AddRigidbody(Object2D obj)
    {
        if (_rigidbodies.TryGetValue(obj, out var existing)) return existing;

        // BUG #2: Avisar quando CircleCollider é combinado com Rigidbody.
        // A resolução de colisão sólida (ResolveBodyAxis) é AABB-only;
        // um corpo circular com Rigidbody atravessará paredes silenciosamente.
        var circleCol = _colliders.Values.FirstOrDefault(
            c => c.Owner == obj && c.Shape == ColliderShape.Circle);
        if (circleCol != null)
            AegisLog.Warn("Physics",
                "Rigidbody adicionado em objeto com CircleCollider. " +
                "Resolução de colisão sólida não suporta círculo — " +
                "use AABB para corpos físicos sólidos. " +
                "CircleCollider funciona apenas como trigger/detecção.");

        var rb = new Rigidbody2D(obj);
        PhysicsWorld.Instance.AddBody(rb);
        _rigidbodies[obj] = rb;
        return rb;
    }

    public void RemoveRigidbody(Object2D obj)
    {
        if (_rigidbodies.TryGetValue(obj, out var rb))
        {
            PhysicsWorld.Instance.RemoveBody(rb);
            _rigidbodies.Remove(obj);
        }
    }

    private const float MaxVel = 8000f;

    private static float ClampVel(float v)
    {
        if (!float.IsFinite(v)) return 0f;
        if (v >  MaxVel) return  MaxVel;
        if (v < -MaxVel) return -MaxVel;
        return v;
    }

    /// aegis.setVelocity(rb, vx, vy)
    public void SetVelocity(Rigidbody2D rb, float vx, float vy)
    {
        rb.VelocityX = ClampVel(vx);
        rb.VelocityY = ClampVel(vy);
    }

    /// aegis.setVelocityX(rb, vx) — só horizontal, não toca VelocityY (gravidade fica intacta)
    public void SetVelocityX(Rigidbody2D rb, float vx)
        => rb.VelocityX = ClampVel(vx);

    /// aegis.addImpulseY(rb, vy) — impulso vertical (pulo). Substitui setVelocity para pulos.
    public void AddImpulseY(Rigidbody2D rb, float vy)
        => rb.ApplyImpulseY(ClampVel(vy));

    /// Compat roadmap: setVel(rb, vx, vy) sem matar Y em voo quando vy=0.
    public void SetVel(Rigidbody2D rb, float vx, float vy)
    {
        rb.VelocityX = ClampVel(vx);
        if (MathF.Abs(vy) < 1e-5f && !rb.IsGrounded) return;
        rb.VelocityY = ClampVel(vy);
    }

    /// Alias retrocompat.
    public void SetVelX(Rigidbody2D rb, float vx) => SetVelocityX(rb, vx);

    /// Alias retrocompat (pulo por impulso).
    public void JumpY(Rigidbody2D rb, float vy) => AddImpulseY(rb, vy);

    /// aegis.addVelocity(rb, dvx, dvy)
    public void AddVelocity(Rigidbody2D rb, float dvx, float dvy)
    {
        rb.VelocityX = ClampVel(rb.VelocityX + dvx);
        rb.VelocityY = ClampVel(rb.VelocityY + dvy);
    }

    public float GetVelocityX(Rigidbody2D rb) => rb.VelocityX;
    public float GetVelocityY(Rigidbody2D rb) => rb.VelocityY;

    /// aegis.setGravity(rb, scale)  — multiplicador por instância
    public void SetGravity(Rigidbody2D rb, float scale) => rb.GravityScale = scale;

    /// aegis.setGroundFriction(rb, k)  — 0=off; ~12–20 = paragem suave no chão
    public void SetGroundFriction(Rigidbody2D rb, float k)
    {
        if (!float.IsFinite(k) || k < 0f) k = 0f;
        rb.GroundFriction = MathF.Min(k, 200f);
    }

    /// aegis.setGlobalGravity(900)  — altera gravidade global (padrão 800)
    public void SetGlobalGravity(float g)
    {
        if (!float.IsFinite(g) || g < 0f) g = 800f;
        g = MathF.Min(g, 20_000f);
        Rigidbody2D.Gravity = g;
    }

    public void SetKinematic(Rigidbody2D rb, bool v) => rb.IsKinematic = v;

    /// aegis.isGrounded(rb) → bool
    public bool IsGrounded(Rigidbody2D rb) => rb.IsGrounded;

    // ════════════════════════════════════════════════════════════════
    //  v0.4 — Raycast API
    // ════════════════════════════════════════════════════════════════

    /// aegis.raycast(ox, oy, dx, dy, length) → {hit, x, y, nx, ny, dist} ou nil
    public LuaTable? DoRaycast(float ox, float oy, float dx, float dy, float len)
        => BuildRayResult(CollisionSystem.Instance.Raycast(
               new Vector2(ox, oy), new Vector2(dx, dy), len));

    /// aegis.raycastMask(ox, oy, dx, dy, length, mask)
    public LuaTable? DoRaycastMask(float ox, float oy, float dx, float dy,
                                    float len, object mask)
    {
        int m = mask is string s ? ParseMaskString(s) : Convert.ToInt32(mask);
        return BuildRayResult(CollisionSystem.Instance.Raycast(
               new Vector2(ox, oy), new Vector2(dx, dy), len, m));
    }

    /// aegis.lineOfSight(ax, ay, bx, by [, mask]) → bool
    /// Retorna true se NÃO houver obstáculo entre A e B.
    public bool LineOfSight(float ax, float ay, float bx, float by,
                             object? mask = null)
    {
        int m = mask is string s ? ParseMaskString(s)
              : mask is not null  ? Convert.ToInt32(mask)
              : ~0;
        var dir = new Vector2(bx - ax, by - ay);
        float len = dir.Length();
        if (len < 0.001f) return true;
        var hit = CollisionSystem.Instance.Raycast(
            new Vector2(ax, ay), dir / len, len, m);
        return hit is null;
    }

    // ── Helpers Raycast ───────────────────────────────────────────────
    private static int ParseMaskString(string s)
    {
        int m = 0;
        foreach (var part in s.Split('|'))
            m |= part.Trim().ToUpper() switch
            {
                "WORLD"  => 1, "PLAYER" => 2, "ENEMY"  => 4,
                "BULLET" => 8, "PICKUP" => 16, "UI"    => 32,
                _        => 1
            };
        return m;
    }

    private LuaTable? BuildRayResult(RaycastHit? hit)
    {
        if (hit is null) return null;
        var h = hit.Value;
        _lua.NewTable("_ray_result");
        var t = (LuaTable)_lua["_ray_result"];
        t["hit"]      = true;
        t["collider"] = h.Collider;
        t["x"]        = h.Point.X;
        t["y"]        = h.Point.Y;
        t["nx"]       = h.Normal.X;
        t["ny"]       = h.Normal.Y;
        t["dist"]     = h.Distance;
        return t;
    }

    // ── Utils ─────────────────────────────────────────────────────────
}