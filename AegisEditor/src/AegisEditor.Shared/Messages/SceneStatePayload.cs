using System.Text.Json.Serialization;
using AegisEditor.Shared.Models;

namespace AegisEditor.Shared.Messages;

public sealed class SceneStatePayload
{
    [JsonPropertyName("entities")]
    public List<SceneEntityDto> Entities { get; set; } = new();

    [JsonPropertyName("tilemaps")]
    public List<TilemapDto>? Tilemaps { get; set; }
}
