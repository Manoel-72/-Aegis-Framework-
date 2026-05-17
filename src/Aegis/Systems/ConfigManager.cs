using System.Text.Json;
using Aegis.Core;

namespace Aegis.Systems;

public sealed class AegisConfig
{
    public int windowWidth { get; set; } = 1280;
    public int windowHeight { get; set; } = 720;
    public string? displayMode { get; set; }
    public bool fullscreen { get; set; } = false;
    public bool vsync { get; set; } = true;
    public float masterVolume { get; set; } = 1f;
}

public static class ConfigManager
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static string _root = Directory.GetCurrentDirectory();
    public static AegisConfig Current { get; private set; } = new();
    public static string ConfigPath => Path.Combine(_root, "aegis.cfg");

    public static void Initialize(string root, int fallbackWidth, int fallbackHeight)
    {
        _root = Path.GetFullPath(root);
        Current = new AegisConfig { windowWidth = fallbackWidth, windowHeight = fallbackHeight };
        if (File.Exists(ConfigPath))
        {
            try { Current = JsonSerializer.Deserialize<AegisConfig>(File.ReadAllText(ConfigPath)) ?? Current; }
            catch { }
        }
        Sanitize();
        Save();
    }

    public static void Save()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Current, Options));
    }

    public static void SetResolution(int width, int height)
    {
        Current.windowWidth = Math.Clamp(width, 320, 7680);
        Current.windowHeight = Math.Clamp(height, 240, 4320);
        Save();
        AegisGame.Current?.ApplyWindowConfig(Current.windowWidth, Current.windowHeight, Current.displayMode ?? "windowed");
    }

    public static void SetFullscreen(bool value)
    {
        Current.displayMode = value ? "borderless" : "windowed";
        Current.fullscreen = value;
        Save();
        AegisGame.Current?.ApplyWindowConfig(Current.windowWidth, Current.windowHeight, Current.displayMode);
    }

    public static void SetDisplayMode(string? mode)
    {
        Current.displayMode = NormalizeDisplayMode(mode, Current.fullscreen);
        Current.fullscreen = Current.displayMode != "windowed";
        Save();
        AegisGame.Current?.ApplyWindowConfig(Current.windowWidth, Current.windowHeight, Current.displayMode);
    }

    public static object? Load(string key) => key switch
    {
        "windowWidth" or "width" => Current.windowWidth,
        "windowHeight" or "height" => Current.windowHeight,
        "displayMode" => Current.displayMode,
        "fullscreen" => Current.fullscreen,
        "vsync" => Current.vsync,
        "masterVolume" => Current.masterVolume,
        _ => null
    };

    private static void Sanitize()
    {
        Current.windowWidth = Math.Clamp(Current.windowWidth, 320, 7680);
        Current.windowHeight = Math.Clamp(Current.windowHeight, 240, 4320);
        Current.displayMode = NormalizeDisplayMode(Current.displayMode, Current.fullscreen);
        Current.fullscreen = Current.displayMode != "windowed";
        Current.masterVolume = Math.Clamp(Current.masterVolume, 0f, 1f);
    }

    private static string NormalizeDisplayMode(string? mode, bool legacyFullscreen)
    {
        mode = string.IsNullOrWhiteSpace(mode)
            ? (legacyFullscreen ? "borderless" : "windowed")
            : mode.Trim().ToLowerInvariant();

        return mode switch
        {
            "window" or "windowed" => "windowed",
            "borderless" or "fullscreen" or "fullscreen-borderless" => "borderless",
            _ => legacyFullscreen ? "borderless" : "windowed"
        };
    }
}
