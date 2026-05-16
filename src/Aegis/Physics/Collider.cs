using Aegis.Scene;
using Microsoft.Xna.Framework;

namespace Aegis.Physics;

public enum ColliderShape
{
    AABB,
    Circle,
    /// <summary>
    /// Triângulo retângulo (ramp).
    /// Definido por SlopeDir (Left/Right) e Height — largura = Width do collider.
    /// A base está no fundo do AABB envolvente; o vértice alto está no topo do lado indicado.
    /// Ex: SlopeDir.Right = rampa subindo da esquerda para a direita.
    /// </summary>
    Slope
}

/// <summary>Direção do vértice alto da rampa.</summary>
public enum SlopeDir { Right, Left }

/// <summary>
/// Componente de colisão acoplado a um Object2D.
///
/// Shape:
/// - AABB:   retângulo padrão, resolução física por eixo.
/// - Circle: círculo para detecção/trigger.
/// - Slope:  triângulo de rampa — resolução Y via interpolação da superfície inclinada.
///
/// Layer / Mask: bitmask de colisão.
/// Trigger: apenas detecta sobreposição, sem resolução física.
///
/// Layers pré-definidas (string no Lua):
///   WORLD=1  PLAYER=2  ENEMY=4  BULLET=8  PICKUP=16  UI=32
/// </summary>
public sealed class Collider
{
    public Object2D Owner { get; }

    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public float Width   { get; set; }
    public float Height  { get; set; }

    public ColliderShape Shape   { get; set; } = ColliderShape.AABB;
    public float         Radius  { get; set; }

    // ── Slope ────────────────────────────────────────────────────────────
    /// <summary>Direção do vértice alto. Só relevante quando Shape == Slope.</summary>
    public SlopeDir SlopeDirection { get; set; } = SlopeDir.Right;

    /// <summary>
    /// Retorna a altura Y da superfície da rampa para uma posição X no espaço de mundo.
    /// Retorna float.MaxValue se X estiver fora da rampa.
    /// </summary>
    public float GetSlopeSurfaceY(float worldX)
    {
        var b = Bounds;
        if (worldX < b.Left || worldX > b.Right) return float.MaxValue;
        float t = (worldX - b.Left) / b.Width;   // 0..1
        // SlopeDir.Right: topo à direita  → Y desce de cima-esq (topo) para baixo-dir (base)
        // SlopeDir.Left:  topo à esquerda → Y desce de cima-dir para baixo-esq
        float surfaceY = SlopeDirection == SlopeDir.Right
            ? b.Top    + (1f - t) * b.Height   // esq=topo, dir=base
            : b.Top    + t        * b.Height;  // esq=base, dir=topo
        return surfaceY;
    }

    public int  Layer     { get; set; } = 1;
    public int  Mask      { get; set; } = ~0;
    public bool IsTrigger { get; set; } = false;
    public bool IsActive  { get; set; } = true;
    public bool IsOneWay  { get; set; } = false;

    // Callbacks Lua
    internal Action<Collider, Collider>? OnCollideEnter;
    internal Action<Collider, Collider>? OnCollideStay;
    internal Action<Collider, Collider>? OnCollideExit;

    internal readonly HashSet<Collider> CurrentContacts  = new();
    internal readonly HashSet<Collider> PreviousContacts = new();

    public Collider(Object2D owner, float w, float h,
                    float offX = 0f, float offY = 0f)
    {
        Owner   = owner;
        Width   = w;
        Height  = h;
        Radius  = MathF.Min(w, h) * 0.5f;
        OffsetX = offX;
        OffsetY = offY;
    }

    // AABB em espaço de mundo. Para Circle, retorna a caixa envolvente do círculo.
    public RectangleF Bounds => Shape == ColliderShape.Circle
        ? new RectangleF(CircleCenter.X - Radius, CircleCenter.Y - Radius, Radius * 2f, Radius * 2f)
        : new RectangleF(Owner.X + OffsetX, Owner.Y + OffsetY, Width, Height);

    public Vector2 CircleCenter => new(Owner.X + OffsetX + Radius, Owner.Y + OffsetY + Radius);

    // Teste rápido de sobreposição + filtro de layer.
    public bool Overlaps(Collider other)
    {
        if (!IsActive || !other.IsActive) return false;
        if ((Layer & other.Mask) == 0 || (other.Layer & Mask) == 0) return false;
        return CollisionMath.Overlaps(this, other);
    }
}

internal static class CollisionMath
{
    public static bool Overlaps(Collider a, Collider b)
    {
        if (a.Shape == ColliderShape.AABB && b.Shape == ColliderShape.AABB)
            return a.Bounds.Intersects(b.Bounds);

        if (a.Shape == ColliderShape.Circle && b.Shape == ColliderShape.Circle)
            return CircleCircle(a, b);

        return a.Shape == ColliderShape.Circle
            ? AabbCircle(b, a)
            : AabbCircle(a, b);
    }

    private static bool CircleCircle(Collider a, Collider b)
    {
        var delta = a.CircleCenter - b.CircleCenter;
        var radius = MathF.Max(0f, a.Radius) + MathF.Max(0f, b.Radius);
        return delta.LengthSquared() < radius * radius;
    }

    private static bool AabbCircle(Collider box, Collider circle)
    {
        var b = box.Bounds;
        var center = circle.CircleCenter;
        var closestX = Math.Clamp(center.X, b.Left, b.Right);
        var closestY = Math.Clamp(center.Y, b.Top, b.Bottom);
        var dx = center.X - closestX;
        var dy = center.Y - closestY;
        var r = MathF.Max(0f, circle.Radius);
        return dx * dx + dy * dy < r * r;
    }
}

/// <summary>RectangleF — versão float (MonoGame só tem Rectangle inteiro).</summary>
public readonly struct RectangleF
{
    public readonly float X, Y, Width, Height;

    public RectangleF(float x, float y, float w, float h)
    { X = x; Y = y; Width = w; Height = h; }

    public float Left   => X;
    public float Right  => X + Width;
    public float Top    => Y;
    public float Bottom => Y + Height;

    public bool Intersects(RectangleF o) =>
        Left < o.Right && Right > o.Left &&
        Top  < o.Bottom && Bottom > o.Top;
}
