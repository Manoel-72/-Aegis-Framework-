using System.Text.Json;
using AegisEditor.Shared.Models;

namespace Aegis.Scene;

internal static class SceneComponentJson
{
    public static ComponentDto? Get(SceneEntityDto entity, string type)
        => entity.Components.FirstOrDefault(c => c.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

    public static string? String(ComponentDto? component, params string[] keys)
    {
        var value = Find(component, keys);
        return value is { ValueKind: JsonValueKind.String } ? value.Value.GetString() : null;
    }

    public static float? Float(ComponentDto? component, params string[] keys)
    {
        var value = Find(component, keys);
        return value is { ValueKind: JsonValueKind.Number } && value.Value.TryGetSingle(out var number)
            ? number
            : null;
    }

    public static bool? Bool(ComponentDto? component, params string[] keys)
    {
        var value = Find(component, keys);
        return value?.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    public static float[]? FloatArray(ComponentDto? component, string key, int minCount)
    {
        if (component is null || !component.Properties.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.Array)
            return null;

        var result = new List<float>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetSingle(out var number))
                result.Add(number);
        }

        return result.Count >= minCount ? result.ToArray() : null;
    }

    private static JsonElement? Find(ComponentDto? component, IEnumerable<string> keys)
    {
        if (component is null)
            return null;

        foreach (var key in keys)
        {
            if (component.Properties.TryGetValue(key, out var value))
                return value;
        }

        return null;
    }
}
