using Aegis.Scene;
using Aegis.World;
using Microsoft.Xna.Framework;
using NLua;

namespace Aegis.Scripting;

public sealed partial class LuaRuntime
{
    public TilemapNode LoadTilemap(string tiledJsonPath)
        => TilemapNode.LoadTiledJson(tiledJsonPath, _app.S2D);

    public TilemapNode GenerateTilemap(
        string tilesetPath,
        int width,
        int height,
        int tileW,
        int tileH,
        int seed = 1337,
        float scale = 0.08f)
        => TilemapNode.GenerateProcedural(tilesetPath, width, height, tileW, tileH, seed, scale, parent: _app.S2D);

    public void SetTile(TilemapNode map, int layer, int x, int y, int gid) => map.SetTile(layer, x, y, gid);

    public int GetTile(TilemapNode map, int layer, int x, int y) => map.GetTile(layer, x, y);

    public void SetTileCulling(TilemapNode map, bool enabled, int padding = 2)
    {
        map.CameraCulling = enabled;
        map.CullingPaddingTiles = Math.Clamp(padding, 0, 32);
    }

    public int BuildTilemapColliders(TilemapNode map, LuaTable opts)
    {
        var gids = ReadIntArray(opts["solidGids"] as LuaTable);
        var merge = TableBool(opts, "merge", true);
        var useTiledProperty = TableBool(opts, "useTiledProperty", false);
        var layer = opts["layer"] is null ? 1 : (opts["layer"] is string ls ? ParseLayer(ls) : Convert.ToInt32(opts["layer"]));
        var mask = opts["mask"] is null ? ~0 : (opts["mask"] is string ms ? ParseMaskString(ms) : Convert.ToInt32(opts["mask"]));
        return map.BuildColliders(gids, merge, layer, mask, useTiledProperty);
    }

    public void ClearTilemapColliders(TilemapNode map) => map.ClearColliders();

    public int TilemapColliderCount(TilemapNode map) => map.GeneratedColliderCount;

    public LuaTable MapObjects(TilemapNode map)
        => ToLuaMapObjects(map.Objects);

    public LuaTable MapObjectsByType(TilemapNode map, string type)
        => ToLuaMapObjects(map.GetObjectsByType(type));

    public int SpawnMapObjects(TilemapNode map, LuaTable handlers)
    {
        var spawned = 0;
        foreach (var obj in map.Objects)
        {
            var callback = FindMapObjectHandler(handlers, obj);
            if (callback is null) continue;

            callback.Call(ToLuaMapObject(obj));
            spawned++;
        }

        return spawned;
    }

    public NavGrid NewNavGrid(int width, int height, int cellSize, bool diagonal = false)
        => new(width, height, cellSize, diagonal);

    public NavGrid NavFromTilemap(TilemapNode map, LuaTable opts)
    {
        var gids = ReadIntArray(opts["solidGids"] as LuaTable);
        var diagonal = TableBool(opts, "diagonal", false);
        return NavGrid.FromTilemap(map, gids, diagonal);
    }

    public LuaTable? NavFindPath(NavGrid nav, float startX, float startY, float goalX, float goalY)
    {
        var path = nav.FindPath(new Vector2(startX, startY), new Vector2(goalX, goalY));
        if (path is null) return null;

        _lua.NewTable("_aegis_path");
        var t = (LuaTable)_lua["_aegis_path"];
        for (int i = 0; i < path.Count; i++)
        {
            _lua.NewTable("_aegis_path_point");
            var p = (LuaTable)_lua["_aegis_path_point"];
            p["x"] = path[i].X;
            p["y"] = path[i].Y;
            t[i + 1] = p;
        }

        return t;
    }

    public void NavSetSolid(NavGrid nav, int x, int y, bool solid) => nav.SetSolid(x, y, solid);

    public bool NavIsSolid(NavGrid nav, int x, int y) => nav.IsSolid(x, y);

    public float Perlin(float x, float y, int seed = 1337, int octaves = 4, float scale = 0.08f)
        => new PerlinNoise(seed).Fractal(x * scale, y * scale, octaves);

    public void RegisterScene(string name, string luaFile) => SceneManager.Instance.RegisterScene(name, luaFile);

    public void TransitionTo(string scene, string mode = "fade", float seconds = 0.35f)
        => SceneManager.Instance.TransitionTo(scene, mode, seconds);

    public AreaTrigger NewAreaTrigger(string name, float x, float y, float w, float h, bool oneShot = false)
    {
        var trigger = new AreaTrigger(name, x, y, w, h) { OneShot = oneShot };
        SceneManager.Instance.AddTrigger(trigger);
        return trigger;
    }

    public void OnTriggerEnter(AreaTrigger trigger, LuaFunction cb)
        => trigger.OnEnter = (t, o) => cb.Call(t, o);

    public void OnTriggerStay(AreaTrigger trigger, LuaFunction cb)
        => trigger.OnStay = (t, o) => cb.Call(t, o);

    public void OnTriggerExit(AreaTrigger trigger, LuaFunction cb)
        => trigger.OnExit = (t, o) => cb.Call(t, o);

    public void CheckTrigger(AreaTrigger trigger, Object2D obj) => trigger.Check(obj);

    public void ClearTriggers() => SceneManager.Instance.ClearTriggers();

    private LuaTable ToLuaMapObjects(IReadOnlyList<TiledMapObject> objects)
    {
        _lua.NewTable("_aegis_map_objects");
        var table = (LuaTable)_lua["_aegis_map_objects"];

        for (var i = 0; i < objects.Count; i++)
        {
            var obj = objects[i];
            _lua.NewTable("_aegis_map_object");
            var item = (LuaTable)_lua["_aegis_map_object"];
            item["layer"] = obj.LayerName;
            item["id"] = obj.Id;
            item["name"] = obj.Name;
            item["type"] = obj.Type;
            item["x"] = obj.X;
            item["y"] = obj.Y;
            item["width"] = obj.Width;
            item["height"] = obj.Height;
            item["rotation"] = obj.Rotation;
            item["visible"] = obj.Visible;

            _lua.NewTable("_aegis_map_object_props");
            var props = (LuaTable)_lua["_aegis_map_object_props"];
            foreach (var prop in obj.Properties)
                props[prop.Key] = prop.Value;
            item["properties"] = props;

            table[i + 1] = item;
        }

        return table;
    }

    private LuaTable ToLuaMapObject(TiledMapObject obj)
    {
        _lua.NewTable("_aegis_map_object");
        var item = (LuaTable)_lua["_aegis_map_object"];
        item["layer"] = obj.LayerName;
        item["id"] = obj.Id;
        item["name"] = obj.Name;
        item["type"] = obj.Type;
        item["x"] = obj.X;
        item["y"] = obj.Y;
        item["width"] = obj.Width;
        item["height"] = obj.Height;
        item["rotation"] = obj.Rotation;
        item["visible"] = obj.Visible;

        _lua.NewTable("_aegis_map_object_props");
        var props = (LuaTable)_lua["_aegis_map_object_props"];
        foreach (var prop in obj.Properties)
            props[prop.Key] = prop.Value;
        item["properties"] = props;

        return item;
    }

    private static LuaFunction? FindMapObjectHandler(LuaTable handlers, TiledMapObject obj)
    {
        if (!string.IsNullOrWhiteSpace(obj.Type) && handlers[obj.Type] is LuaFunction byType)
            return byType;

        if (!string.IsNullOrWhiteSpace(obj.Name) && handlers[obj.Name] is LuaFunction byName)
            return byName;

        return handlers["default"] as LuaFunction;
    }
}
