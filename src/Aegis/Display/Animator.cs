using Aegis.Resource;
using Aegis.Scene;
using Microsoft.Xna.Framework;

namespace Aegis.Display;

/// <summary>
/// Animator para SpriteNode. Suporta spritesheet em grade e atlas nomeado do Aseprite.
/// A API de Play/Stop é a mesma nos dois modos.
/// </summary>
public sealed class Animator : Object2D
{
    private sealed record GridClip(int[] Frames, float Fps, bool Loop);
    private sealed record AtlasClip(string[] Frames, float Fps, bool Loop);

    private readonly SpriteNode _target;
    private readonly Dictionary<string, GridClip> _gridClips = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AtlasClip> _atlasClips = new(StringComparer.OrdinalIgnoreCase);
    private readonly SpriteAtlas? _atlas;

    private GridClip? _currentGrid;
    private AtlasClip? _currentAtlas;
    private string? _currentName;
    private int _frameIndex;
    private float _elapsed;
    private bool _playing;
    private bool _finished = true;

    public int FrameWidth { get; }
    public int FrameHeight { get; }
    public bool UsesAtlas => _atlas is not null;
    public string? CurrentClip => _currentName;
    public bool IsPlaying => _playing;
    public bool IsFinished => _finished;
    public event Action<Animator, string>? Completed;

    public Animator(SpriteNode target, int frameWidth, int frameHeight)
    {
        if (frameWidth <= 0) throw new ArgumentOutOfRangeException(nameof(frameWidth));
        if (frameHeight <= 0) throw new ArgumentOutOfRangeException(nameof(frameHeight));

        _target = target;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        target.AddChild(this);
        ApplyGridFrame(0);
    }

    public Animator(SpriteNode target, SpriteAtlas atlas)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
        target.AddChild(this);
    }

    public void AddClip(string name, IEnumerable<int> frames, float fps, bool loop = true)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome do clip inválido.", nameof(name));
        var safeFrames = frames.Where(f => f >= 0).ToArray();
        if (safeFrames.Length == 0) safeFrames = new[] { 0 };
        if (!float.IsFinite(fps) || fps <= 0f) fps = 8f;
        _gridClips[name] = new GridClip(safeFrames, Math.Clamp(fps, 0.1f, 120f), loop);
    }

    public void AddAtlasClip(string name, IEnumerable<string> frameNames, float fps, bool loop = true)
    {
        if (_atlas is null) throw new InvalidOperationException("Este Animator não foi criado com SpriteAtlas.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome do clip inválido.", nameof(name));

        var safeFrames = frameNames
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim())
            .Where(f => _atlas.TryGetFrame(f, out _))
            .ToArray();

        if (safeFrames.Length == 0) throw new ArgumentException($"Clip '{name}' não possui frames válidos no atlas.", nameof(frameNames));
        if (!float.IsFinite(fps) || fps <= 0f) fps = 8f;
        _atlasClips[name] = new AtlasClip(safeFrames, Math.Clamp(fps, 0.1f, 120f), loop);
    }

    public bool Play(string name, bool restart = false)
    {
        if (_gridClips.TryGetValue(name, out var gridClip))
        {
            if (!restart && _playing && string.Equals(_currentName, name, StringComparison.OrdinalIgnoreCase)) return true;
            _currentGrid = gridClip;
            _currentAtlas = null;
            Start(name);
            ApplyGridFrame(gridClip.Frames[0]);
            return true;
        }

        if (_atlasClips.TryGetValue(name, out var atlasClip))
        {
            if (!restart && _playing && string.Equals(_currentName, name, StringComparison.OrdinalIgnoreCase)) return true;
            _currentAtlas = atlasClip;
            _currentGrid = null;
            Start(name);
            ApplyAtlasFrame(atlasClip.Frames[0]);
            return true;
        }

        return false;
    }

    public void Stop()
    {
        _playing = false;
        _finished = true;
    }

    public override void Update(float dt)
    {
        if (_playing)
        {
            if (!float.IsFinite(dt) || dt < 0f) dt = 0f;
            _elapsed += dt;
            var fps = _currentGrid?.Fps ?? _currentAtlas?.Fps ?? 8f;
            var frameDuration = 1f / fps;

            while (_elapsed >= frameDuration && _playing)
            {
                _elapsed -= frameDuration;
                _frameIndex++;

                if (_currentGrid is not null) AdvanceGrid();
                else if (_currentAtlas is not null) AdvanceAtlas();
                else _playing = false;
            }
        }

        base.Update(dt);
    }

    private void Start(string name)
    {
        _currentName = name;
        _frameIndex = 0;
        _elapsed = 0f;
        _playing = true;
        _finished = false;
    }

    private void Finish()
    {
        if (_finished) return;
        _playing = false;
        _finished = true;
        Completed?.Invoke(this, _currentName ?? string.Empty);
    }

    private void AdvanceGrid()
    {
        var clip = _currentGrid!;
        if (_frameIndex >= clip.Frames.Length)
        {
            if (clip.Loop) _frameIndex = 0;
            else { _frameIndex = clip.Frames.Length - 1; Finish(); }
        }
        ApplyGridFrame(clip.Frames[_frameIndex]);
    }

    private void AdvanceAtlas()
    {
        var clip = _currentAtlas!;
        if (_frameIndex >= clip.Frames.Length)
        {
            if (clip.Loop) _frameIndex = 0;
            else { _frameIndex = clip.Frames.Length - 1; Finish(); }
        }
        ApplyAtlasFrame(clip.Frames[_frameIndex]);
    }

    private void ApplyGridFrame(int index)
    {
        var tex = _target.Texture;
        if (tex is null || FrameWidth <= 0 || FrameHeight <= 0) return;
        var cols = Math.Max(1, tex.Width / FrameWidth);
        var x = (index % cols) * FrameWidth;
        var y = (index / cols) * FrameHeight;
        if (x >= tex.Width || y >= tex.Height) return;
        var w = Math.Min(FrameWidth, tex.Width - x);
        var h = Math.Min(FrameHeight, tex.Height - y);
        _target.SourceRect = new Rectangle(x, y, w, h);
    }

    private void ApplyAtlasFrame(string name)
    {
        if (_atlas is null) return;
        _target.SourceRect = _atlas.GetFrame(name);
    }
}
