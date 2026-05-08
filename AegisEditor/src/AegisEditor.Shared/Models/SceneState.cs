namespace AegisEditor.Shared.Models;

public sealed class SceneState
{
    public List<SceneEntityDto> Entities { get; set; } = new();

    public List<TilemapDto> Tilemaps { get; set; } = new();
}
