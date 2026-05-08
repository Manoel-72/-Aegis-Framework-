using System.Text.Json.Serialization;

namespace AegisEditor.Shared.Messages;

public sealed class EditorToRuntimeEnvelope
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("payload")]
    public object? Payload { get; init; }
}
