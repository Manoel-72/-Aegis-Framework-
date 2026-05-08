using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using Aegis.Physics;

namespace Aegis;

public sealed class Engine
{
    public static Engine Instance { get; private set; } = null!;

    public int   Width  { get; }
    public int   Height { get; }
    public float Time   { get; private set; }

    private readonly List<Node>  _world = new();
    private readonly List<Node>  _hud   = new();
    private readonly World       _physics;
    private readonly Input       _input;
    private readonly Camera      _camera;
    private readonly LuaBridge   _lua;

    private float _shakeAmt, _shakeDur, _shakeTimer;
    private readonly Random _rng = new();

    public Engine(int w, int h, string title)
    {
        Instance = this;
        Width    = w;
        Height   = h;
        _physics = new World();
        _input   = new Input();
        _camera  = new Camera(w, h);
        _lua     = new LuaBridge(this, _physics, _input, _camera);
        _ = title;
    }

    public void Run(string gameDir)
    {
        string dir = Path.IsPathRooted(gameDir)
            ? gameDir
            : Path.Combine(AppContext.BaseDirectory, "games", gameDir);

        if (!Directory.Exists(dir))
            dir = Path.Combine(Directory.GetCurrentDirectory(), "games", gameDir);

        _lua.Load(dir);

        string title = $"Aegis — {gameDir}";
        if (File.Exists(Path.Combine(dir, "game.json")))
        {
            var txt = File.ReadAllText(Path.Combine(dir, "game.json"));
            var m   = System.Text.RegularExpressions.Regex.Match(
                          txt, @"""title""\s*:\s*""([^""]+)""");
            if (m.Success) title = m.Groups[1].Value;
        }

        InitWindow(Width, Height, title);
        SetTargetFPS(60);

        _lua.CallInit();

        while (!WindowShouldClose())
        {
            float dt = MathF.Min(GetFrameTime(), 0.05f);
            Time += dt;

            if (_shakeTimer > 0f) _shakeTimer -= dt;

            _physics.Update(dt);
            _lua.CallUpdate(dt);
            _camera.Update(dt);

            BeginDrawing();
            ClearBackground(new Color(22, 22, 30, 255));

            float sx = 0f, sy = 0f;
            if (_shakeTimer > 0f)
            {
                float t = _shakeTimer / _shakeDur;
                sx = ((float)_rng.NextDouble() * 2f - 1f) * _shakeAmt * t;
                sy = ((float)_rng.NextDouble() * 2f - 1f) * _shakeAmt * t;
            }

            BeginMode2D(new Raylib_cs.Camera2D
            {
                Offset   = new Vector2(Width * 0.5f + sx, Height * 0.5f + sy),
                Target   = new Vector2(_camera.TargetX, _camera.TargetY),
                Zoom     = _camera.Zoom,
                Rotation = 0f
            });

            foreach (var n in _world) if (n.Visible) n.Draw();
            _lua.CallDraw();

            EndMode2D();

            foreach (var n in _hud) if (n.Visible) n.Draw();

            EndDrawing();
        }

        CloseWindow();
        ResManager.UnloadAll();   // ← v0.5: libera VRAM ao encerrar
    }

    // ── Node management ───────────────────────────────────────────────────
    public void AddNode(Node n, bool hud = false)
    {
        if (hud) _hud.Add(n);
        else     _world.Add(n);
    }

    public void RemoveNode(Node n)
    {
        _world.Remove(n);
        _hud.Remove(n);
    }

    /// Remove todos os nodes — chamado por LuaBridge.ClearAll()
    public void ClearNodes()
    {
        _world.Clear();
        _hud.Clear();
    }

    public void ScreenShake(float amount, float duration)
    {
        _shakeAmt   = amount;
        _shakeDur   = duration;
        _shakeTimer = duration;
    }
}
