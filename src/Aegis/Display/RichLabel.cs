using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Display;

/// <summary>
/// Label com marcação inline de cor e escala por segmento.
///
/// Sintaxe (inspirada em BBCode simples):
///   [c=r,g,b]texto[/c]   → muda cor do segmento  (0..1 por canal)
///   [s=1.5]texto[/s]     → escala o segmento (relativo ao base ScaleX/Y)
///   [n]                  → quebra de linha
///
/// Exemplo Lua:
///   local r = aegis.newRichLabel("[c=1,0.3,0.3]GAME[/c] [c=1,1,0]OVER[/c]")
///   aegis.setPosition(r, 400, 300)
///   aegis.setPivotRich(r, 0.5, 0.5)
///
/// Sem font externa → usa SpriteFont injetada via RichLabel.DefaultFont
/// (mesma injeção da Label normal).
/// </summary>
public sealed class RichLabel : Object2D
{
    // Font compartilhada com Label — injetada pelo ResManager quando disponível
    public static SpriteFont? DefaultFont { get; set; }

    public SpriteFont? Font  { get; set; }
    public string      Markup { get; set; } = "";
    public Vector2     Pivot  { get; set; } = Vector2.Zero;

    // Cache de segmentos parseados — re-parse só quando Markup muda
    private string          _lastMarkup = "";
    private List<Segment>   _segments   = new();

    private readonly struct Segment
    {
        public readonly string  Text;
        public readonly Color   Color;
        public readonly float   Scale;
        public Segment(string t, Color c, float s) { Text = t; Color = c; Scale = s; }
    }

    public RichLabel(Scene2D? parent = null)
    {
        parent?.AddChild(this);
    }

    // ── Parse ─────────────────────────────────────────────────────────
    private void ParseIfDirty()
    {
        if (_lastMarkup == Markup) return;
        _lastMarkup = Markup;
        _segments   = Parse(Markup);
    }

    private static List<Segment> Parse(string markup)
    {
        var result    = new List<Segment>();
        var colorStack = new Stack<Color>();
        var scaleStack = new Stack<float>();
        colorStack.Push(Color.White);
        scaleStack.Push(1f);

        // Tokeniza por tags simples
        int i = 0;
        while (i < markup.Length)
        {
            if (markup[i] == '[')
            {
                int end = markup.IndexOf(']', i);
                if (end < 0) { result.Add(new Segment(markup[i..], colorStack.Peek(), scaleStack.Peek())); break; }

                var tag = markup[(i + 1)..end];

                if (tag.StartsWith("c="))
                {
                    var parts = tag[2..].Split(',');
                    if (parts.Length == 3
                        && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r)
                        && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float g)
                        && float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float b))
                        colorStack.Push(new Color(r, g, b));
                }
                else if (tag == "/c" && colorStack.Count > 1)
                    colorStack.Pop();
                else if (tag.StartsWith("s="))
                {
                    if (float.TryParse(tag[2..], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float sc))
                        scaleStack.Push(sc);
                }
                else if (tag == "/s" && scaleStack.Count > 1)
                    scaleStack.Pop();
                else if (tag == "n")
                    result.Add(new Segment("\n", colorStack.Peek(), scaleStack.Peek()));

                i = end + 1;
            }
            else
            {
                // Texto literal até a próxima tag ou fim
                int next = markup.IndexOf('[', i);
                var text = next < 0 ? markup[i..] : markup[i..next];
                if (text.Length > 0)
                    result.Add(new Segment(text, colorStack.Peek(), scaleStack.Peek()));
                i = next < 0 ? markup.Length : next;
            }
        }
        return result;
    }

    // ── Medida total (para pivot) ─────────────────────────────────────
    private Vector2 MeasureAll(SpriteFont font)
    {
        float lineW = 0f, maxW = 0f, totalH = font.LineSpacing;
        foreach (var seg in _segments)
        {
            if (seg.Text == "\n")
            {
                maxW   = MathF.Max(maxW, lineW);
                lineW  = 0f;
                totalH += font.LineSpacing * seg.Scale;
            }
            else
            {
                var sz = font.MeasureString(seg.Text) * seg.Scale;
                lineW += sz.X;
            }
        }
        maxW = MathF.Max(maxW, lineW);
        return new Vector2(maxW, totalH);
    }

    // ── Draw ──────────────────────────────────────────────────────────
    public override void Draw(SpriteBatch sb, float inheritedAlpha = 1f)
    {
        if (!Visible || string.IsNullOrEmpty(Markup)) return;

        var font = Font ?? DefaultFont;
        if (font is null) return;

        ParseIfDirty();

        var   eff    = Alpha * inheritedAlpha;
        var   world  = GetWorldMatrix();
        var   origin = MeasureAll(font);
        float baseX  = world.M41 - origin.X * Pivot.X;
        float baseY  = world.M42 - origin.Y * Pivot.Y;
        float curX   = baseX;
        float curY   = baseY;

        foreach (var seg in _segments)
        {
            if (seg.Text == "\n")
            {
                curX  = baseX;
                curY += font.LineSpacing * seg.Scale;
                continue;
            }

            sb.DrawString(
                font, seg.Text,
                new Vector2(curX, curY),
                seg.Color * eff,
                0f,
                Vector2.Zero,
                seg.Scale,
                SpriteEffects.None, 0f
            );

            curX += font.MeasureString(seg.Text).X * seg.Scale;
        }

        base.Draw(sb, eff);
    }
}
