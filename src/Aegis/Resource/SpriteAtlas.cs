using System.Text.Json;
using Aegis.Core;
using Microsoft.Xna.Framework;

namespace Aegis.Resource;

/// <summary>
/// Atlas de spritesheet por nome, compatível com JSON exportado pelo Aseprite.
/// Formato esperado: { "frames": { "run_00": { "frame": {"x":0,"y":0,"w":32,"h":32} } } }
/// </summary>
public sealed class SpriteAtlas
{
    private readonly Dictionary<string, Rectangle> _frames = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<string> FrameNames => _frames.Keys;

    public static SpriteAtlas Load(string jsonPath)
    {
        var atlas = new SpriteAtlas();
        var full = ResolveResPath(jsonPath);
        if (!File.Exists(full)) throw new FileNotFoundException($"[Aegis|Atlas] Atlas JSON não encontrado: '{full}'.");

        using var doc = JsonDocument.Parse(File.ReadAllText(full));
        if (!doc.RootElement.TryGetProperty("frames", out var frames))
            throw new InvalidDataException("[Aegis|Atlas] JSON inválido: propriedade 'frames' ausente.");

        if (frames.ValueKind == JsonValueKind.Object)
        {
            foreach (var named in frames.EnumerateObject())
            {
                if (TryReadFrame(named.Value, out var rect)) atlas._frames[named.Name] = rect;
                else AegisLog.Warn("Atlas", $"Frame inválido ignorado: {named.Name}");
            }
        }
        else if (frames.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in frames.EnumerateArray())
            {
                var name = ReadName(item);
                if (!string.IsNullOrWhiteSpace(name) && TryReadFrame(item, out var rect)) atlas._frames[name] = rect;
            }
        }

        if (atlas._frames.Count == 0)
            throw new InvalidDataException("[Aegis|Atlas] Nenhum frame válido encontrado no atlas.");
        return atlas;
    }

    public Rectangle GetFrame(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome de frame vazio.", nameof(name));
        if (_frames.TryGetValue(name, out var rect)) return rect;
        throw new KeyNotFoundException($"[Aegis|Atlas] Frame '{name}' não encontrado. Frames disponíveis: {string.Join(", ", _frames.Keys.Take(12))}");
    }

    public bool TryGetFrame(string name, out Rectangle frame) => _frames.TryGetValue(name, out frame);

    private static bool TryReadFrame(JsonElement item, out Rectangle rect)
    {
        rect = Rectangle.Empty;
        if (!item.TryGetProperty("frame", out var frame)) return false;
        var x = ReadInt(frame, "x");
        var y = ReadInt(frame, "y");
        var w = ReadInt(frame, "w");
        var h = ReadInt(frame, "h");
        if (w <= 0 || h <= 0) return false;
        rect = new Rectangle(x, y, w, h);
        return true;
    }

    private static string? ReadName(JsonElement item)
    {
        if (item.TryGetProperty("filename", out var filename)) return filename.GetString();
        if (item.TryGetProperty("name", out var name)) return name.GetString();
        return null;
    }

    private static int ReadInt(JsonElement obj, string property)
        => obj.TryGetProperty(property, out var value) && value.TryGetInt32(out var result) ? result : 0;

    private static string ResolveResPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) throw new ArgumentException("Caminho de atlas vazio.", nameof(relativePath));
        if (Path.IsPathRooted(relativePath)) throw new InvalidOperationException($"[Aegis|Atlas] Use caminho relativo dentro da pasta res/: '{relativePath}'");

        var key = relativePath.Replace('\\', '/').TrimStart('/');
        var root = Path.GetFullPath(ResManager.ResRoot);
        var full = Path.GetFullPath(Path.Combine(root, key));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"[Aegis|Atlas] Caminho fora da pasta res/: '{relativePath}'");
        return full;
    }
}
