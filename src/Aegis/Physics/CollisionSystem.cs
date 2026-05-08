using Microsoft.Xna.Framework;
using Aegis.Scene;

namespace Aegis.Physics;

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

    public void ResolveBodyAxis(Rigidbody2D rb, IReadOnlyList<Rigidbody2D> bodies, Axis axis)
    {
        if (rb.IsKinematic) return;

        for (int safety = 0; safety < 4; safety++)
        {
            bool moved = false;

            foreach (var self in _colliders.ToArray())
            {
                if (!self.IsActive || self.Owner != rb.Owner || self.IsTrigger)
                    continue;

                foreach (var other in _colliders.ToArray())
                {
                    if (!other.IsActive || other == self || other.Owner == rb.Owner)
                        continue;
                    if (!LayersMask(self, other))
                        continue;
                    if (other.IsTrigger)
                        continue;

                    // Axis solver stays AABB-only. Circle is detection/trigger-safe.
                    if (self.Shape != ColliderShape.AABB || other.Shape != ColliderShape.AABB)
                        continue;

                    var otherBody = FindBody(bodies, other.Owner);

                    // Meio-termo estável: dinâmicos não empurram dinâmicos.
                    // Para dano/coleta use trigger/callback; para chão/plataforma use collider sem Rigidbody.
                    if (otherBody is not null && !otherBody.IsKinematic)
                        continue;

                    var a = self.Bounds;
                    var b = other.Bounds;
                    if (!a.Intersects(b)) continue;

                    float overlapX = MathF.Min(a.Right, b.Right) - MathF.Max(a.Left, b.Left);
                    float overlapY = MathF.Min(a.Bottom, b.Bottom) - MathF.Max(a.Top, b.Top);
                    if (overlapX <= 0f || overlapY <= 0f) continue;

                    if (axis == Axis.X)
                    {
                        if (rb.VelocityX > 0f)
                        {
                            rb.Owner.X -= overlapX;
                            rb.OnHitWall(-1f);
                        }
                        else if (rb.VelocityX < 0f)
                        {
                            rb.Owner.X += overlapX;
                            rb.OnHitWall(1f);
                        }
                        else
                        {
                            float acx = a.Left + a.Width * 0.5f;
                            float bcx = b.Left + b.Width * 0.5f;
                            if (acx < bcx) { rb.Owner.X -= overlapX; rb.OnHitWall(-1f); }
                            else           { rb.Owner.X += overlapX; rb.OnHitWall( 1f); }
                        }

                        moved = true;
                    }
                    else
                    {
                        if (rb.VelocityY > 0f)
                        {
                            rb.Owner.Y -= overlapY;
                            rb.OnLand();
                        }
                        else if (rb.VelocityY < 0f)
                        {
                            rb.Owner.Y += overlapY;
                            rb.OnHitCeiling();
                        }
                        else
                        {
                            float acy = a.Top + a.Height * 0.5f;
                            float bcy = b.Top + b.Height * 0.5f;
                            if (acy < bcy) { rb.Owner.Y -= overlapY; rb.OnLand(); }
                            else           { rb.Owner.Y += overlapY; rb.OnHitCeiling(); }
                        }

                        moved = true;
                    }

                    rb.SyncPositionOnly();
                }
            }

            if (!moved) break;
        }
    }

    public void RebuildContactsAndFire()
    {
        foreach (var c in _colliders)
            c.CurrentContacts.Clear();

        for (int i = 0; i < _colliders.Count; i++)
        for (int j = i + 1; j < _colliders.Count; j++)
        {
            var a = _colliders[i];
            var b = _colliders[j];
            if (!a.IsActive || !b.IsActive) continue;
            if (!LayersMask(a, b)) continue;
            if (!a.Overlaps(b)) continue;
            a.CurrentContacts.Add(b);
            b.CurrentContacts.Add(a);
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

    private static bool RayVsRect(Vector2 ro, Vector2 rd, float maxLength, RectangleF r, out float tHit, out Vector2 normal)
    {
        tHit = 0f;
        normal = Vector2.Zero;

        float invX = rd.X == 0f ? float.MaxValue : 1f / rd.X;
        float invY = rd.Y == 0f ? float.MaxValue : 1f / rd.Y;

        float tx1 = (r.Left - ro.X) * invX;
        float tx2 = (r.Right - ro.X) * invX;
        float ty1 = (r.Top - ro.Y) * invY;
        float ty2 = (r.Bottom - ro.Y) * invY;

        float tMinX = MathF.Min(tx1, tx2), tMaxX = MathF.Max(tx1, tx2);
        float tMinY = MathF.Min(ty1, ty2), tMaxY = MathF.Max(ty1, ty2);

        float tEnter = MathF.Max(tMinX, tMinY);
        float tExit = MathF.Min(tMaxX, tMaxY);

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
