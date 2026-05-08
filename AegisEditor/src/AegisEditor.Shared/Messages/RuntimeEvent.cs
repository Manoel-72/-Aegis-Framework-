using System.Text.Json;

namespace AegisEditor.Shared.Messages;

/// <summary>Well-known Runtime → Editor message kinds.</summary>
public static class RuntimeEvent
{
    public const string SceneState = "SCENE_STATE";

    public const string EntityUpdated = "ENTITY_UPDATED";

    public const string Log = "LOG";

    public const string Connected = "CONNECTED";

    public const string Error = "ERROR";

    public static RuntimeInboundEnvelope? TryParseEnvelope(string jsonLine)
    {
        try
        {
            return JsonSerializer.Deserialize<RuntimeInboundEnvelope>(jsonLine, IpcSerializerOptions.Create());
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
