using Microsoft.Xna.Framework.Graphics;
using SpriteFontPlus;

namespace Aegis.Resource;

/// <summary>
/// Carrega fontes TTF/OTF em runtime e gera SpriteFont sem Content Pipeline.
/// Usa SpriteFontPlus (TtfFontBaker) para baking em textura.
///
/// Cache por (caminho, tamanho) — cada tamanho gera uma textura separada.
/// Arquivos devem estar em res/fonts/
///
/// Uso Lua:
///   aegis.loadFont("roboto.ttf", 24)   → retorna handle da font
///   aegis.setFont(label, font)
///   aegis.setFontRich(richLabel, font)
/// </summary>
public static class FontManager
{
    private static GraphicsDevice _gd = null!;

    // Chave: (arquivo, tamanho)
    private static readonly Dictionary<(string, int), SpriteFont> _cache = new();

    public static string FontRoot { get; set; } = "res/fonts";

    public static void Initialize(GraphicsDevice gd) => _gd = gd;

    // ── Carga pública ─────────────────────────────────────────────────
    public static SpriteFont Load(string file, int size)
    {
        var key = (file, size);
        if (_cache.TryGetValue(key, out var hit)) return hit;

        var path = Path.Combine(FontRoot, file);
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"[Aegis|Font] TTF não encontrado: '{path}'\n" +
                $"Coloque o arquivo em res/fonts/");

        var fontBytes = File.ReadAllBytes(path);

        // Bake: gera textura atlas + SpriteFont em runtime
        var font = TtfFontBaker.Bake(
            fontBytes,
            size,
            1024, 1024,           // tamanho do atlas
            new[]
            {
                CharacterRange.BasicLatin,
                CharacterRange.Latin1Supplement,
                // Suporte a caracteres PT-BR (acentos)
                new CharacterRange(' ', 'ÿ'),
            }
        ).CreateSpriteFont(_gd);

        _cache[key] = font;
        return font;
    }

    // ── Fonte padrão embutida (fallback quando nenhuma TTF for carregada) ──
    /// Retorna a primeira fonte em cache, ou null se nenhuma foi carregada.
    public static SpriteFont? Default
    {
        get
        {
            foreach (var f in _cache.Values) return f;
            return null;
        }
    }

    public static void Unload()
    {
        foreach (var f in _cache.Values) f.Texture?.Dispose();
        _cache.Clear();
    }
}
