using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Display;

/// <summary>
/// Componente visual de barra de progresso composto por fundo e preenchimento.
/// Mantem os filhos agrupados para alpha, escala, Z e remocao funcionarem como uma unidade.
/// </summary>
public sealed class ProgressBar : Object2D
{
    public Bitmap Background { get; }
    public Bitmap Fill { get; }
    public int Width { get; }
    public int Height { get; }
    public float Current { get; private set; } = 1f;
    public float Max { get; private set; } = 1f;

    public ProgressBar(int width, int height, Color backgroundColor, Color fillColor, Object2D? parent = null)
        : base(parent)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);

        Background = new Bitmap(CreateTexture(Width, Height, backgroundColor), this);
        Fill = new Bitmap(CreateTexture(Width, Height, fillColor), this)
        {
            Pivot = Vector2.Zero
        };
    }

    public void SetValue(float current, float max)
    {
        Current = current;
        Max = max;

        var ratio = Max > 0f ? Math.Clamp(Current / Max, 0f, 1f) : 0f;
        Fill.ScaleX = Math.Max(0.001f, ratio);
    }

    public void SetColors(Color? backgroundColor = null, Color? fillColor = null)
    {
        if (backgroundColor.HasValue)
            Paint(Background, backgroundColor.Value);

        if (fillColor.HasValue)
            Paint(Fill, fillColor.Value);
    }

    private void Paint(Bitmap bitmap, Color color)
    {
        if (bitmap.Texture is null) return;
        bitmap.Texture.SetData(Enumerable.Repeat(color, Width * Height).ToArray());
    }

    private static Texture2D CreateTexture(int width, int height, Color color)
    {
        var texture = new Texture2D(Renderer.GraphicsDevice, width, height);
        texture.SetData(Enumerable.Repeat(color, width * height).ToArray());
        return texture;
    }
}
