using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Display;

/// <summary>
/// Container de layout simples inspirado em h2d.Flow: organiza filhos em linha ou coluna.
/// Ideal para HUDs, menus e barras de UI sem posição hardcoded.
/// </summary>
public sealed class FlowContainer : Object2D
{
    public string Direction { get; private set; } = "horizontal";
    public float Gap { get; set; } = 0f;
    public float Padding { get; set; } = 0f;
    public string Align { get; set; } = "start"; // start, center, end
    public float Width { get; private set; }
    public float Height { get; private set; }

    public FlowContainer(string direction = "horizontal", float gap = 0f, float padding = 0f, string align = "start", Scene2D? parent = null)
    {
        Direction = NormalizeDirection(direction);
        Gap = MathF.Max(0f, gap);
        Padding = MathF.Max(0f, padding);
        Align = NormalizeAlign(align);
        parent?.AddChild(this);
    }

    public void AddFlowChild(Object2D child)
    {
        AddChild(child);
        Layout();
    }

    public void Configure(string? direction = null, float? gap = null, float? padding = null, string? align = null)
    {
        if (!string.IsNullOrWhiteSpace(direction)) Direction = NormalizeDirection(direction);
        if (gap.HasValue) Gap = MathF.Max(0f, gap.Value);
        if (padding.HasValue) Padding = MathF.Max(0f, padding.Value);
        if (!string.IsNullOrWhiteSpace(align)) Align = NormalizeAlign(align);
        Layout();
    }

    public override void Update(float dt)
    {
        Layout();
        base.Update(dt);
    }

    public void Layout()
    {
        var children = Children.Where(c => c.Visible).ToArray();
        if (children.Length == 0)
        {
            Width = Height = Padding * 2f;
            return;
        }

        var sizes = children.Select(Measure).ToArray();
        if (Direction == "vertical")
        {
            var maxW = sizes.Max(s => s.X);
            var totalH = sizes.Sum(s => s.Y) + Gap * MathF.Max(0, children.Length - 1);
            var y = Padding;
            for (var i = 0; i < children.Length; i++)
            {
                var x = Padding + AlignOffset(maxW, sizes[i].X);
                children[i].X = x;
                children[i].Y = y;
                y += sizes[i].Y + Gap;
            }
            Width = maxW + Padding * 2f;
            Height = totalH + Padding * 2f;
        }
        else
        {
            var maxH = sizes.Max(s => s.Y);
            var totalW = sizes.Sum(s => s.X) + Gap * MathF.Max(0, children.Length - 1);
            var x = Padding;
            for (var i = 0; i < children.Length; i++)
            {
                var y = Padding + AlignOffset(maxH, sizes[i].Y);
                children[i].X = x;
                children[i].Y = y;
                x += sizes[i].X + Gap;
            }
            Width = totalW + Padding * 2f;
            Height = maxH + Padding * 2f;
        }
    }

    private float AlignOffset(float max, float size) => Align switch
    {
        "center" => (max - size) * 0.5f,
        "end" or "right" or "bottom" => max - size,
        _ => 0f
    };

    private static Vector2 Measure(Object2D o)
    {
        try
        {
            return o switch
            {
                Bitmap b => new Vector2(MathF.Max(0, b.TextureWidth * MathF.Abs(b.ScaleX)), MathF.Max(0, b.TextureHeight * MathF.Abs(b.ScaleY))),
                Label l when l.Font is not null => l.Font.MeasureString(l.Text) * new Vector2(MathF.Abs(l.ScaleX), MathF.Abs(l.ScaleY)),
                NineSlice n => new Vector2(MathF.Max(0, n.Width * MathF.Abs(n.ScaleX)), MathF.Max(0, n.Height * MathF.Abs(n.ScaleY))),
                FlowContainer f => new Vector2(f.Width, f.Height),
                _ => new Vector2(32f * MathF.Abs(o.ScaleX), 32f * MathF.Abs(o.ScaleY))
            };
        }
        catch { return new Vector2(32f, 32f); }
    }

    private static string NormalizeDirection(string d)
        => d.Trim().Equals("vertical", StringComparison.OrdinalIgnoreCase) ? "vertical" : "horizontal";

    private static string NormalizeAlign(string a)
    {
        var v = a.Trim().ToLowerInvariant();
        return v is "center" or "end" or "right" or "bottom" ? v : "start";
    }

    public override void Draw(SpriteBatch sb, float inheritedAlpha = 1f)
    {
        Layout();
        base.Draw(sb, inheritedAlpha);
    }
}
