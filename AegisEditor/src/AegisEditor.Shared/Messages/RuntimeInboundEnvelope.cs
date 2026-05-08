using System.Text.Json;
using System.Text.Json.Serialization;

namespace AegisEditor.Shared.Messages;

/// <summary>Raw inbound message before dispatch by <see cref="RuntimeEvent"/>.</summary>
public sealed class RuntimeInboundEnvelope
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; init; }
}
