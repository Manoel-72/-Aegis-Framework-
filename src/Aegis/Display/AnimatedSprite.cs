using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Display;

/// <summary>
/// Sprite com animação por frames em spritesheet.
/// Herdado de Bitmap — reutiliza toda a pipeline de render existente.
/// O spritesheet deve ter frames de tamanho uniforme, dispostos em linha.
///
/// Uso Lua:
///   local anim = aegis.newAnim("player.png", 32, 32, 8, 0.1)
///   aegis.playAnim(anim, 0, 3, true)   -- frame 0..3, loop
///   aegis.stopAnim(anim)
/// </summary>
public sealed class AnimatedSprite : Bitmap
{
    // ── Configuração da sheet ────────────────────────────────────────
    public int FrameWidth  { get; }
    public int FrameHeight { get; }
    public int TotalFrames { get; }

    // ── Estado da animação ───────────────────────────────────────────
    private int   _startFrame;
    private int   _endFrame;
    private float _frameDuration;   // segundos por frame
    private bool  _loop;
    private bool  _playing;

    private int   _currentFrame;
    private float _elapsed;

    public int  CurrentFrame => _currentFrame;
    public bool IsPlaying    => _playing;

    public AnimatedSprite(Texture2D texture, int frameWidth, int frameHeight,
                          Scene2D? parent = null)
        : base(texture, parent)
    {
        FrameWidth   = frameWidth;
        FrameHeight  = frameHeight;
        TotalFrames  = (texture?.Width ?? 0) / frameWidth;

        // Exibe o primeiro frame por padrão
        SetFrame(0);
    }

    /// <summary>
    /// Inicia a animação entre [start, end] inclusive.
    /// </summary>
    public void Play(int start, int end, bool loop, float fps)
    {
        _startFrame    = Math.Clamp(start, 0, TotalFrames - 1);
        _endFrame      = Math.Clamp(end,   0, TotalFrames - 1);
        _frameDuration = fps > 0f ? 1f / fps : 0.1f;
        _loop          = loop;
        _playing       = true;
        _currentFrame  = _startFrame;
        _elapsed       = 0f;
        SetFrame(_currentFrame);
    }

    public void Stop()
    {
        _playing = false;
    }

    public void Resume() => _playing = true;

    private void SetFrame(int index)
    {
        _currentFrame = index;
        SourceRect    = new Rectangle(index * FrameWidth, 0, FrameWidth, FrameHeight);
    }

    public override void Update(float dt)
    {
        if (_playing)
        {
            _elapsed += dt;
            if (_elapsed >= _frameDuration)
            {
                _elapsed -= _frameDuration;
                int next = _currentFrame + 1;

                if (next > _endFrame)
                {
                    if (_loop)
                        next = _startFrame;
                    else
                    {
                        _playing = false;
                        next     = _endFrame;
                    }
                }
                SetFrame(next);
            }
        }

        base.Update(dt);   // propaga update para filhos
    }
}
