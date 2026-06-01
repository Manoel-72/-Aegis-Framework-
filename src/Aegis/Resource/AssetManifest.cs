namespace Aegis.Resource;

public enum AssetKind
{
    Unknown,
    Sprite,
    Audio,
    Font,
    Tilemap,
    Data
}

public sealed record AssetManifestEntry(
    AssetKind Kind,
    string RelativePath,
    string FullPath,
    long SizeBytes);

public sealed class AssetManifest
{
    private readonly List<AssetManifestEntry> _entries = new();

    public IReadOnlyList<AssetManifestEntry> Entries => _entries;

    public int SpriteCount => Count(AssetKind.Sprite);
    public int AudioCount => Count(AssetKind.Audio);
    public int FontCount => Count(AssetKind.Font);
    public int TilemapCount => Count(AssetKind.Tilemap);
    public int DataCount => Count(AssetKind.Data);

    public static AssetManifest Build(string projectDir)
    {
        var manifest = new AssetManifest();
        var root = Path.GetFullPath(projectDir);
        var res = Path.Combine(root, "res");
        if (!Directory.Exists(res))
            return manifest;

        foreach (var file in Directory.EnumerateFiles(res, "*", SearchOption.AllDirectories))
        {
            var full = Path.GetFullPath(file);
            var rel = Path.GetRelativePath(res, full).Replace('\\', '/');
            var info = new FileInfo(full);
            manifest._entries.Add(new AssetManifestEntry(GetKind(rel), rel, full, info.Length));
        }

        return manifest;
    }

    public IReadOnlyList<AssetManifestEntry> ByKind(AssetKind kind)
        => _entries.Where(e => e.Kind == kind).ToArray();

    private int Count(AssetKind kind)
        => _entries.Count(e => e.Kind == kind);

    private static AssetKind GetKind(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        var ext = Path.GetExtension(normalized).ToLowerInvariant();
        var topFolder = normalized.Split('/', 2)[0];

        if (topFolder.Equals("sprites", StringComparison.OrdinalIgnoreCase)
            || ext is ".png" or ".jpg" or ".jpeg")
            return AssetKind.Sprite;

        if (topFolder.Equals("audio", StringComparison.OrdinalIgnoreCase)
            || ext is ".wav" or ".ogg" or ".mp3")
            return AssetKind.Audio;

        if (topFolder.Equals("fonts", StringComparison.OrdinalIgnoreCase)
            || ext is ".ttf" or ".otf")
            return AssetKind.Font;

        if (topFolder.Equals("tilemaps", StringComparison.OrdinalIgnoreCase)
            || ext is ".tmj")
            return AssetKind.Tilemap;

        if (ext is ".json" or ".txt" or ".csv" or ".toml" or ".cfg")
            return AssetKind.Data;

        return AssetKind.Unknown;
    }
}
