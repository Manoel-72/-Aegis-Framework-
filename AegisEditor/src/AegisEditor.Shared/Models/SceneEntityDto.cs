namespace AegisEditor.Shared.Models;

public sealed class SceneEntityDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public float X { get; set; }

    public float Y { get; set; }

    public float ScaleX { get; set; } = 1f;

    public float ScaleY { get; set; } = 1f;

    public float Rotation { get; set; }

    public string? TexturePath { get; set; }

    public string? ScriptPath { get; set; }

    public List<ComponentDto> Components { get; set; } = new();

    public List<string> Children { get; set; } = new();

    public string? ParentId { get; set; }
}
