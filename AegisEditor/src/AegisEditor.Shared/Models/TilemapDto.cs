namespace AegisEditor.Shared.Models;

public sealed class TilemapDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? TiledJsonPath { get; set; }

    public float X { get; set; }

    public float Y { get; set; }
}
