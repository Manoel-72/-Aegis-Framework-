using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Aegis;

/// <summary>
/// Node que renderiza uma textura (PNG/JPG) via Raylib DrawTexturePro.
///
/// Funcionalidades:
///   • Pivot  — âncora de posição/rotação (0,0 = topo-esq, 0.5/1 = centro-baixo)
///   • Frame  — recorte de spritesheet em pixels (null = textura inteira)
///   • FlipH  — espelha horizontalmente
///   • FlipV  — espelha verticalmente
///   • ScaleX/Y, Rotation, Color tint, Alpha — herdados de Node
/// </summary>
public sealed class SpriteNode : Node
{
    public Texture2D  Texture  { get; set; }
    public Rectangle? Frame    { get; set; }   // null = textura inteira
    public bool       FlipH    { get; set; }
    public bool       FlipV    { get; set; }

    // Dimensões do frame atual (usadas para física e câmera)
    public int FrameW => Frame.HasValue ? (int)Frame.Value.Width  : Texture.Width;
    public int FrameH => Frame.HasValue ? (int)Frame.Value.Height : Texture.Height;
    public int TexW   => Texture.Width;
    public int TexH   => Texture.Height;

    public SpriteNode(Texture2D tex) => Texture = tex;

    public override void Draw()
    {
        if (!Visible) return;

        // Fonte: frame do spritesheet ou textura inteira
        var src = Frame ?? new Rectangle(0, 0, Texture.Width, Texture.Height);

        // Flip: inverter dimensão na source (Raylib entende sinal negativo)
        if (FlipH) { src.X += src.Width;  src.Width  = -src.Width;  }
        if (FlipV) { src.Y += src.Height; src.Height = -src.Height; }

        float dw = FrameW * MathF.Abs(ScaleX);
        float dh = FrameH * MathF.Abs(ScaleY);

        var dst    = new Rectangle(X, Y, dw, dh);
        var origin = new Vector2(dw * PivotX, dh * PivotY);
        var tint   = new Color(R, G, B, A);

        DrawTexturePro(Texture, src, dst, origin,
                       Rotation * (180f / MathF.PI), tint);
    }
}
