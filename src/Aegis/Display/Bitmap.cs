using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Display;

/// <summary>
/// Sprite 2D — desenha uma textura na cena com suporte a
/// recorte (SourceRect), pivot e transformação hierárquica.
/// Equivalente ao h2d.Bitmap do Heaps.
/// </summary>
public class Bitmap : Object2D
{
    public Texture2D? Texture    { get; set; }
    public Rectangle? SourceRect { get; set; }  // null = textura inteira

    /// Pivot normalizado: (0,0)=topo-esq, (0.5,0.5)=centro, (1,1)=baixo-dir
    public Vector2 Pivot { get; set; } = Vector2.Zero;
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }

    public int TextureWidth  => SourceRect?.Width  ?? Texture?.Width  ?? 0;
    public int TextureHeight => SourceRect?.Height ?? Texture?.Height ?? 0;

    public Bitmap(Texture2D? texture, Scene2D? parent = null)
    {
        Texture = texture;
        parent?.AddChild(this);
    }

    public Bitmap(Texture2D? texture, Object2D parent)
    {
        Texture = texture;
        parent.AddChild(this);
    }

    public override void Draw(SpriteBatch sb, float inheritedAlpha = 1f)
    {
        if (!Visible || Texture is null) return;

        var   eff    = Alpha * inheritedAlpha;
        var   world  = GetWorldMatrix();
        var   pos    = new Vector2(world.M41, world.M42);
        float scX    = MathF.Sqrt(world.M11 * world.M11 + world.M21 * world.M21);
        float scY    = MathF.Sqrt(world.M12 * world.M12 + world.M22 * world.M22);
        float rot    = MathF.Atan2(world.M21, world.M11);
        var   origin = new Vector2(TextureWidth * Pivot.X, TextureHeight * Pivot.Y);
        var   effects = SpriteEffects.None;
        if (FlipX) effects |= SpriteEffects.FlipHorizontally;
        if (FlipY) effects |= SpriteEffects.FlipVertically;

        var shader = Shader;
        if (shader?.Name == "outline")
        {
            var w = MathF.Max(1f, shader.Width);
            var outlineColor = shader.Color * eff;
            sb.Draw(Texture, pos + new Vector2( w, 0), SourceRect, outlineColor, rot, origin, new Vector2(scX, scY), effects, 0f);
            sb.Draw(Texture, pos + new Vector2(-w, 0), SourceRect, outlineColor, rot, origin, new Vector2(scX, scY), effects, 0f);
            sb.Draw(Texture, pos + new Vector2(0,  w), SourceRect, outlineColor, rot, origin, new Vector2(scX, scY), effects, 0f);
            sb.Draw(Texture, pos + new Vector2(0, -w), SourceRect, outlineColor, rot, origin, new Vector2(scX, scY), effects, 0f);
        }

        var color = Color.White * eff;
        if (shader?.Name == "flash") color = shader.Color * eff;
        if (shader?.Name == "grayscale") color = Color.Gray * eff; // fallback sem MGFX: tint cinza simples.
        if (shader?.Name == "dissolve" && shader.Progress >= 1f) return;

        sb.Draw(
            Texture, pos, SourceRect,
            color,
            rot, origin, new Vector2(scX, scY),
            effects, 0f
        );

        base.Draw(sb, eff);
    }
}
