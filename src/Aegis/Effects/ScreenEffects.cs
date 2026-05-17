using Aegis.Resource;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Effects;

public sealed class ScreenEffects
{
    public static ScreenEffects Instance { get; } = new();
    private ScreenEffects() { }

    private float _fadeAlpha, _fadeFrom, _fadeTo, _fadeT, _fadeDur;
    private float _flashT, _flashDur;
    private Color _flashColor = Color.White;

    public void FadeIn(float dur = 0.35f)  => StartFade(1f, 0f, dur);
    public void FadeOut(float dur = 0.35f) => StartFade(0f, 1f, dur);
    public void Flash(Color color, float dur = 0.12f) { _flashColor = color; _flashDur = MathF.Max(0.01f, dur); _flashT = _flashDur; }

    public void Reset()
    {
        _fadeAlpha = 0f;
        _fadeFrom = 0f;
        _fadeTo = 0f;
        _fadeT = 0f;
        _fadeDur = 0f;
        _flashT = 0f;
        _flashDur = 0f;
        _flashColor = Color.White;
    }

    private void StartFade(float from, float to, float dur)
    {
        _fadeFrom = from; _fadeTo = to; _fadeDur = MathF.Max(0.01f, dur); _fadeT = 0f; _fadeAlpha = from;
    }

    public void Update(float dt)
    {
        if (_fadeT < _fadeDur)
        {
            _fadeT += dt;
            var t = Math.Clamp(_fadeT / _fadeDur, 0f, 1f);
            _fadeAlpha = _fadeFrom + (_fadeTo - _fadeFrom) * t;
        }
        if (_flashT > 0f) _flashT = MathF.Max(0f, _flashT - dt);
    }

    public void Draw(SpriteBatch sb, GraphicsDevice gd)
    {
        var vp = gd.Viewport;
        var rect = new Rectangle(0, 0, vp.Width, vp.Height);
        if (_fadeAlpha > 0.001f)
            sb.Draw(ResManager.Pixel, rect, Color.Black * Math.Clamp(_fadeAlpha, 0f, 1f));
        if (_flashT > 0f)
        {
            var a = Math.Clamp(_flashT / _flashDur, 0f, 1f);
            sb.Draw(ResManager.Pixel, rect, _flashColor * a);
        }
    }
}
