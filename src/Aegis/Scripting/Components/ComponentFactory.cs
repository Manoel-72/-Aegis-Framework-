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
/// Objetos podem ser criados na cena de mundo (S2D) ou na camada UI (Ui2D).
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
        var root = ResolveRoot(opts);
        return normalized switch
        {
            "group" => ApplyCommon(new Object2D(root), opts),
            "sprite" => ApplyCommon(CreateSpriteOn(RequiredString(opts, "path", normalized), root), opts),
            "rect" => ApplyCommon(CreateRectOn(
                TableInt(opts, "width", TableInt(opts, "w", 32)),
                TableInt(opts, "height", TableInt(opts, "h", 32)),
                ReadColor(opts, Color.White), root), opts),
            "label" => ApplyCommon(CreateLabelOn(TableString(opts, "text", ""), TableInt(opts, "size", FontManager.DefaultSize), root), opts),
            "richlabel" => ApplyCommon(CreateRichLabelOn(
                TableString(opts, "text", TableString(opts, "markup", "")),
                TableInt(opts, "size", FontManager.DefaultSize), root), opts),
            "panel" => ApplyCommon(CreatePanelOn(
                RequiredString(opts, "path", normalized),
                TableInt(opts, "border", 8),
                TableInt(opts, "width", 64),
                TableInt(opts, "height", 64), root), opts),
            "flow" => ApplyCommon(CreateFlowOn(TableString(opts, "direction", "horizontal"), opts, root), opts),
            "progressbar" => ApplyCommon(CreateProgressBarOn(
                TableFloat(opts, "x", 0f),
                TableFloat(opts, "y", 0f),
                TableInt(opts, "width", TableInt(opts, "w", 120)),
                TableInt(opts, "height", TableInt(opts, "h", 12)),
                ReadNestedColor(opts, "bg", new Color(0.15f, 0.15f, 0.15f)),
                ReadNestedColor(opts, "fill", new Color(0.2f, 1f, 0.3f)), root), opts),
            "anim" or "animatedsprite" => ApplyCommon(CreateAnimatedSpriteOn(
                RequiredString(opts, "path", normalized),
                TableInt(opts, "frameWidth", TableInt(opts, "fw", 32)),
                TableInt(opts, "frameHeight", TableInt(opts, "fh", 32)), root), opts),
            _ => throw new ArgumentException($"[Aegis|Lua] Tipo de componente desconhecido: {kind}.", nameof(kind))
        };
    }

    public SpriteNode CreateSprite(string path, bool ui = false)
        => CreateSpriteOn(path, ResolveRoot(ui));

    public Bitmap CreateRect(int width, int height, Color color, bool ui = false)
        => CreateRectOn(width, height, color, ResolveRoot(ui));

    public Label CreateLabel(string text, bool ui = false)
        => CreateLabel(text, FontManager.DefaultSize, ui);

    public Label CreateLabel(string text, int size, bool ui = false)
        => CreateLabelOn(text, size, ResolveRoot(ui));

    public RichLabel CreateRichLabel(string markup, bool ui = false)
        => CreateRichLabel(markup, FontManager.DefaultSize, ui);

    public RichLabel CreateRichLabel(string markup, int size, bool ui = false)
        => CreateRichLabelOn(markup, size, ResolveRoot(ui));

    public NineSlice CreatePanel(string path, int border, bool ui = false)
        => CreatePanel(path, border, 64, 64, ui);

    public NineSlice CreatePanel(string path, int border, int width, int height, bool ui = false)
        => CreatePanelOn(path, border, width, height, ResolveRoot(ui));

    public FlowContainer CreateFlow(string direction, LuaTable? opts = null, bool ui = false)
        => CreateFlowOn(direction, opts, ResolveRoot(opts, ui));

    public AnimatedSprite CreateAnimatedSprite(string path, int frameWidth, int frameHeight, bool ui = false)
        => CreateAnimatedSpriteOn(path, frameWidth, frameHeight, ResolveRoot(ui));

    public ProgressBar CreateProgressBar(float x, float y, int width, int height, Color bgColor, Color fillColor, bool ui = false)
        => CreateProgressBarOn(x, y, width, height, bgColor, fillColor, ResolveRoot(ui));

    private SpriteNode CreateSpriteOn(string path, Scene2D root)
        => new(ResManager.LoadTexture(path), root);

    private Label CreateLabelOn(string text, int size, Scene2D root)
        => new(FontManager.LoadDefault(size), root) { Text = text };

    private RichLabel CreateRichLabelOn(string markup, int size, Scene2D root)
        => new RichLabel(root) { Markup = markup, Font = FontManager.LoadDefault(size) };

    private NineSlice CreatePanelOn(string path, int border, int width, int height, Scene2D root)
        => new NineSlice(ResManager.LoadTexture(path), Math.Max(1, border), root) { Width = width, Height = height };

    private AnimatedSprite CreateAnimatedSpriteOn(string path, int frameWidth, int frameHeight, Scene2D root)
        => new(ResManager.LoadTexture(path), Math.Max(1, frameWidth), Math.Max(1, frameHeight), root);

    private ProgressBar CreateProgressBarOn(float x, float y, int width, int height, Color bgColor, Color fillColor, Scene2D root)
        => new ProgressBar(width, height, bgColor, fillColor, root) { X = x, Y = y };

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

    public Scene2D ResolveRoot(LuaTable? opts, bool ui = false)
    {
        if (opts is not null && IsUiLayer(opts)) return _app.Ui2D;
        return ui ? _app.Ui2D : _app.S2D;
    }

    public Scene2D ResolveRoot(bool ui)
        => ui ? _app.Ui2D : _app.S2D;

    public static bool IsUiLayer(LuaTable? opts)
    {
        if (opts is null) return false;
        if (TableBool(opts, "hud", false)) return true;
        var layer = TableString(opts, "layer", "");
        return layer.Equals("ui", StringComparison.OrdinalIgnoreCase)
            || layer.Equals("hud", StringComparison.OrdinalIgnoreCase);
    }

    private Bitmap CreateRectOn(int width, int height, Color color, Scene2D root)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        var texture = new Texture2D(Renderer.GraphicsDevice, width, height);
        texture.SetData(Enumerable.Repeat(color, width * height).ToArray());
        return new Bitmap(texture, root);
    }

    private FlowContainer CreateFlowOn(string direction, LuaTable? opts, Scene2D root)
        => new(
            direction,
            TableFloat(opts, "gap", 0f),
            TableFloat(opts, "padding", 0f),
            TableString(opts, "align", "start"),
            root);

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
