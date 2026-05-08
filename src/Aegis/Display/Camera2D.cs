using Microsoft.Xna.Framework;

namespace Aegis.Display;

/// <summary>
/// Câmera 2D com follow suave, zoom e limites de mundo.
/// Produz uma matriz de transformação que é passada ao SpriteBatch.Begin().
///
/// Estratégia de integração: NÃO altera Renderer.cs nem SpriteBatch global.
/// O AegisGame.cs usa Camera2D.GetTransform() no Begin() do frame de câmera.
/// O dev liga/desliga via aegis.setCameraTarget() / aegis.setCameraOff().
///
/// Uso Lua:
///   aegis.setCameraTarget(player)     -- segue objeto
///   aegis.setCameraZoom(2.0)          -- zoom 2x
///   aegis.setCameraOffset(0, -50)     -- offset vertical (ex: ver mais abaixo)
///   aegis.setCameraOff()              -- câmera fixa (sem transform)
///   aegis.setCameraLimits(0,0,3200,1800)  -- clamp de mundo
/// </summary>
public sealed class Camera2D
{
    public static Camera2D Instance { get; } = new();

    private Camera2D() { }

    // ── Estado ───────────────────────────────────────────────────────
    public float X         { get; private set; }
    public float Y         { get; private set; }
    public float Zoom      { get; private set; } = 1f;
    public bool  Active    { get; private set; } = false;

    // Follow
    private Scene.Object2D? _target;
    private float           _followSpeed = 5f;   // lerp speed (unidades/s)
    private Vector2         _offset;
    private Vector2         _deadzone;           // pixels de tela; 0 = desligado
    private float           _lookaheadDistance;  // pixels/unidades à frente do movimento
    private float           _lookaheadSpeed = 4f;
    private Vector2         _lookahead;
    private Vector2?        _lastTargetPos;

    // Limites do mundo (null = sem limites)
    private Rectangle? _bounds;

    // Tamanho da viewport (preenchido pelo AegisGame)
    internal int ViewWidth;
    internal int ViewHeight;

    // ── Configuração ─────────────────────────────────────────────────
    public void SetTarget(Scene.Object2D? target, float speed = 5f)
    {
        _target      = target;
        _followSpeed = speed;
        Active       = true;
    }

    public void SetZoom(float zoom)
    {
        if (!float.IsFinite(zoom) || zoom <= 0f) zoom = 1f;
        Zoom = Math.Clamp(zoom, 0.1f, 16f);
    }
    public void SetOffset(float ox, float oy) => _offset = new Vector2(ox, oy);
    public void SetDeadzone(float width, float height)
        => _deadzone = new Vector2(MathF.Max(0f, width), MathF.Max(0f, height));
    public void SetLookahead(float distance, float speed = 4f)
    {
        _lookaheadDistance = MathF.Max(0f, distance);
        _lookaheadSpeed = Math.Clamp(speed, 0.01f, 60f);
    }
    public void SetLimits(float left, float top, float right, float bottom)
        => _bounds = new Rectangle((int)left, (int)top,
                                   (int)(right - left), (int)(bottom - top));
    public void ClearLimits() => _bounds = null;
    public void Deactivate()  { Active = false; _target = null; }

    /// Limpa estado de sessão anterior (singleton persiste no processo).
    public void ResetForNewSession()
    {
        X     = 0f;
        Y     = 0f;
        Zoom  = 1f;
        Active  = false;
        _target = null;
        _offset = Vector2.Zero;
        _deadzone = Vector2.Zero;
        _lookahead = Vector2.Zero;
        _lookaheadDistance = 0f;
        _lookaheadSpeed = 4f;
        _lastTargetPos = null;
        _followSpeed = 5f;
        _bounds  = null;
    }

    /// Teleporta a câmera sem lerp
    public void MoveTo(float x, float y) { X = x; Y = y; }

    // ── Update (chamado pelo AegisGame antes do Draw) ─────────────────
    public void Update(float dt)
    {
        if (!Active) return;

        if (_target is not null)
        {
            var targetPos = new Vector2(_target.X, _target.Y);
            var velocity = Vector2.Zero;
            if (_lastTargetPos.HasValue && dt > 0f)
                velocity = (targetPos - _lastTargetPos.Value) / dt;
            _lastTargetPos = targetPos;

            var desiredLookahead = Vector2.Zero;
            if (_lookaheadDistance > 0f && velocity.LengthSquared() > 0.001f)
                desiredLookahead = Vector2.Normalize(velocity) * _lookaheadDistance;
            var lt = Math.Clamp(_lookaheadSpeed * dt, 0f, 1f);
            _lookahead += (desiredLookahead - _lookahead) * lt;

            var focus = targetPos + _offset + _lookahead;
            var viewWorldW = ViewWidth / Zoom;
            var viewWorldH = ViewHeight / Zoom;

            if (_deadzone.X > 0f || _deadzone.Y > 0f)
            {
                var dzW = _deadzone.X / Zoom;
                var dzH = _deadzone.Y / Zoom;
                var left = X + (viewWorldW - dzW) * 0.5f;
                var right = left + dzW;
                var top = Y + (viewWorldH - dzH) * 0.5f;
                var bottom = top + dzH;

                if (focus.X < left) X += focus.X - left;
                else if (focus.X > right) X += focus.X - right;
                if (focus.Y < top) Y += focus.Y - top;
                else if (focus.Y > bottom) Y += focus.Y - bottom;
            }
            else
            {
                float desiredX = focus.X - viewWorldW / 2f;
                float desiredY = focus.Y - viewWorldH / 2f;
                float t = Math.Clamp(_followSpeed * dt, 0f, 1f);
                X = X + (desiredX - X) * t;
                Y = Y + (desiredY - Y) * t;
            }
        }

        // Clamp aos limites do mundo
        if (_bounds.HasValue)
        {
            float maxX = _bounds.Value.Right  - ViewWidth  / Zoom;
            float maxY = _bounds.Value.Bottom - ViewHeight / Zoom;
            X = Math.Clamp(X, _bounds.Value.Left, MathF.Max(_bounds.Value.Left, maxX));
            Y = Math.Clamp(Y, _bounds.Value.Top,  MathF.Max(_bounds.Value.Top,  maxY));
        }
    }

    // ── Matriz para SpriteBatch.Begin() ──────────────────────────────
    public Matrix GetTransform() =>
        Matrix.CreateTranslation(-X, -Y, 0f)
      * Matrix.CreateScale(Zoom, Zoom, 1f);

    // ── Utilitário: converte posição de tela → mundo ──────────────────
    public Vector2 ScreenToWorld(float sx, float sy) =>
        new(sx / Zoom + X, sy / Zoom + Y);

    public Vector2 WorldToScreen(float wx, float wy) =>
        new((wx - X) * Zoom, (wy - Y) * Zoom);
}
