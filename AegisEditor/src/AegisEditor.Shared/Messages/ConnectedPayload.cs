using System.Text.Json.Serialization;

namespace AegisEditor.Shared.Messages;

public sealed class ConnectedPayload
{
    [JsonPropertyName("runtimeVersion")]
    public string RuntimeVersion { get; set; } = string.Empty;
}
