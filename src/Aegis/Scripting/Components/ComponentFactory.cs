using Aegis.Core;
using Aegis.Display;
using Aegis.Resource;
using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLua;

namespace Aegis.Scripting.Components;

/// <summary>
/// Centraliza a criação de componentes visuais expostos ao Lua.
/// Esta é a superfície recomendada para novos atalhos de criação da engine.
/// </summary>
internal sealed class ComponentFactory
{
    private readonly App _app;

    public ComponentFactory(App app)
    {
        _app = app;
    }

    public void ClearRuntimeState()
    {
    }

    public Object2D Create(string kind, LuaTable? opts = null)
    {
        var normalized = NormalizeKind(kind);
        return normalized switch
        {
            "group" => ApplyCommon(new Object2D(_app.S2D), opts),
            "sprite" => ApplyCommon(CreateSprite(RequiredString(opts, "path", normalized)), opts),
            "rect" => ApplyCommon(CreateRect(
                TableInt(opts, "width", TableInt(opts, "w", 32)),
                TableInt(opts, "height", TableInt(opts, "h", 32)),
                ReadColor(opts, Color.White)), opts),
            "label" => ApplyCommon(CreateLabel(TableString(opts, "text", ""), TableInt(opts, "size", FontManager.DefaultSize)), opts),
            "richlabel" => ApplyCommon(CreateRichLabel(
                TableString(opts, "text", TableString(opts, "markup", "")),
                TableInt(opts, "size", FontManager.DefaultSize)), opts),
            "panel" => ApplyCommon(CreatePanel(
                RequiredString(opts, "path", normalized),
                TableInt(opts, "border", 8),
                TableInt(opts, "width", 64),
                TableInt(opts, "height", 64)), opts),
            "flow" => ApplyCommon(CreateFlow(
                TableString(opts, "direction", "horizontal"),
                opts), opts),
            "progressbar" => ApplyCommon(CreateProgressBar(
                TableFloat(opts, "x", 0f),
                TableFloat(opts, "y", 0f),
                TableInt(opts, "width", TableInt(opts, "w", 120)),
                TableInt(opts, "height", TableInt(opts, "h", 12)),
                ReadNestedColor(opts, "bg", new Color(0.15f, 0.15f, 0.15f)),
                ReadNestedColor(opts, "fill", new Color(0.2f, 1f, 0.3f))), opts),
            "anim" or "animatedsprite" => ApplyCommon(CreateAnimatedSprite(
                RequiredString(opts, "path", normalized),
                TableInt(opts, "frameWidth", TableInt(opts, "fw", 32)),
                TableInt(opts, "frameHeight", TableInt(opts, "fh", 32))), opts),
            _ => throw new ArgumentException($"[Aegis|Lua] Tipo de componente desconhecido: {kind}.", nameof(kind))
        };
    }

    public SpriteNode CreateSprite(string path)
        => new(ResManager.LoadTexture(path), _app.S2D);

    public Bitmap CreateRect(int width, int height, Color color)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        var texture = new Texture2D(Renderer.GraphicsDevice, width, height);
        texture.SetData(Enumerable.Repeat(color, width * height).ToArray());
        return new Bitmap(texture, _app.S2D);
    }

    public Label CreateLabel(string text)
        => CreateLabel(text, FontManager.DefaultSize);

    public Label CreateLabel(string text, int size)
        => new(FontManager.LoadDefault(size), _app.S2D) { Text = text };

    public RichLabel CreateRichLabel(string markup)
        => CreateRichLabel(markup, FontManager.DefaultSize);

    public RichLabel CreateRichLabel(string markup, int size)
    {
        var richLabel = new RichLabel(_app.S2D)
        {
            Markup = markup,
            Font = FontManager.LoadDefault(size)
        };
        return richLabel;
    }

    public NineSlice CreatePanel(string path, int border, int width = 64, int height = 64)
    {
        var panel = new NineSlice(ResManager.LoadTexture(path), Math.Max(1, border), _app.S2D)
        {
            Width = width,
            Height = height
        };
        return panel;
    }

    public FlowContainer CreateFlow(string direction, LuaTable? opts = null)
        => new(
            direction,
            TableFloat(opts, "gap", 0f),
            TableFloat(opts, "padding", 0f),
            TableString(opts, "align", "start"),
            _app.S2D);

    public AnimatedSprite CreateAnimatedSprite(string path, int frameWidth, int frameHeight)
        => new(ResManager.LoadTexture(path), Math.Max(1, frameWidth), Math.Max(1, frameHeight), _app.S2D);

    public ProgressBar CreateProgressBar(float x, float y, int width, int height, Color bgColor, Color fillColor)
    {
        return new ProgressBar(width, height, bgColor, fillColor, _app.S2D)
        {
            X = x,
            Y = y
        };
    }

    public void SetProgressValue(Object2D barObject, float current, float max)
    {
        if (barObject is ProgressBar bar)
            bar.SetValue(current, max);
    }

    public void SetProgressColors(Object2D barObject, LuaTable opts)
    {
        if (barObject is not ProgressBar bar) return;

        var backgroundColor = opts["bg"] is LuaTable bgColor
            ? ReadColor(bgColor, new Color(0.15f, 0.15f, 0.15f))
            : (Color?)null;
        var fillColor = opts["fill"] is LuaTable fillColorTable
            ? ReadColor(fillColorTable, new Color(0.2f, 1f, 0.3f))
            : (Color?)null;
        bar.SetColors(backgroundColor, fillColor);
    }

    private static Object2D ApplyCommon(Object2D obj, LuaTable? opts)
    {
        if (opts is null) return obj;

        obj.X = TableFloat(opts, "x", obj.X);
        obj.Y = TableFloat(opts, "y", obj.Y);
        obj.Z = TableInt(opts, "z", obj.Z);
        obj.Alpha = TableFloat(opts, "alpha", obj.Alpha);
        obj.Visible = TableBool(opts, "visible", obj.Visible);

        var scale = TableFloat(opts, "scale", 1f);
        obj.ScaleX = TableFloat(opts, "scaleX", scale);
        obj.ScaleY = TableFloat(opts, "scaleY", scale);
        obj.Rotation = TableFloat(opts, "rotation", obj.Rotation);

        if (obj is Bitmap bitmap)
            bitmap.Pivot = new Vector2(TableFloat(opts, "pivotX", bitmap.Pivot.X), TableFloat(opts, "pivotY", bitmap.Pivot.Y));

        if (obj is Label label)
        {
            label.Pivot = new Vector2(TableFloat(opts, "pivotX", label.Pivot.X), TableFloat(opts, "pivotY", label.Pivot.Y));
            label.Color = ReadColor(opts, label.Color);
        }

        if (obj is RichLabel richLabel)
            richLabel.Pivot = new Vector2(TableFloat(opts, "pivotX", richLabel.Pivot.X), TableFloat(opts, "pivotY", richLabel.Pivot.Y));

        return obj;
    }

    private static string NormalizeKind(string kind)
        => kind.Trim().Replace("_", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();

    private static string RequiredString(LuaTable? table, string key, string component)
    {
        var value = TableString(table, key, "");
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"[Aegis|Lua] aegis.create('{component}') precisa de opts.{key}.");
        return value;
    }

    private static Color ReadNestedColor(LuaTable? table, string key, Color fallback)
        => table?[key] is LuaTable nested ? ReadColor(nested, fallback) : fallback;

    private static Color ReadColor(LuaTable? table, Color fallback)
    {
        var r = TableFloat(table, "r", TableFloat(table, "1", fallback.R / 255f));
        var g = TableFloat(table, "g", TableFloat(table, "2", fallback.G / 255f));
        var b = TableFloat(table, "b", TableFloat(table, "3", fallback.B / 255f));
        var a = TableFloat(table, "a", TableFloat(table, "4", fallback.A / 255f));
        return new Color(r, g, b, a);
    }

    private static int TableInt(LuaTable? table, string key, int fallback)
    {
        try { return table?[key] is null ? fallback : Convert.ToInt32(table[key]); }
        catch { return fallback; }
    }

    private static float TableFloat(LuaTable? table, string key, float fallback)
    {
        try { return table?[key] is null ? fallback : Convert.ToSingle(table[key]); }
        catch { return fallback; }
    }

    private static bool TableBool(LuaTable? table, string key, bool fallback)
    {
        try { return table?[key] is null ? fallback : Convert.ToBoolean(table[key]); }
        catch { return fallback; }
    }

    private static string TableString(LuaTable? table, string key, string fallback)
    {
        try { return table?[key]?.ToString() ?? fallback; }
        catch { return fallback; }
    }

}
