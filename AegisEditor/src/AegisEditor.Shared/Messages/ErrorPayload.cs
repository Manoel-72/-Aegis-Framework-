using System.Text.Json.Serialization;

namespace AegisEditor.Shared.Messages;

public sealed class ErrorPayload
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
