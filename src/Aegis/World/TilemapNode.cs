using Aegis.Core;
using Aegis.Display;
using Aegis.Physics;
using Aegis.Resource;
using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;

namespace Aegis.World;

/// <summary>
/// Tilemap para mapas grandes. Desenha apenas tiles visíveis na câmera.
/// Suporta Tiled JSON ortogonal com layers tilelayer + data CSV/array.
/// </summary>
public sealed class TilemapNode : Object2D
{
    public int MapWidth { get; private set; }
    public int MapHeight { get; private set; }
    public int TileWidth { get; private set; } = 16;
    public int TileHeight { get; private set; } = 16;
    public int PixelWidth => MapWidth * TileWidth;
    public int PixelHeight => MapHeight * TileHeight;

    private readonly List<TileLayer> _layers = new();
    private readonly List<TilesetInfo> _tilesets = new();
    private readonly List<Collider> _generatedColliders = new();
    private readonly List<Object2D> _generatedColliderNodes = new();
    private readonly HashSet<int> _tiledSolidGids = new();

    public int GeneratedColliderCount => _generatedColliders.Count;
    public bool CameraCulling { get; set; } = true;
    public int CullingPaddingTiles { get; set; } = 2;

    private sealed class TileLayer
    {
        public string Name = "Layer";
        public int Width;
        public int Height;
        public int[] Data = Array.Empty<int>();
        public bool Visible = true;
        public float Opacity = 1f;
    }

    private sealed class TilesetInfo
    {
        public int FirstGid;
        public int TileWidth;
        public int TileHeight;
        public int Columns;
        public int TileCount;
        public Texture2D? Texture;
    }

    private TilemapNode(Scene2D? parent = null) => parent?.AddChild(this);

    public static TilemapNode LoadTiledJson(string path, Scene2D? parent = null)
    {
        var map = new TilemapNode(parent);
        map.LoadFromTiled(path);
        return map;
    }

    public static TilemapNode GenerateProcedural(
        string tilesetPath,
        int width,
        int height,
        int tileW,
        int tileH,
        int seed,
        float scale = 0.08f,
        float water = 0.35f,
        float sand = 0.45f,
        float grass = 0.72f,
        Scene2D? parent = null)
    {
        var map = new TilemapNode(parent)
        {
            MapWidth = Math.Max(1, width),
            MapHeight = Math.Max(1, height),
            TileWidth = Math.Max(1, tileW),
            TileHeight = Math.Max(1, tileH)
        };

        var tex = ResManager.LoadTexture(tilesetPath);
        map._tilesets.Add(new TilesetInfo
        {
            FirstGid = 1,
            TileWidth = map.TileWidth,
            TileHeight = map.TileHeight,
            Columns = Math.Max(1, tex.Width / map.TileWidth),
            TileCount = Math.Max(1, (tex.Width / map.TileWidth) * (tex.Height / map.TileHeight)),
            Texture = tex
        });

        var noise = new PerlinNoise(seed);
        var data = new int[map.MapWidth * map.MapHeight];
        for (var y = 0; y < map.MapHeight; y++)
        for (var x = 0; x < map.MapWidth; x++)
        {
            var n = noise.Fractal(x * scale, y * scale, 4);
            // gids 1..4: água, areia, grama, pedra/montanha.
            data[y * map.MapWidth + x] = n < water ? 1 : n < sand ? 2 : n < grass ? 3 : 4;
        }

        map._layers.Add(new TileLayer
        {
            Name = "procedural",
            Width = map.MapWidth,
            Height = map.MapHeight,
            Data = data,
            Visible = true,
            Opacity = 1f
        });

        return map;
    }

    private void LoadFromTiled(string relativePath)
    {
        var full = ResolveSafe(relativePath);
        using var doc = JsonDocument.Parse(File.ReadAllText(full));
        var root = doc.RootElement;

        MapWidth = root.GetProperty("width").GetInt32();
        MapHeight = root.GetProperty("height").GetInt32();
        TileWidth = root.GetProperty("tilewidth").GetInt32();
        TileHeight = root.GetProperty("tileheight").GetInt32();

        var mapDir = Path.GetDirectoryName(full) ?? ResManager.ResRoot;
        if (root.TryGetProperty("tilesets", out var tilesets))
        {
            foreach (var ts in tilesets.EnumerateArray())
            {
                // Suporta tileset embutido do Tiled. Tileset externo .tsx fica para depois.
                if (!ts.TryGetProperty("image", out var imageEl)) continue;
                var imagePath = imageEl.GetString() ?? string.Empty;
                var imageFull = Path.GetFullPath(Path.Combine(mapDir, imagePath));
                var resRoot = Path.GetFullPath(ResManager.ResRoot);
                if (!imageFull.StartsWith(resRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"[Aegis|Tilemap] Tileset fora de res/: {imagePath}");

                var relImage = Path.GetRelativePath(resRoot, imageFull).Replace('\\', '/');
                var tex = ResManager.LoadTexture(relImage);
                var tw = ts.TryGetProperty("tilewidth", out var twEl) ? twEl.GetInt32() : TileWidth;
                var th = ts.TryGetProperty("tileheight", out var thEl) ? thEl.GetInt32() : TileHeight;
                var columns = ts.TryGetProperty("columns", out var colEl) ? Math.Max(1, colEl.GetInt32()) : Math.Max(1, tex.Width / tw);
                var count = ts.TryGetProperty("tilecount", out var countEl) ? Math.Max(1, countEl.GetInt32()) : Math.Max(1, columns * (tex.Height / th));

                var firstGid = ts.GetProperty("firstgid").GetInt32();
                _tilesets.Add(new TilesetInfo
                {
                    FirstGid = firstGid,
                    TileWidth = tw,
                    TileHeight = th,
                    Columns = columns,
                    TileCount = count,
                    Texture = tex
                });

                if (ts.TryGetProperty("tiles", out var tileProps))
                {
                    foreach (var tile in tileProps.EnumerateArray())
                    {
                        if (!tile.TryGetProperty("id", out var idEl)) continue;
                        if (!tile.TryGetProperty("properties", out var propsEl)) continue;
                        foreach (var prop in propsEl.EnumerateArray())
                        {
                            var name = prop.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                            if (!string.Equals(name, "solid", StringComparison.OrdinalIgnoreCase)) continue;
                            var isSolid = prop.TryGetProperty("value", out var valEl) && valEl.ValueKind == JsonValueKind.True;
                            if (isSolid) _tiledSolidGids.Add(firstGid + idEl.GetInt32());
                        }
                    }
                }
            }
        }

        if (_tilesets.Count == 0)
            throw new InvalidOperationException("[Aegis|Tilemap] Tiled JSON precisa de pelo menos um tileset embutido com image.");

        foreach (var layer in root.GetProperty("layers").EnumerateArray())
        {
            if (!layer.TryGetProperty("type", out var type) || type.GetString() != "tilelayer") continue;
            var w = layer.TryGetProperty("width", out var wEl) ? wEl.GetInt32() : MapWidth;
            var h = layer.TryGetProperty("height", out var hEl) ? hEl.GetInt32() : MapHeight;
            var data = ReadLayerData(layer.GetProperty("data"), w * h);

            _layers.Add(new TileLayer
            {
                Name = layer.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "Layer" : "Layer",
                Width = w,
                Height = h,
                Data = data,
                Visible = !layer.TryGetProperty("visible", out var visEl) || visEl.GetBoolean(),
                Opacity = layer.TryGetProperty("opacity", out var opEl) ? Math.Clamp(opEl.GetSingle(), 0f, 1f) : 1f
            });
        }
    }

    private static int[] ReadLayerData(JsonElement dataEl, int expected)
    {
        var data = new int[expected];
        if (dataEl.ValueKind == JsonValueKind.Array)
        {
            var i = 0;
            foreach (var v in dataEl.EnumerateArray())
            {
                if (i >= data.Length) break;
                data[i++] = v.GetInt32();
            }
            return data;
        }

        if (dataEl.ValueKind == JsonValueKind.String)
        {
            var raw = dataEl.GetString() ?? string.Empty;
            var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = 0; i < Math.Min(parts.Length, data.Length); i++)
                int.TryParse(parts[i], out data[i]);
        }
        return data;
    }

    public int GetTile(int layerIndex, int x, int y)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return 0;
        var layer = _layers[layerIndex];
        if (x < 0 || y < 0 || x >= layer.Width || y >= layer.Height) return 0;
        return layer.Data[y * layer.Width + x];
    }

    public void SetTile(int layerIndex, int x, int y, int gid)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count) return;
        var layer = _layers[layerIndex];
        if (x < 0 || y < 0 || x >= layer.Width || y >= layer.Height) return;
        layer.Data[y * layer.Width + x] = Math.Max(0, gid);
    }



    public bool AnyLayerHasSolidGid(int x, int y, HashSet<int> solidGids)
    {
        if (x < 0 || y < 0) return false;
        foreach (var layer in _layers)
        {
            if (x >= layer.Width || y >= layer.Height) continue;
            var gid = layer.Data[y * layer.Width + x] & 0x1FFFFFFF;
            if (gid > 0 && solidGids.Contains(gid)) return true;
        }
        return false;
    }

    public int BuildColliders(IEnumerable<int> solidGids, bool merge = true, int layerMask = 1, int collisionMask = ~0, bool useTiledProperty = false)
    {
        ClearColliders();
        var set = new HashSet<int>(solidGids.Select(g => g & 0x1FFFFFFF));
        if (useTiledProperty) foreach (var gid in _tiledSolidGids) set.Add(gid);
        if (set.Count == 0) return 0;

        var solid = new bool[MapWidth, MapHeight];
        for (int y = 0; y < MapHeight; y++)
        for (int x = 0; x < MapWidth; x++)
            solid[x, y] = AnyLayerHasSolidGid(x, y, set);

        if (merge) BuildMergedRectColliders(solid, layerMask, collisionMask);
        else
        {
            for (int y = 0; y < MapHeight; y++)
            for (int x = 0; x < MapWidth; x++)
                if (solid[x, y]) AddTileColliderRect(x, y, 1, 1, layerMask, collisionMask);
        }

        AegisLog.Info("Tilemap", $"Colliders gerados: {_generatedColliders.Count} (merge={merge}).");
        return _generatedColliders.Count;
    }

    public void ClearColliders()
    {
        foreach (var c in _generatedColliders)
            CollisionSystem.Instance.Unregister(c);
        _generatedColliders.Clear();

        foreach (var node in _generatedColliderNodes.ToArray())
            node.RemoveFromParent();
        _generatedColliderNodes.Clear();
    }

    private void BuildMergedRectColliders(bool[,] solid, int layerMask, int collisionMask)
    {
        var used = new bool[MapWidth, MapHeight];
        for (int y = 0; y < MapHeight; y++)
        for (int x = 0; x < MapWidth; x++)
        {
            if (!solid[x, y] || used[x, y]) continue;

            int w = 1;
            while (x + w < MapWidth && solid[x + w, y] && !used[x + w, y]) w++;

            int h = 1;
            bool canGrow = true;
            while (y + h < MapHeight && canGrow)
            {
                for (int xx = x; xx < x + w; xx++)
                {
                    if (!solid[xx, y + h] || used[xx, y + h])
                    {
                        canGrow = false;
                        break;
                    }
                }
                if (canGrow) h++;
            }

            for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                used[xx, yy] = true;

            AddTileColliderRect(x, y, w, h, layerMask, collisionMask);
        }
    }

    private void AddTileColliderRect(int tileX, int tileY, int tilesW, int tilesH, int layerMask, int collisionMask)
    {
        var node = new Object2D
        {
            X = X + tileX * TileWidth,
            Y = Y + tileY * TileHeight,
            Visible = false
        };
        Parent?.AddChild(node);

        var collider = new Collider(node, tilesW * TileWidth, tilesH * TileHeight)
        {
            Layer = layerMask,
            Mask = collisionMask,
            IsTrigger = false
        };
        CollisionSystem.Instance.Register(collider);
        _generatedColliderNodes.Add(node);
        _generatedColliders.Add(collider);
    }

    public override void Draw(SpriteBatch sb, float inheritedAlpha = 1f)
    {
        if (!Visible || _layers.Count == 0 || _tilesets.Count == 0) return;

        var cam = Camera2D.Instance;
        var zoom = MathF.Max(0.1f, cam.Zoom);
        var startX = 0;
        var startY = 0;
        var endX = MapWidth - 1;
        var endY = MapHeight - 1;

        if (CameraCulling && cam.Active)
        {
            var pad = Math.Max(0, CullingPaddingTiles);
            startX = Math.Max(0, (int)MathF.Floor(cam.X / TileWidth) - pad);
            startY = Math.Max(0, (int)MathF.Floor(cam.Y / TileHeight) - pad);
            endX = Math.Min(MapWidth - 1, (int)MathF.Ceiling((cam.X + cam.ViewWidth / zoom) / TileWidth) + pad);
            endY = Math.Min(MapHeight - 1, (int)MathF.Ceiling((cam.Y + cam.ViewHeight / zoom) / TileHeight) + pad);
        }

        var world = GetWorldMatrix();
        var mapOffset = new Vector2(world.M41, world.M42);
        var alpha = Alpha * inheritedAlpha;

        foreach (var layer in _layers)
        {
            if (!layer.Visible) continue;
            var layerAlpha = alpha * layer.Opacity;
            for (var y = startY; y <= Math.Min(endY, layer.Height - 1); y++)
            for (var x = startX; x <= Math.Min(endX, layer.Width - 1); x++)
            {
                var rawGid = layer.Data[y * layer.Width + x];
                // Remove flags de flip do Tiled; flip avançado fica para outra versão.
                var gid = rawGid & 0x1FFFFFFF;
                if (gid <= 0) continue;

                var ts = FindTileset(gid);
                if (ts?.Texture is null) continue;
                var local = gid - ts.FirstGid;
                if (local < 0) continue;
                var sx = (local % ts.Columns) * ts.TileWidth;
                var sy = (local / ts.Columns) * ts.TileHeight;
                var src = new Rectangle(sx, sy, ts.TileWidth, ts.TileHeight);
                var dst = new Rectangle(
                    (int)(mapOffset.X + x * TileWidth),
                    (int)(mapOffset.Y + y * TileHeight),
                    TileWidth,
                    TileHeight);
                sb.Draw(ts.Texture, dst, src, Color.White * layerAlpha);
            }
        }

        base.Draw(sb, inheritedAlpha);
    }

    private TilesetInfo? FindTileset(int gid)
    {
        TilesetInfo? best = null;
        foreach (var ts in _tilesets)
        {
            if (gid >= ts.FirstGid && (best is null || ts.FirstGid > best.FirstGid)) best = ts;
        }
        return best;
    }

    private static string ResolveSafe(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Caminho do mapa vazio.", nameof(relativePath));
        var key = relativePath.Replace('\\', '/').TrimStart('/');
        var root = Path.GetFullPath(ResManager.ResRoot);
        var full = Path.GetFullPath(Path.Combine(root, key));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"[Aegis|Tilemap] Caminho fora da pasta res/: '{relativePath}'");
        if (!File.Exists(full))
            throw new FileNotFoundException($"[Aegis|Tilemap] Mapa não encontrado: {full}");
        return full;
    }
}
