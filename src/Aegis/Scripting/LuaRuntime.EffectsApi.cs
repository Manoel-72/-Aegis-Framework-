using Aegis.Display;
using Aegis.Effects;
using Aegis.Scene;
using Microsoft.Xna.Framework;
using NLua;

namespace Aegis.Scripting;

public sealed partial class LuaRuntime
{
    public void Burst(float x, float y, LuaTable? opts = null)
    {
        _particles ??= new ParticleSystem2D();
        if (_particles.Parent is null) _app.S2D.AddChild(_particles);
        var o = ParticleSystem2D.ParseOptions(opts);
        _particles.Burst(x, y, o.count, o.speed, o.life, o.size, o.r, o.g, o.b);
    }

    public ParticleSystem2D.EmitterHandle NewEmitter(float x, float y, LuaTable? opts = null)
    {
        _particles ??= new ParticleSystem2D();
        if (_particles.Parent is null) _app.S2D.AddChild(_particles);

        float F(string key, float fallback)
        {
            try { return opts?[key] is null ? fallback : Convert.ToSingle(opts[key]); }
            catch { return fallback; }
        }

        var rate = F("rate", 16f);
        var duration = F("duration", -1f);
        var emitterOpts = ParticleSystem2D.ParseEmitterOptions(opts);
        return _particles.StartEmitter(x, y, rate, duration, emitterOpts);
    }

    public void StopEmitter(ParticleSystem2D.EmitterHandle handle)
        => _particles?.StopEmitter(handle);

    public void Tween(Object2D obj, LuaTable props, float duration, string ease = "linear", LuaFunction? onComplete = null, LuaTable? opts = null)
        => TweenManager.Instance.Add(obj, props, duration, ease, onComplete, opts);

    public TweenManager.SequenceHandle NewSequence() => TweenManager.Instance.NewSequence();

    public void SeqAdd(TweenManager.SequenceHandle seq, Object2D obj, LuaTable props, float duration, string ease = "linear", LuaFunction? onComplete = null, LuaTable? opts = null)
        => TweenManager.Instance.SeqAdd(seq, obj, props, duration, ease, onComplete, opts);

    public void SeqWait(TweenManager.SequenceHandle seq, float seconds)
        => TweenManager.Instance.SeqWait(seq, seconds);

    public void SeqPlay(TweenManager.SequenceHandle seq)
        => TweenManager.Instance.SeqPlay(seq);

    public void SeqStop(TweenManager.SequenceHandle seq)
        => TweenManager.Instance.SeqStop(seq);

    public void FadeIn(float duration = 0.35f) => ScreenEffects.Instance.FadeIn(duration);
    public void FadeOut(float duration = 0.35f) => ScreenEffects.Instance.FadeOut(duration);

    public void FlashScreen(object? color = null, float duration = 0.12f)
    {
        var c = Color.White;
        if (color is LuaTable t)
        {
            float R(string k, float d) { try { return t[k] is null ? d : Convert.ToSingle(t[k]); } catch { return d; } }
            c = new Color(R("r", 1f), R("g", 1f), R("b", 1f));
        }
        ScreenEffects.Instance.Flash(c, duration);
    }

    public void SetShader(Object2D obj, string name, LuaTable? opts = null)
    {
        var cfg = new ObjectShaderConfig { Name = (name ?? string.Empty).ToLowerInvariant() };
        cfg.Width = TableFloat(opts, "width", 1f);
        cfg.Progress = TableFloat(opts, "progress", 0f);
        cfg.Color = new Color(TableFloat(opts, "r", 1f), TableFloat(opts, "g", 1f), TableFloat(opts, "b", 1f));
        obj.Shader = cfg;
    }

    public void ClearShader(Object2D obj) => obj.Shader = null;

    public void SetScreenShader(string name, LuaTable? opts = null)
        => ShaderManager.SetScreenShader(name, TableFloat(opts, "intensity", 0.5f));

    public void ClearScreenShader() => ShaderManager.ClearScreenShader();

    // ════════════════════════════════════════════════════════════════
    //  Sprint 2 — APIs de jogo urgentes
    // ════════════════════════════════════════════════════════════════
}