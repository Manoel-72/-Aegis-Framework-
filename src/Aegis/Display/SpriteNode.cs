using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Display;

/// <summary>
/// v0.5 — nó de sprite para PNG/JPG, com pivot e recorte de spritesheet.
/// Usa Texture2D.FromStream via ResManager.LoadTexture.
/// </summary>
public sealed class SpriteNode : Bitmap
{
    public SpriteNode(Texture2D texture, Scene2D? parent = null)
        : base(texture, parent)
    {
    }

    public void SetFrame(int x, int y, int width, int height)
    {
        if (Texture is null) return;

        width = Math.Max(0, width);
        height = Math.Max(0, height);
        x = Math.Clamp(x, 0, Texture.Width);
        y = Math.Clamp(y, 0, Texture.Height);

        if (x + width > Texture.Width) width = Texture.Width - x;
        if (y + height > Texture.Height) height = Texture.Height - y;

        SourceRect = width <= 0 || height <= 0
            ? null
            : new Rectangle(x, y, width, height);
    }

    public void ClearFrame() => SourceRect = null;
}
