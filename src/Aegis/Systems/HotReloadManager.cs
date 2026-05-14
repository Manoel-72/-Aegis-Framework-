using Aegis.Editor;
using Aegis.Scripting;
using Aegis.Input;

namespace Aegis.Systems;

public sealed class HotReloadManager
{
    public static HotReloadManager Instance { get; } = new();

    private HotReloadManager()
    {
    }

    private string _entry = "";
    private LuaRuntime? _lua;
    private DateTime _lastWrite;
    private float _timer;
    public bool Enabled { get; set; } = true;

    public string LastStatus { get; private set; } = "hot reload: ready";

    public void Initialize(string entry, LuaRuntime lua)
    {
        _entry = Path.GetFullPath(entry);
        _lua = lua;
        _lastWrite = File.Exists(_entry) ? File.GetLastWriteTimeUtc(_entry) : DateTime.MinValue;
    }

    public void Update(float dt)
    {
        if (EditorPipeHost.SimulationPausedByEditor) return;
        if (!Enabled || _lua is null || string.IsNullOrEmpty(_entry)) return;
        _timer += dt;
        if (_timer < 0.5f && !InputManager.JustPressed("F5")) return;
        _timer = 0f;
        try
        {
            var w = File.GetLastWriteTimeUtc(_entry);
            if (w <= _lastWrite && !InputManager.JustPressed("F5")) return;
            _lastWrite = w;
            _lua.ReloadMainScript(_entry);
            LastStatus = "hot reload: ok";
        }
        catch (Exception ex)
        {
            LastStatus = "hot reload error: " + ex.Message;
            Console.Error.WriteLine("[Aegis|HotReload] " + ex);
        }
    }
}
