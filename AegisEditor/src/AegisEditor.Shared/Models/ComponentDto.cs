using System.Text.Json;

namespace AegisEditor.Shared.Models;

public sealed class ComponentDto
{
    public string Type { get; set; } = string.Empty;

    /// <summary>Property bag; values are typically JSON primitives or nested objects.</summary>
    public Dictionary<string, JsonElement> Properties { get; set; } = new();
}
