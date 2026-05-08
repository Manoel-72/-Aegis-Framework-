using System.Text.Json;

namespace AegisEditor.Shared.Messages;

/// <summary>
/// Factory for newline-delimited JSON commands (Editor → Runtime).
/// </summary>
public static class EditorCommand
{
    public const string SceneLoad = "SCENE_LOAD";

    public const string EntitySelect = "ENTITY_SELECT";

    public const string EntityMove = "ENTITY_MOVE";

    public const string EntityScale = "ENTITY_SCALE";

    public const string EntitySpawn = "ENTITY_SPAWN";

    public const string EntityDelete = "ENTITY_DELETE";

    public const string PropSet = "PROP_SET";

    public const string Play = "PLAY";

    public const string Pause = "PAUSE";

    public const string Stop = "STOP";

    public const string HotReload = "HOT_RELOAD";

    public static string ToLine(string type, object? payload)
    {
        var env = new EditorToRuntimeEnvelope { Type = type, Payload = payload };
        return JsonSerializer.Serialize(env, IpcSerializerOptions.Create()) + "\n";
    }

    public static string SceneLoadLine(string path)
        => ToLine(SceneLoad, new { path });

    public static string EntitySelectLine(string id)
        => ToLine(EntitySelect, new { id });

    public static string EntityMoveLine(string id, double x, double y)
        => ToLine(EntityMove, new { id, x, y });

    public static string EntityScaleLine(string id, double sx, double sy)
        => ToLine(EntityScale, new { id, sx, sy });

    public static string EntitySpawnLine(string type, double x, double y)
        => ToLine(EntitySpawn, new { type, x, y });

    public static string EntityDeleteLine(string id)
        => ToLine(EntityDelete, new { id });

    public static string PropSetLine(string id, string component, string key, object? value)
        => ToLine(PropSet, new { id, component, key, value });

    public static string PlayLine()
        => ToLine(Play, new { });

    public static string PauseLine()
        => ToLine(Pause, new { });

    public static string StopLine()
        => ToLine(Stop, new { });

    public static string HotReloadLine(string file)
        => ToLine(HotReload, new { file });
}
