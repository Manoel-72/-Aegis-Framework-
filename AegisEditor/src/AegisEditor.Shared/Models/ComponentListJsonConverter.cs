using System.Text.Json;
using System.Text.Json.Serialization;

namespace AegisEditor.Shared.Models;

public sealed class ComponentListJsonConverter : JsonConverter<List<ComponentDto>>
{
    public override List<ComponentDto> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var result = new List<ComponentDto>();

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var component in root.EnumerateObject())
            {
                var dto = new ComponentDto { Type = component.Name };
                if (component.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in component.Value.EnumerateObject())
                        dto.Properties[prop.Name] = prop.Value.Clone();
                }

                result.Add(dto);
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                var dto = new ComponentDto();
                if (item.TryGetProperty("type", out var type) || item.TryGetProperty("Type", out type))
                    dto.Type = type.GetString() ?? string.Empty;

                if (item.TryGetProperty("properties", out var properties) || item.TryGetProperty("Properties", out properties))
                {
                    if (properties.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in properties.EnumerateObject())
                            dto.Properties[prop.Name] = prop.Value.Clone();
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.Type))
                    result.Add(dto);
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, List<ComponentDto> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var component in value.Where(c => !string.IsNullOrWhiteSpace(c.Type)))
        {
            writer.WritePropertyName(component.Type.Trim());
            writer.WriteStartObject();
            foreach (var prop in component.Properties)
            {
                writer.WritePropertyName(prop.Key);
                prop.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }
}
