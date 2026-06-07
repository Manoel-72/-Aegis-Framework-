namespace AegisEditor.Shared.Models;

public sealed class SceneState
{
    public const int CurrentVersion = 2;

    public string Format { get; set; } = "aegis.scene";

    public int Version { get; set; } = CurrentVersion;

    public string Name { get; set; } = "Scene";

    public string Kind { get; set; } = "2d";

    public int Width { get; set; } = 1280;

    public int Height { get; set; } = 720;

    public List<SceneEntityDto> Entities { get; set; } = new();

    public List<TilemapDto> Tilemaps { get; set; } = new();

    public static SceneState CreateDefault2D(string name = "Main")
    {
        return new SceneState
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Main" : name.Trim(),
            Entities =
            [
                new SceneEntityDto
                {
                    Id = "camera-main",
                    Name = "Main Camera",
                    Type = "Camera",
                    X = 0,
                    Y = 0
                },
                new SceneEntityDto
                {
                    Id = "level-root",
                    Name = "Level Root",
                    Type = "Group",
                    X = 0,
                    Y = 0
                }
            ]
        };
    }
}
