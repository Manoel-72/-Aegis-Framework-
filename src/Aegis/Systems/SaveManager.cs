using System.Text.Json;

namespace Aegis.Systems;

public static class SaveManager
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static string _root = Directory.GetCurrentDirectory();
    private static string SaveDir => Path.Combine(_root, "saves");
    private static string SaveFile => Path.Combine(SaveDir, "save.json");
    private static Dictionary<string, JsonElement> _data = new();

    public static void Initialize(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(SaveDir);
        LoadFromDisk();
    }

    public static void Save(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        lock (Gate)
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value, JsonOptions));
            _data[key] = doc.RootElement.Clone();
            Flush();
        }
    }

    public static object? Load(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        lock (Gate)
        {
            if (!_data.TryGetValue(key, out var v)) return null;
            return ToLuaFriendly(v);
        }
    }

    private static void LoadFromDisk()
    {
        lock (Gate)
        {
            _data = new Dictionary<string, JsonElement>();
            if (!File.Exists(SaveFile)) { Flush(); return; }
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(SaveFile));
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
                foreach (var p in doc.RootElement.EnumerateObject()) _data[p.Name] = p.Value.Clone();
            }
            catch { _data.Clear(); Flush(); }
        }
    }

    private static void Flush()
    {
        Directory.CreateDirectory(SaveDir);
        var plain = _data.ToDictionary(k => k.Key, v => ToPlain(v.Value));
        File.WriteAllText(SaveFile, JsonSerializer.Serialize(plain, JsonOptions));
    }

    private static object? ToPlain(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.TryGetInt64(out var i) ? i : e.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => e.EnumerateArray().Select(ToPlain).ToArray(),
        JsonValueKind.Object => e.EnumerateObject().ToDictionary(p => p.Name, p => ToPlain(p.Value)),
        _ => null
    };

    private static object? ToLuaFriendly(JsonElement e) => ToPlain(e);
}
