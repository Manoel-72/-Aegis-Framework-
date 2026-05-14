using Aegis.Core;
using Aegis.Resource;
using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLua;

namespace Aegis.Effects;

public sealed class ParticleSystem2D : Object2D
{
    private struct Particle
    {
        public Vector2 Pos, Vel;
        public float Life, MaxLife, Size;
        public Color Color;
    }

    public sealed class EmitterHandle
    {
        internal EmitterHandle(int id) => Id = id;
        internal int Id { get; }
        public bool IsStopped { get; internal set; }
    }

    public sealed class EmitterOptions
    {
        public float Speed { get; set; } = 160f;
        public float Life { get; set; } = 0.45f;
        public float Size { get; set; } = 4f;
        public float R { get; set; } = 1f;
        public float G { get; set; } = 1f;
        public float B { get; set; } = 1f;
        public float Spread { get; set; } = 360f;
        public float Angle { get; set; } = -90f;
        public Object2D? Follow { get; set; }
    }

    private sealed class EmitterConfig
    {
        public int Id;
        public float X;
        public float Y;
        public float Rate;
        public float Duration;
        public float Elapsed;
        public float Accumulator;
        public Object2D? Follow;
        public EmitterOptions Opts = new();
    }

    private readonly List<Particle> _particles = new();
    private readonly List<EmitterConfig> _emitters = new();
    private readonly Random _rng = new();
    private int _nextEmitterId = 1;

    public int Count => _particles.Count;
    public int EmitterCount => _emitters.Count;

    public void Burst(float x, float y, int count = 16, float speed = 160f, float life = 0.45f,
                      float size = 4f, float r = 1f, float g = 1f, float b = 1f)
    {
        var opts = new EmitterOptions { Speed = speed, Life = life, Size = size, R = r, G = g, B = b, Spread = 360f };
        count = Math.Clamp(count, 1, 512);
        for (var i = 0; i < count; i++) EmitOne(x, y, opts);
    }

    public EmitterHandle StartEmitter(float x, float y, float rate, float duration, EmitterOptions opts)
    {
        if (!float.IsFinite(rate) || rate <= 0f)
        {
            AegisLog.Warn("ParticleSystem", "Emitter criado com rate inválido; usando 1 partícula/s.");
            rate = 1f;
        }
        if (!float.IsFinite(duration)) duration = -1f;

        var id = _nextEmitterId++;
        _emitters.Add(new EmitterConfig
        {
            Id = id,
            X = float.IsFinite(x) ? x : 0f,
            Y = float.IsFinite(y) ? y : 0f,
            Rate = Math.Clamp(rate, 0.01f, 5000f),
            Duration = duration < 0f ? -1f : duration,
            Follow = opts.Follow,
            Opts = opts
        });
        return new EmitterHandle(id);
    }

    public void StopEmitter(EmitterHandle? handle)
    {
        if (handle is null) return;
        if (_emitters.RemoveAll(e => e.Id == handle.Id) > 0)
            handle.IsStopped = true;
    }

    /// <summary>Remove todas as partículas em voo e para todos os emitters.
    /// Chamado em LuaRuntime.ClearAll() para evitar partículas zumbis após troca de cena.</summary>
    public void ClearAll()
    {
        foreach (var e in _emitters) e.Id = -1; // marca todos como parados
        _emitters.Clear();
        _particles.Clear();
    }

    public override void Update(float dt)
    {
        if (!float.IsFinite(dt) || dt < 0f) dt = 0f;

        for (int i = _emitters.Count - 1; i >= 0; i--)
        {
            var e = _emitters[i];
            e.Elapsed += dt;
            if (e.Duration >= 0f && e.Elapsed >= e.Duration)
            {
                _emitters.RemoveAt(i);
                continue;
            }

            e.Accumulator += e.Rate * dt;
            var emitCount = Math.Min(256, (int)e.Accumulator);
            if (emitCount > 0) e.Accumulator -= emitCount;

            var baseX = e.Follow is null ? e.X : e.Follow.WorldPosition.X + e.X;
            var baseY = e.Follow is null ? e.Y : e.Follow.WorldPosition.Y + e.Y;
            for (var n = 0; n < emitCount; n++) EmitOne(baseX, baseY, e.Opts);
        }

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Life -= dt;
            if (p.Life <= 0f) { _particles.RemoveAt(i); continue; }
            p.Pos += p.Vel * dt;
            p.Vel *= MathF.Pow(0.02f, dt);
            _particles[i] = p;
        }
        base.Update(dt);
    }

    public override void Draw(SpriteBatch sb, float inheritedAlpha = 1f)
    {
        foreach (var p in _particles)
        {
            var alpha = Math.Clamp(p.Life / p.MaxLife, 0f, 1f) * inheritedAlpha;
            var size = Math.Max(1, (int)p.Size);
            var rect = new Rectangle((int)(p.Pos.X - p.Size * 0.5f), (int)(p.Pos.Y - p.Size * 0.5f), size, size);
            sb.Draw(ResManager.Pixel, rect, p.Color * alpha);
        }
        base.Draw(sb, inheritedAlpha);
    }

    private void EmitOne(float x, float y, EmitterOptions opts)
    {
        var life = MathF.Max(0.02f, Safe(opts.Life, 0.45f));
        var speed = MathF.Max(0f, Safe(opts.Speed, 160f));
        var spread = Math.Clamp(Safe(opts.Spread, 360f), 0f, 360f);
        var angle = Safe(opts.Angle, -90f);

        float degrees = spread >= 359.9f
            ? (float)(_rng.NextDouble() * 360.0)
            : angle - spread * 0.5f + (float)_rng.NextDouble() * spread;

        var a = degrees * MathF.PI / 180f;
        var s = speed * (0.35f + (float)_rng.NextDouble() * 0.75f);
        _particles.Add(new Particle
        {
            Pos = new Vector2(x, y),
            Vel = new Vector2(MathF.Cos(a), MathF.Sin(a)) * s,
            Life = life,
            MaxLife = life,
            Size = MathF.Max(1f, Safe(opts.Size, 4f)),
            Color = new Color(Clamp01(opts.R), Clamp01(opts.G), Clamp01(opts.B))
        });
    }

    public static (int count, float speed, float life, float size, float r, float g, float b) ParseOptions(LuaTable? opts)
    {
        int count = 16; float speed = 160f, life = 0.45f, size = 4f, r = 1f, g = 1f, b = 1f;
        if (opts is null) return (count, speed, life, size, r, g, b);
        object? Get(string k) => opts[k];
        try { if (Get("count") is not null) count = Convert.ToInt32(Get("count")); } catch { }
        try { if (Get("speed") is not null) speed = Convert.ToSingle(Get("speed")); } catch { }
        try { if (Get("life") is not null) life = Convert.ToSingle(Get("life")); } catch { }
        try { if (Get("size") is not null) size = Convert.ToSingle(Get("size")); } catch { }
        try { if (Get("r") is not null) r = Convert.ToSingle(Get("r")); } catch { }
        try { if (Get("g") is not null) g = Convert.ToSingle(Get("g")); } catch { }
        try { if (Get("b") is not null) b = Convert.ToSingle(Get("b")); } catch { }
        return (count, speed, life, size, r, g, b);
    }

    public static EmitterOptions ParseEmitterOptions(LuaTable? opts)
    {
        var o = new EmitterOptions();
        if (opts is null) return o;
        object? Get(string k) => opts[k];
        float F(string k, float fallback) { try { return Get(k) is null ? fallback : Convert.ToSingle(Get(k)); } catch { return fallback; } }

        o.Speed = F("speed", o.Speed);
        o.Life = F("life", o.Life);
        o.Size = F("size", o.Size);
        o.R = F("r", o.R);
        o.G = F("g", o.G);
        o.B = F("b", o.B);
        o.Spread = F("spread", o.Spread);
        o.Angle = F("angle", o.Angle);
        if (Get("follow") is Object2D follow) o.Follow = follow;
        else if (Get("followX") is Object2D followX) o.Follow = followX;
        return o;
    }

    private static float Safe(float value, float fallback) => float.IsFinite(value) ? value : fallback;
    private static float Clamp01(float value) => Math.Clamp(Safe(value, 1f), 0f, 1f);
}
