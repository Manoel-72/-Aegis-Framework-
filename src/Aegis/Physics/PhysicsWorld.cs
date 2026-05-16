namespace Aegis.Physics;

public sealed class PhysicsWorld
{
    public static PhysicsWorld Instance { get; } = new();
    private PhysicsWorld() { }

    private readonly List<Rigidbody2D> _bodies = new();
    public int BodyCount => _bodies.Count;
    public IReadOnlyList<Rigidbody2D> Bodies => _bodies;

    public void AddBody(Rigidbody2D rb)
    {
        if (!_bodies.Contains(rb)) _bodies.Add(rb);
    }

    public void RemoveBody(Rigidbody2D rb) => _bodies.Remove(rb);

    public void Reset()
    {
        _bodies.Clear();
        CollisionSystem.Instance.Reset();
    }

    public void Step(float dt)
    {
        if (!float.IsFinite(dt) || dt <= 0f) return;

        // Cenas só com Colliders (Lua move objetos, sem Rigidbody) ainda precisam
        // de detecção de overlap e callbacks de trigger — não retornar cedo.
        if (_bodies.Count == 0)
        {
            CollisionSystem.Instance.BeginContacts();
            CollisionSystem.Instance.RebuildContactsAndFire();
            return;
        }

        dt = Math.Clamp(dt, 1f / 600f, 1f / 30f);

        CollisionSystem.Instance.BeginContacts();

        foreach (var rb in _bodies.ToArray())
        {
            rb.SyncFromOwner();
            rb.SanitizeVelocities();
            rb.BeginFrame();
        }

        // Rebuild resolve hash uma vez por eixo — O(n) médio para candidatos
        CollisionSystem.Instance.RebuildResolveHash();
        foreach (var rb in _bodies.ToArray())
        {
            rb.IntegrateX(dt);
            CollisionSystem.Instance.ResolveBodyAxis(rb, _bodies, Axis.X);
        }

        CollisionSystem.Instance.RebuildResolveHash();
        foreach (var rb in _bodies.ToArray())
        {
            rb.IntegrateY(dt);
            CollisionSystem.Instance.ResolveBodyAxis(rb, _bodies, Axis.Y);
        }

        // Slope: resolver rampas depois do eixo Y — empurra corpo para cima da superfície
        CollisionSystem.Instance.ResolveSlopes(_bodies);

        foreach (var rb in _bodies.ToArray())
            rb.ApplyGroundFriction(dt);

        // Sprint 4: moving platform — transfere deslocamento de kinematic para dinâmico em cima
        ApplyMovingPlatformCarry(_bodies);

        CollisionSystem.Instance.RebuildContactsAndFire();
    }

    /// <summary>
    /// Sprint 4: kinematic em movimento que está diretamente abaixo de um dinâmico
    /// transfere sua velocidade horizontal para o dinâmico — "carrega" o player.
    /// </summary>
    private static void ApplyMovingPlatformCarry(List<Rigidbody2D> bodies)
    {
        const float CarryBlend = 0.85f; // quanto da velocidade da plataforma é transferida
        const float GroundTolerance = 4f; // pixels de margem para considerar "em cima"

        var arr = bodies.ToArray();
        foreach (var dyn in arr)
        {
            if (dyn.IsKinematic || !dyn.IsGrounded) continue;
            var dynCol = CollisionSystem.Instance.GetFirstCollider(dyn.Owner);
            if (dynCol is null) continue;
            var db = dynCol.Bounds;

            foreach (var kin in arr)
            {
                if (!kin.IsKinematic) continue;
                var kinCol = CollisionSystem.Instance.GetFirstCollider(kin.Owner);
                if (kinCol is null) continue;
                var kb = kinCol.Bounds;

                // O dinâmico deve estar logo acima do topo da plataforma
                bool horizontalOverlap = db.Left < kb.Right && db.Right > kb.Left;
                bool sittingOnTop = MathF.Abs(db.Bottom - kb.Top) < GroundTolerance;
                if (!horizontalOverlap || !sittingOnTop) continue;

                // Transfere velocidade horizontal da plataforma para o dinâmico
                dyn.Owner.X += (kin.Owner.X - kin.PrevX) * CarryBlend;
                dyn.SyncPositionOnly();
                break;
            }
        }
    }
}

public enum Axis { X, Y }
