using Aegis.Scene;
using Aegis.Core;
using NLua;

namespace Aegis.Effects;

/// <summary>
/// Tween runtime da Aegis: tween simples, callback, loop/yoyo e sequências.
/// Mantém a API antiga e adiciona uma camada de timeline para animações encadeadas.
/// </summary>
public sealed class TweenManager
{
    public static TweenManager Instance { get; } = new();
    private TweenManager() { }

    public sealed class SequenceHandle
    {
        internal readonly List<ISequenceStep> Steps = new();
        internal int Index;
        internal bool Playing;
    }

    internal interface ISequenceStep
    {
        bool Started { get; set; }
        bool Update(float dt);
        void Start();
    }

    private sealed class Tween
    {
        public required Object2D Obj;
        public Dictionary<string, float> Start = new();
        public Dictionary<string, float> End = new();
        public float Dur, T;
        public string Ease = "linear";
        public LuaFunction? OnComplete;
        public bool Loop;
        public bool Yoyo;
        public bool Reversed;
        public bool Completed;

        public void CaptureStart()
        {
            Start.Clear();
            foreach (var key in End.Keys)
                Start[key] = Get(Obj, key);
        }
    }

    private sealed class TweenStep : ISequenceStep
    {
        private readonly Tween _tween;
        public bool Started { get; set; }
        public TweenStep(Tween tween) => _tween = tween;
        public void Start()
        {
            _tween.T = 0f;
            _tween.Completed = false;
            _tween.CaptureStart();
            Started = true;
        }
        public bool Update(float dt) => UpdateTween(_tween, dt, removeOnComplete: false);
    }

    private sealed class WaitStep : ISequenceStep
    {
        private readonly float _duration;
        private float _elapsed;
        public bool Started { get; set; }
        public WaitStep(float duration) => _duration = Math.Clamp(duration, 0f, 3600f);
        public void Start() { _elapsed = 0f; Started = true; }
        public bool Update(float dt) { _elapsed += dt; return _elapsed >= _duration; }
    }

    private readonly List<Tween> _tweens = new();
    private readonly List<SequenceHandle> _sequences = new();

    public void Add(Object2D obj, LuaTable props, float dur, string ease)
        => Add(obj, props, dur, ease, null, null);

    public void Add(Object2D obj, LuaTable props, float dur, string ease, LuaFunction? onComplete, LuaTable? opts)
    {
        var t = CreateTween(obj, props, dur, ease, onComplete, opts);
        if (t.End.Count > 0) _tweens.Add(t);
    }

    public SequenceHandle NewSequence()
    {
        var seq = new SequenceHandle();
        _sequences.Add(seq);
        return seq;
    }

    public void SeqAdd(SequenceHandle seq, Object2D obj, LuaTable props, float dur, string ease, LuaFunction? onComplete = null, LuaTable? opts = null)
    {
        ArgumentNullException.ThrowIfNull(seq);
        var t = CreateTween(obj, props, dur, ease, onComplete, opts);
        if (t.End.Count > 0) seq.Steps.Add(new TweenStep(t));
    }

    public void SeqWait(SequenceHandle seq, float seconds)
    {
        ArgumentNullException.ThrowIfNull(seq);
        seq.Steps.Add(new WaitStep(seconds));
    }

    public void SeqPlay(SequenceHandle seq)
    {
        ArgumentNullException.ThrowIfNull(seq);
        seq.Index = 0;
        seq.Playing = true;
        foreach (var step in seq.Steps) step.Started = false;
    }

    public void SeqStop(SequenceHandle seq)
    {
        ArgumentNullException.ThrowIfNull(seq);
        seq.Playing = false;
    }

    public void Update(float dt)
    {
        if (!float.IsFinite(dt) || dt <= 0f) return;

        for (var i = _tweens.Count - 1; i >= 0; i--)
        {
            if (UpdateTween(_tweens[i], dt, removeOnComplete: true))
                _tweens.RemoveAt(i);
        }

        for (var i = _sequences.Count - 1; i >= 0; i--)
        {
            var seq = _sequences[i];
            if (!seq.Playing) continue;
            if (seq.Index < 0 || seq.Index >= seq.Steps.Count)
            {
                seq.Playing = false;
                continue;
            }

            var step = seq.Steps[seq.Index];
            if (!step.Started) step.Start();
            if (step.Update(dt))
            {
                seq.Index++;
                if (seq.Index >= seq.Steps.Count) seq.Playing = false;
            }
        }
    }

    public void Clear()
    {
        _tweens.Clear();
        _sequences.Clear();
    }

    /// <summary>Alias para Clear() — mata todos os tweens e sequências ativos.
    /// Chamado em ClearAll() para evitar tweens zumbis após troca de cena.</summary>
    public void KillAll() => Clear();

    private static Tween CreateTween(Object2D obj, LuaTable props, float dur, string ease, LuaFunction? onComplete, LuaTable? opts)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(props);

        if (!float.IsFinite(dur) || dur <= 0f)
        {
            AegisLog.Warn("Tween", $"Duração inválida ({dur}); usando 0.001s.");
            dur = 0.001f;
        }

        bool loop = TableBool(opts, "loop", false);
        bool yoyo = TableBool(opts, "yoyo", false);

        var t = new Tween
        {
            Obj = obj,
            Dur = Math.Clamp(dur, 0.001f, 3600f),
            Ease = string.IsNullOrWhiteSpace(ease) ? "linear" : ease,
            OnComplete = onComplete,
            Loop = loop,
            Yoyo = yoyo
        };

        foreach (var keyObj in props.Keys)
        {
            var key = keyObj?.ToString();
            if (string.IsNullOrWhiteSpace(key)) continue;
            try
            {
                var end = Convert.ToSingle(props[keyObj]);
                if (!float.IsFinite(end)) continue;
                t.End[key] = end;
                t.Start[key] = Get(obj, key);
            }
            catch { }
        }
        return t;
    }

    private static bool UpdateTween(Tween t, float dt, bool removeOnComplete)
    {
        if (t.Completed) return true;

        t.T += dt;
        var raw = Math.Clamp(t.T / t.Dur, 0f, 1f);
        var u = Ease(raw, t.Ease);
        foreach (var kv in t.End)
        {
            var start = t.Start[kv.Key];
            var end = kv.Value;
            if (t.Reversed) (start, end) = (end, start);
            Set(t.Obj, kv.Key, start + (end - start) * u);
        }

        if (t.T < t.Dur) return false;

        if (t.Loop)
        {
            t.T = 0f;
            if (t.Yoyo) t.Reversed = !t.Reversed;
            else t.CaptureStart();
            return false;
        }

        t.Completed = true;
        try { t.OnComplete?.Call(); }
        catch (Exception ex) { AegisLog.Error("Tween", $"Erro em callback de tween: {ex.Message}"); }
        return true;
    }

    private static float Ease(float t, string ease) => ease.ToLowerInvariant() switch
    {
        "in" or "easein" => t * t,
        "out" or "easeout" => 1f - MathF.Pow(1f - t, 2f),
        "inout" or "easeinout" => t < .5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f,
        _ => t
    };

    private static float Get(Object2D o, string k) => k switch
    {
        "x" or "X" => o.X,
        "y" or "Y" => o.Y,
        "alpha" => o.Alpha,
        "rotation" => o.Rotation,
        "scaleX" => o.ScaleX,
        "scaleY" => o.ScaleY,
        "scale" => o.ScaleX,
        _ => 0f
    };

    private static void Set(Object2D o, string k, float v)
    {
        if (!float.IsFinite(v)) return;
        switch (k)
        {
            case "x": case "X": o.X = v; break;
            case "y": case "Y": o.Y = v; break;
            case "alpha": o.Alpha = Math.Clamp(v, 0f, 1f); break;
            case "rotation": o.Rotation = v; break;
            case "scaleX": o.ScaleX = v; break;
            case "scaleY": o.ScaleY = v; break;
            case "scale": o.ScaleX = o.ScaleY = v; break;
        }
    }

    private static bool TableBool(LuaTable? table, string key, bool fallback)
    {
        try { return table?[key] is null ? fallback : Convert.ToBoolean(table[key]); }
        catch { return fallback; }
    }
}
