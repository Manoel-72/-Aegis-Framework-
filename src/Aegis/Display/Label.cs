using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Display;

/// <summary>
/// Texto 2D renderizado na cena.
/// Equivalente ao h2d.Text do Heaps.
/// </summary>
public class Label : Object2D
{
    public string      Text  { get; set; } = "";
    public SpriteFont? Font  { get; set; }
    public Color       Color { get; set; } = Color.White;
    public Vector2     Pivot { get; set; } = Vector2.Zero;

    public Label(SpriteFont? font, Scene2D? parent = null)
    {
        Font = font;
        parent?.AddChild(this);
    }

    public Label(SpriteFont? font, Object2D parent)
    {
        Font = font;
        parent.AddChild(this);
    }

    public override void Draw(SpriteBatch sb, float inheritedAlpha = 1f)
    {
        if (!Visible || Font is null || string.IsNullOrEmpty(Text)) return;

        var   eff    = Alpha * inheritedAlpha;
        var   world  = GetWorldMatrix();
        var   pos    = new Vector2(world.M41, world.M42);
        var   size   = Font.MeasureString(Text);
        var   origin = new Vector2(size.X * Pivot.X, size.Y * Pivot.Y);
        float scX    = MathF.Sqrt(world.M11 * world.M11 + world.M21 * world.M21);
        float scY    = MathF.Sqrt(world.M12 * world.M12 + world.M22 * world.M22);
        float rot    = MathF.Atan2(world.M21, world.M11);

        sb.DrawString(
            Font, Text, pos,
            Color * eff,
            rot, origin, new Vector2(scX, scY),
            SpriteEffects.None, 0f
        );

        base.Draw(sb, eff);
    }
}
