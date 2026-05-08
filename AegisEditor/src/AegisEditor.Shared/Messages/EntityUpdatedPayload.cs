using System.Text.Json.Serialization;

namespace AegisEditor.Shared.Messages;

public sealed class EntityUpdatedPayload
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }
}
