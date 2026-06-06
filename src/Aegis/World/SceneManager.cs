using Aegis.Core;
using Aegis.Display;
using Aegis.Scene;
using Aegis.Scripting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLua;

namespace Aegis.World;

/// <summary>
/// SceneManager nativo: transições simples e troca de main.lua sem reiniciar a janela.
/// </summary>
public sealed class SceneManager
{
    public static SceneManager Instance { get; } = new();
    private SceneManager() { }

    private LuaRuntime? _lua;
    private App? _app;
    private readonly Dictionary<string, string> _scenes = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<AreaTrigger> _triggers = new();
    private string? _currentScene;

    private string? _pendingScene;
    private LuaTable? _pendingData;
    private string _transition = "fade";
    private float _transitionTime = 0.35f;
    private float _timer;
    private bool _loading;

    public bool IsTransitioning => _pendingScene is not null || _timer > 0f;
    public float FadeAlpha { get; private set; }

    public void Initialize(App app, LuaRuntime lua)
    {
        _app = app;
        _lua = lua;
        _scenes.Clear();
        _triggers.Clear();
        _pendingScene = null;
        _pendingData = null;
        _currentScene = null;
        _timer = 0f;
        _loading = false;
        FadeAlpha = 0f;
    }

    public void RegisterScene(string name, string luaFile)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome de cena vazio.", nameof(name));
        if (string.IsNullOrWhiteSpace(luaFile)) throw new ArgumentException("Arquivo de cena vazio.", nameof(luaFile));
        _scenes[name.Trim()] = luaFile.Replace('\\', '/').TrimStart('/');
    }

    public void TransitionTo(string scene, string transition = "fade", float duration = 0.35f, LuaTable? data = null)
    {
        if (_lua is null || _app is null) return;
        if (string.IsNullOrWhiteSpace(scene))
            throw new ArgumentException("Nome de cena vazio.", nameof(scene));
        if (!_scenes.ContainsKey(scene) && !File.Exists(scene))
            throw new InvalidOperationException($"[Aegis|Scene] Cena não registrada: {scene}. Use aegis.registerScene(nome, arquivoLua) antes de transitionTo.");
        scene = scene.Trim();
        if (_pendingScene is not null && string.Equals(_pendingScene, scene, StringComparison.OrdinalIgnoreCase))
            return;

        _pendingScene = scene;
        _pendingData = data;
        _transition = string.IsNullOrWhiteSpace(transition) ? "fade" : transition.ToLowerInvariant();
        _transitionTime = Math.Clamp(duration, 0.01f, 10f);
        _timer = 0f;
        _loading = false;
        if (_transition == "none")
        {
            LoadPending();
            _pendingScene = null;
            _pendingData = null;
            _loading = false;
            FadeAlpha = 0f;
        }
    }

    public void AddTrigger(AreaTrigger trigger) => _triggers.Add(trigger);
    public void ClearTriggers() => _triggers.Clear();
    public void CheckTriggers(Object2D obj)
    {
        foreach (var t in _triggers.ToArray()) t.Check(obj);
    }

    public void Update(float dt)
    {
        if (_pendingScene is null)
        {
            FadeAlpha = Math.Max(0f, FadeAlpha - dt * 4f);
            return;
        }

        if (_transition == "none") { FadeAlpha = 0f; return; }

        _timer += Math.Max(0f, dt);
        var half = _transitionTime * 0.5f;
        if (!_loading)
        {
            FadeAlpha = Math.Clamp(_timer / half, 0f, 1f);
            if (_timer >= half) LoadPending();
        }
        else
        {
            FadeAlpha = Math.Clamp(1f - ((_timer - half) / half), 0f, 1f);
            if (_timer >= _transitionTime)
            {
                _pendingScene = null;
                _pendingData = null;
                _loading = false;
                FadeAlpha = 0f;
            }
        }
    }

    public void DrawOverlay(SpriteBatch sb)
    {
        var cam = Camera2D.Instance;
        var w = Math.Max(1, cam.ViewWidth);
        var h = Math.Max(1, cam.ViewHeight);
        if (_transition == "slide" && _pendingScene is not null)
        {
            var coverWidth = Math.Clamp((int)MathF.Ceiling(w * FadeAlpha), 0, w);
            if (coverWidth > 0)
                sb.Draw(Aegis.Resource.ResManager.Pixel, new Rectangle(0, 0, coverWidth, h), Color.Black);
            return;
        }

        if (FadeAlpha <= 0.001f) return;
        sb.Draw(Aegis.Resource.ResManager.Pixel, new Rectangle(0, 0, w, h), Color.Black * FadeAlpha);
    }

    private void LoadPending()
    {
        if (_pendingScene is null || _lua is null) return;
        var name = _pendingScene;
        var file = _scenes.TryGetValue(name, out var mapped) ? mapped : name;
        if (!string.IsNullOrWhiteSpace(_currentScene))
            _lua.NotifySceneExit(_currentScene, name, _pendingData);
        _lua.SetSceneData(_pendingData);
        _lua.LoadSceneFile(file);
        _currentScene = name;
        _lua.NotifySceneEnter(name, _pendingData);
        _loading = true;
    }
}
