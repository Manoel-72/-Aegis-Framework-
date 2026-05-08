using Aegis.Resource;
using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Display;

/// <summary>
/// Painel de UI com borda não-esticada (9-slice / 9-patch).
/// Divide a textura em 9 regiões: 4 cantos fixos, 4 bordas esticadas, 1 centro.
///
///  ┌──┬──────────┬──┐
///  │TL│  TOP     │TR│
///  ├──┼──────────┼──┤
///  │L │  CENTER  │R │
///  ├──┼──────────┼──┤
///  │BL│  BOTTOM  │BR│
///  └──┴──────────┴──┘
///
/// border = tamanho em px de cada canto (uniforme nos 4 lados).
/// Width / Height definem o tamanho total do painel.
///
/// Uso Lua:
///   local panel = aegis.newPanel("ui/panel.png", 12)
///   aegis.setPanelSize(panel, 300, 180)
///   aegis.setPosition(panel, 100, 100)
/// </summary>
public sealed class NineSlice : Object2D
{
    public Texture2D? Texture { get; set; }
    public int Border { get; set; } = 8;

    private int _width  = 64;
    private int _height = 64;

    public int Width
    {
        get => _width;
        set => _width = Math.Max(value, Border * 2 + 1);
    }

    public int Height
    {
        get => _height;
        set => _height = Math.Max(value, Border * 2 + 1);
    }

    public NineSlice(Texture2D? texture, int border, Scene2D? parent = null)
    {
        Texture = texture;
        Border  = border;
        parent?.AddChild(this);
    }

    public override void Draw(SpriteBatch sb, float inheritedAlpha = 1f)
    {
        if (!Visible || Texture is null) return;

        var eff   = Alpha * inheritedAlpha;
        var color = Color.White * eff;
        var world = GetWorldMatrix();
        float wx  = world.M41;
        float wy  = world.M42;

        int b  = Border;
        int tw = Texture.Width;
        int th = Texture.Height;
        int mw = tw - b * 2;   // middle source width
        int mh = th - b * 2;   // middle source height

        // Tamanho destino das regiões
        int dMidW = Width  - b * 2;
        int dMidH = Height - b * 2;

        // Fonte: linha × coluna (3×3)
        Rectangle[,] src = new Rectangle[3, 3]
        {
            { Rect(0,     0,  b,  b), Rect(b,    0, mw,  b), Rect(b+mw,  0,  b,  b) },
            { Rect(0,     b,  b, mh), Rect(b,    b, mw, mh), Rect(b+mw,  b,  b, mh) },
            { Rect(0,  b+mh,  b,  b), Rect(b, b+mh, mw,  b), Rect(b+mw, b+mh, b, b) },
        };

        // Destino: linha × coluna
        Rectangle[,] dst = new Rectangle[3, 3]
        {
            { Rect(0,        0,        b,    b),    Rect(b,        0,       dMidW,    b)    , Rect(b+dMidW,  0,        b,    b)    },
            { Rect(0,        b,        b,    dMidH), Rect(b,       b,       dMidW,    dMidH), Rect(b+dMidW,  b,        b,    dMidH)},
            { Rect(0,        b+dMidH,  b,    b),    Rect(b,        b+dMidH, dMidW,    b)    , Rect(b+dMidW,  b+dMidH,  b,    b)    },
        };

        for (int row = 0; row < 3; row++)
        for (int col = 0; col < 3; col++)
        {
            if (dst[row, col].Width <= 0 || dst[row, col].Height <= 0) continue;

            var d = dst[row, col];
            d.X += (int)wx;
            d.Y += (int)wy;
            sb.Draw(Texture, d, src[row, col], color);
        }

        base.Draw(sb, eff);
    }

    private static Rectangle Rect(int x, int y, int w, int h) =>
        new Rectangle(x, y, w, h);
}
