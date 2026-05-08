using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Aegis.Display;

namespace Aegis.Scene;

/// <summary>
/// Nó base da cena 2D com suporte a hierarquia pai-filho e
/// transformação acumulada (posição, escala, rotação, alpha).
/// Equivalente ao h2d.Object do Heaps.
/// </summary>
public class Object2D
{
    private float _x;
    private float _y;
    private float _scaleX = 1f;
    private float _scaleY = 1f;
    private float _rotation;
    private float _alpha = 1f;

    public float X        { get => _x; set => _x = RequireFinite(value, nameof(X)); }
    public float Y        { get => _y; set => _y = RequireFinite(value, nameof(Y)); }
    public float ScaleX   { get => _scaleX; set => _scaleX = RequireFinite(value, nameof(ScaleX), 1f); }
    public float ScaleY   { get => _scaleY; set => _scaleY = RequireFinite(value, nameof(ScaleY), 1f); }
    public float Rotation { get => _rotation; set => _rotation = RequireFinite(value, nameof(Rotation)); }
    public float Alpha    { get => _alpha; set => _alpha = Math.Clamp(RequireFinite(value, nameof(Alpha), 1f), 0f, 1f); }
    public bool  Visible  { get; set; } = true;
    public ObjectShaderConfig? Shader { get; set; }

    /// <summary>Ordem de desenho dentro do mesmo pai. Maior valor desenha por cima.</summary>
    public int Z { get; set; } = 0;

    /// <summary>Alias legado para manter compatibilidade com projetos antigos.</summary>
    public int ZOrder
    {
        get => Z;
        set => Z = value;
    }

    public Object2D? Parent { get; private set; }
    private readonly List<Object2D> _children = new();
    public IReadOnlyList<Object2D> Children => _children;

    public void AddChild(Object2D child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (ReferenceEquals(child, this))
            throw new InvalidOperationException("Object2D não pode ser filho dele mesmo.");

        for (var p = this; p is not null; p = p.Parent)
        {
            if (ReferenceEquals(p, child))
                throw new InvalidOperationException("Hierarquia circular detectada em Object2D.AddChild().");
        }

        if (ReferenceEquals(child.Parent, this)) return;

        child.Parent?.RemoveChild(child);
        child.Parent = this;
        _children.Add(child);
    }

    public void RemoveChild(Object2D child)
    {
        if (_children.Remove(child))
            child.Parent = null;
    }

    public void RemoveFromParent() => Parent?.RemoveChild(this);

    public Matrix GetLocalMatrix() =>
        Matrix.CreateScale(ScaleX, ScaleY, 1f)
      * Matrix.CreateRotationZ(Rotation)
      * Matrix.CreateTranslation(X, Y, 0f);

    public Matrix GetWorldMatrix() =>
        Parent is null
            ? GetLocalMatrix()
            : GetLocalMatrix() * Parent.GetWorldMatrix();

    public Vector2 WorldPosition
    {
        get { var m = GetWorldMatrix(); return new Vector2(m.M41, m.M42); }
    }

    public virtual void Update(float dt)
    {
        foreach (var child in _children.ToArray())
            child.Update(dt);
    }

    public virtual void Draw(SpriteBatch sb, float inheritedAlpha = 1f)
    {
        if (!Visible) return;
        var effectiveAlpha = Alpha * inheritedAlpha;
        foreach (var child in _children.OrderBy(c => c.Z).ToArray())
            child.Draw(sb, effectiveAlpha);
    }

    private static float RequireFinite(float value, string fieldName, float fallback = 0f)
        => float.IsFinite(value) ? value : fallback;
}
