using System.Globalization;
using System.Text.Json;

namespace Aegis.World;

public sealed class TiledMapDocument
{
    public int Width { get; private init; }
    public int Height { get; private init; }
    public int TileWidth { get; private init; }
    public int TileHeight { get; private init; }
    public string? SourcePath { get; private init; }
    public IReadOnlyDictionary<string, string> Properties { get; private init; } = new Dictionary<string, string>();
    public IReadOnlyList<TiledMapLayer> Layers { get; private init; } = Array.Empty<TiledMapLayer>();
    public IReadOnlyList<TiledMapObject> Objects { get; private init; } = Array.Empty<TiledMapObject>();
    public IReadOnlyList<TiledTilesetRef> Tilesets { get; private init; } = Array.Empty<TiledTilesetRef>();

    public static TiledMapDocument Load(string path)
        => Parse(File.ReadAllText(path), path);

    public static TiledMapDocument Parse(string json, string? sourcePath = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var layers = new List<TiledMapLayer>();
        var objects = new List<TiledMapObject>();
        var tilesets = new List<TiledTilesetRef>();

        if (root.TryGetProperty("tilesets", out var tilesetsEl) && tilesetsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var tileset in tilesetsEl.EnumerateArray())
            {
                tilesets.Add(new TiledTilesetRef(
                    GetInt(tileset, "firstgid"),
                    GetString(tileset, "source"),
                    GetString(tileset, "image"),
                    GetString(tileset, "name"),
                    GetInt(tileset, "tilewidth"),
                    GetInt(tileset, "tileheight"),
                    GetInt(tileset, "columns"),
                    GetInt(tileset, "tilecount"),
                    ReadProperties(tileset)));
            }
        }

        if (root.TryGetProperty("layers", out var layersEl) && layersEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var layer in layersEl.EnumerateArray())
            {
                var type = GetString(layer, "type") ?? string.Empty;
                var name = GetString(layer, "name") ?? "Layer";
                var mapLayer = new TiledMapLayer(
                    name,
                    type,
                    GetInt(layer, "width"),
                    GetInt(layer, "height"),
                    GetBool(layer, "visible", true),
                    GetFloat(layer, "opacity", 1f),
                    ReadProperties(layer));
                layers.Add(mapLayer);

                if (!type.Equals("objectgroup", StringComparison.OrdinalIgnoreCase)
                    || !layer.TryGetProperty("objects", out var objectsEl)
                    || objectsEl.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var obj in objectsEl.EnumerateArray())
                {
                    objects.Add(new TiledMapObject(
                        mapLayer.Name,
                        GetInt(obj, "id"),
                        GetString(obj, "name") ?? string.Empty,
                        GetString(obj, "type") ?? string.Empty,
                        GetFloat(obj, "x"),
                        GetFloat(obj, "y"),
                        GetFloat(obj, "width"),
                        GetFloat(obj, "height"),
                        GetFloat(obj, "rotation"),
                        GetBool(obj, "visible", true),
                        ReadProperties(obj)));
                }
            }
        }

        return new TiledMapDocument
        {
            Width = GetInt(root, "width"),
            Height = GetInt(root, "height"),
            TileWidth = GetInt(root, "tilewidth"),
            TileHeight = GetInt(root, "tileheight"),
            SourcePath = sourcePath,
            Properties = ReadProperties(root),
            Layers = layers,
            Objects = objects,
            Tilesets = tilesets,
        };
    }

    private static Dictionary<string, string> ReadProperties(JsonElement owner)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!owner.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var prop in properties.EnumerateArray())
        {
            var name = GetString(prop, "name");
            if (string.IsNullOrWhiteSpace(name) || !prop.TryGetProperty("value", out var value))
                continue;

            result[name] = ToPropertyString(value);
        }

        return result;
    }

    private static string ToPropertyString(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => value.GetRawText(),
            _ => value.ToString(),
        };

    private static string? GetString(JsonElement el, string name)
        => el.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int GetInt(JsonElement el, string name, int fallback = 0)
        => el.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : fallback;

    private static bool GetBool(JsonElement el, string name, bool fallback)
        => el.TryGetProperty(name, out var value) && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            ? value.GetBoolean()
            : fallback;

    private static float GetFloat(JsonElement el, string name, float fallback = 0f)
        => el.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? float.Parse(value.GetRawText(), CultureInfo.InvariantCulture)
            : fallback;
}

public sealed record TiledMapLayer(
    string Name,
    string Type,
    int Width,
    int Height,
    bool Visible,
    float Opacity,
    IReadOnlyDictionary<string, string> Properties);

public sealed record TiledMapObject(
    string LayerName,
    int Id,
    string Name,
    string Type,
    float X,
    float Y,
    float Width,
    float Height,
    float Rotation,
    bool Visible,
    IReadOnlyDictionary<string, string> Properties);

public sealed record TiledTilesetRef(
    int FirstGid,
    string? Source,
    string? Image,
    string? Name,
    int TileWidth,
    int TileHeight,
    int Columns,
    int TileCount,
    IReadOnlyDictionary<string, string> Properties);
