using Aegis.Display;
using Microsoft.Xna.Framework.Graphics;
using SpriteFontPlus;

namespace Aegis.Resource;

/// <summary>
/// Carrega fontes TTF/OTF em runtime e garante uma fonte padrao automatica.
/// Novo codigo deve poder criar labels sem carregar fonte manualmente.
/// </summary>
public static class FontManager
{
    private static GraphicsDevice _graphicsDevice = null!;
    private static bool _initialized;
    private static readonly Dictionary<(string, int), SpriteFont> Cache = new();

    public static string FontRoot { get; set; } = "res/fonts";
    public static int DefaultSize { get; set; } = 24;

    public static void Initialize(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _initialized = true;
        RichLabel.DefaultFont = Default;
    }

    public static SpriteFont Load(string file, int size)
    {
        EnsureInitialized();

        size = NormalizeSize(size);
        var path = ResolveFontPath(file);
        var key = (Path.GetFullPath(path), size);

        if (Cache.TryGetValue(key, out var hit))
            return hit;

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"[Aegis|Font] TTF/OTF nao encontrado: '{path}'. " +
                $"Coloque a fonte em '{Path.GetFullPath(FontRoot)}'.");
        }

        var font = Bake(path, size);
        Cache[key] = font;
        RichLabel.DefaultFont ??= font;
        return font;
    }

    /// <summary>
    /// Fonte padrao automatica. Preferencia:
    /// fonte ja carregada, fonte do projeto em res/fonts, fonte do sistema.
    /// </summary>
    public static SpriteFont? Default
    {
        get
        {
            foreach (var font in Cache.Values)
                return font;

            return LoadDefault();
        }
    }

    public static SpriteFont LoadDefault(int? size = null)
    {
        EnsureInitialized();

        var resolvedSize = NormalizeSize(size ?? DefaultSize);
        var path = ResolveDefaultFontPath();
        var key = ("__aegis_default__:" + Path.GetFullPath(path), resolvedSize);

        if (Cache.TryGetValue(key, out var hit))
            return hit;

        var font = Bake(path, resolvedSize);
        Cache[key] = font;
        RichLabel.DefaultFont = font;
        return font;
    }

    public static void Unload()
    {
        foreach (var font in Cache.Values)
            font.Texture?.Dispose();

        Cache.Clear();
        RichLabel.DefaultFont = null;
    }

    private static SpriteFont Bake(string path, int size)
    {
        var fontBytes = File.ReadAllBytes(path);
        return TtfFontBaker.Bake(
            fontBytes,
            size,
            2048,
            2048,
            new[]
            {
                CharacterRange.BasicLatin,
                CharacterRange.Latin1Supplement,
                new CharacterRange(' ', 'ÿ'),
            }
        ).CreateSpriteFont(_graphicsDevice);
    }

    private static string ResolveFontPath(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
            throw new ArgumentException("Caminho de fonte vazio.", nameof(file));

        return Path.IsPathRooted(file)
            ? file
            : Path.Combine(FontRoot, file);
    }

    private static string ResolveDefaultFontPath()
    {
        foreach (var candidate in ProjectFontCandidates())
        {
            var path = Path.Combine(FontRoot, candidate);
            if (File.Exists(path))
                return path;
        }

        foreach (var candidate in SystemFontCandidates())
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(
            "[Aegis|Font] Nenhuma fonte padrao encontrada. " +
            "Adicione Inter-Regular.ttf, NotoSans-Regular.ttf ou Roboto-Regular.ttf em res/fonts.");
    }

    private static IEnumerable<string> ProjectFontCandidates()
    {
        yield return "Inter-Regular.ttf";
        yield return "Inter.ttf";
        yield return "NotoSans-Regular.ttf";
        yield return "Roboto-Regular.ttf";
        yield return "OpenSans-Regular.ttf";
        yield return "Arial.ttf";

        if (!Directory.Exists(FontRoot))
            yield break;

        foreach (var file in Directory.EnumerateFiles(FontRoot, "*.ttf").OrderBy(Path.GetFileName))
            yield return Path.GetFileName(file);

        foreach (var file in Directory.EnumerateFiles(FontRoot, "*.otf").OrderBy(Path.GetFileName))
            yield return Path.GetFileName(file);
    }

    private static IEnumerable<string> SystemFontCandidates()
    {
        var windowsFonts = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "Fonts");

        yield return Path.Combine(windowsFonts, "segoeui.ttf");
        yield return Path.Combine(windowsFonts, "seguisb.ttf");
        yield return Path.Combine(windowsFonts, "arial.ttf");
        yield return "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
        yield return "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf";
        yield return "/System/Library/Fonts/Supplemental/Arial.ttf";
    }

    private static int NormalizeSize(int size)
        => Math.Clamp(size, 8, 96);

    private static void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("[Aegis|Font] FontManager.Initialize precisa ser chamado antes de usar fontes.");
    }
}
