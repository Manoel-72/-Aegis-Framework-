using System.Text.Json.Serialization;

namespace AegisEditor.Shared.Messages;

public sealed class LogPayload
{
    [JsonPropertyName("level")]
    public string Level { get; set; } = "info";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
