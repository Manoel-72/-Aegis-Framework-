using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Resource;

/// <summary>
/// Gerenciador de assets com cache por caminho.
/// Carrega PNG/JPG/JPEG da pasta res/ do projeto.
/// </summary>
public static class ResManager
{
    private static GraphicsDevice _gd = null!;
    private static ContentManager _content = null!;

    private static readonly Dictionary<string, Texture2D> _textures = new();
    private static readonly Dictionary<string, SpriteFont> _fonts = new();

    public static string ResRoot { get; set; } = "res";

    public static void Initialize(GraphicsDevice gd, ContentManager content)
    { _gd = gd; _content = content; }

    public static Texture2D LoadTexture(string relativePath)
    {
        var key = NormalizeKey(relativePath);
        if (_textures.TryGetValue(key, out var hit)) return hit;

        var full = ResolveResPath(key);
        if (!File.Exists(full))
            throw new FileNotFoundException(
                $"[Aegis|Res] Texture not found: '{full}'\n" +
                $"Coloque PNG/JPG/JPEG na pasta '{Path.GetFullPath(ResRoot)}'.");

        using var stream = File.OpenRead(full);
        var tex = Texture2D.FromStream(_gd, stream);
        _textures[key] = tex;
        return tex;
    }

    private static string NormalizeKey(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Caminho de asset vazio.", nameof(relativePath));

        if (Path.IsPathRooted(relativePath))
            throw new InvalidOperationException($"[Aegis|Res] Use caminho relativo dentro da pasta res/: '{relativePath}'");

        return relativePath.Replace('\\', '/').TrimStart('/');
    }

    private static string ResolveResPath(string key)
    {
        var root = Path.GetFullPath(ResRoot);
        var full = Path.GetFullPath(Path.Combine(root, key));

        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"[Aegis|Res] Caminho fora da pasta res/: '{key}'");

        return full;
    }

    private static Texture2D? _pixel;
    public static Texture2D Pixel
    {
        get
        {
            if (_pixel is not null) return _pixel;
            _pixel = new Texture2D(_gd, 1, 1);
            _pixel.SetData(new[] { Microsoft.Xna.Framework.Color.White });
            return _pixel;
        }
    }

    public static void Unload()
    {
        foreach (var t in _textures.Values) t.Dispose();
        _textures.Clear();
        _pixel?.Dispose();
        _pixel = null;
    }
}
