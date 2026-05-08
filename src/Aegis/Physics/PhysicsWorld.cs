namespace Aegis.Physics;

public sealed class PhysicsWorld
{
    public static PhysicsWorld Instance { get; } = new();
    private PhysicsWorld() { }

    private readonly List<Rigidbody2D> _bodies = new();
    public int BodyCount => _bodies.Count;

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

        foreach (var rb in _bodies.ToArray())
        {
            rb.IntegrateX(dt);
            CollisionSystem.Instance.ResolveBodyAxis(rb, _bodies, Axis.X);
        }

        foreach (var rb in _bodies.ToArray())
        {
            rb.IntegrateY(dt);
            CollisionSystem.Instance.ResolveBodyAxis(rb, _bodies, Axis.Y);
        }

        foreach (var rb in _bodies.ToArray())
            rb.ApplyGroundFriction(dt);

        CollisionSystem.Instance.RebuildContactsAndFire();
    }
}

public enum Axis { X, Y }
