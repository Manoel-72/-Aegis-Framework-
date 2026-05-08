using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Aegis;

/// <summary>
/// Cache de texturas por caminho.
/// Caminhos relativos são resolvidos a partir do diretório do jogo ativo.
/// </summary>
public static class ResManager
{
    private static readonly Dictionary<string, Texture2D> _textures = new();
    private static string _baseDir = "";

    /// Definido pelo LuaBridge.Load() com o diretório do jogo atual
    public static void SetBaseDir(string dir) => _baseDir = dir;

    /// Carrega uma textura (ou devolve do cache se já carregada)
    public static Texture2D LoadTex(string path)
    {
        string full = Resolve(path);

        if (_textures.TryGetValue(full, out var cached)) return cached;

        if (!File.Exists(full))
        {
            Console.WriteLine($"[ResManager] Textura não encontrada: {full}");
            return CreateFallback();
        }

        var tex = LoadTexture(full);
        SetTextureFilter(tex, TextureFilter.Point); // pixel art por padrão
        _textures[full] = tex;
        Console.WriteLine($"[ResManager] Carregado: {full}");
        return tex;
    }

    /// Define filtro de uma textura já carregada
    public static void SetFilter(string path, TextureFilter filter)
    {
        string full = Resolve(path);
        if (_textures.TryGetValue(full, out var tex))
            SetTextureFilter(tex, filter);
    }

    /// Descarrega todas as texturas da memória (chamado ao fechar a janela)
    public static void UnloadAll()
    {
        foreach (var t in _textures.Values) UnloadTexture(t);
        _textures.Clear();
        Console.WriteLine("[ResManager] Todas as texturas descarregadas.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static string Resolve(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(_baseDir, path);

    /// Textura de placeholder 8x8 magenta — aparece se o arquivo não existir
    private static Texture2D CreateFallback()
    {
        const int S = 8;
        var img = GenImageColor(S, S, Color.Magenta);
        var tex = LoadTextureFromImage(img);
        UnloadImage(img);
        return tex;
    }
}
