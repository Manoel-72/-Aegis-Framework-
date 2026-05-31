using Aegis.Systems;

namespace Aegis.Scripting;

public sealed partial class LuaRuntime
{
    public void Save(string key, object? value) => SaveManager.Save(key, value);

    public object? Load(string key) => SaveManager.Load(key);

    public object? LoadConfig(string key) => ConfigManager.Load(key);

    public void SetFullscreen(bool value) => ConfigManager.SetFullscreen(value);

    public void SetDisplayMode(string mode) => ConfigManager.SetDisplayMode(mode);

    public void SetResolution(int width, int height) => ConfigManager.SetResolution(width, height);
}
