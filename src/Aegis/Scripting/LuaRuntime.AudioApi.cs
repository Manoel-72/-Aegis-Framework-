using Aegis.Audio;
using Aegis.Core;
using Aegis.Display;
using Aegis.Physics;
using Microsoft.Xna.Framework;
using NLua;

namespace Aegis.Scripting;

public sealed partial class LuaRuntime
{
    public void PlaySound(string f)
    {
        try { AudioManager.PlaySound(f); }
        catch (Exception ex) { AegisLog.Warn("Audio", ex.Message); }
    }

    public void PlaySoundEx(string f, float v, float p, float n)
    {
        try { AudioManager.PlaySound(f, v, p, n); }
        catch (Exception ex) { AegisLog.Warn("Audio", ex.Message); }
    }

    public void PlayMusic(string f, bool loop = true)
    {
        try { AudioManager.PlayMusic(f, loop); }
        catch (Exception ex) { AegisLog.Warn("Audio", ex.Message); }
    }

    public void StopMusic() => AudioManager.StopMusic();

    public void PauseMusic() => AudioManager.PauseMusic();

    public void ResumeMusic() => AudioManager.ResumeMusic();

    public void SetSfxVolume(float v) => AudioManager.SetSfxVolume(v);

    public void SetMusicVolume(float v) => AudioManager.SetMusicVolume(v);

    public bool MusicPlaying() => AudioManager.IsMusicPlaying;

    public void PlaySoundAt(string file, float x, float y, LuaTable? opts = null)
    {
        float maxDist = TableFloat(opts, "maxDist", 600f);
        float volume = TableFloat(opts, "volume", 1f);
        var cam = Camera2D.Instance;
        try { AudioManager.PlaySoundAt(file, x, y, cam.X, cam.Y, cam.ViewWidth, cam.ViewHeight, maxDist, volume); }
        catch (Exception ex) { AegisLog.Warn("Audio", ex.Message); }
    }

    public void SetGroupVolume(string group, float volume) => AudioManager.SetGroupVolume(group, volume);

    public void CrossfadeTo(string file, float seconds = 1.5f) => AudioManager.CrossfadeTo(file, seconds);

    public void PlayMusicLooped(string intro, string loop) => AudioManager.PlayMusicLooped(intro, loop);

    /// <summary>aegis.playSoundAt3D(file, x, y, opts)
    /// opts: {maxDist=600, volume=1, rolloff="quadratic"|"linear", occlusion=false}</summary>
    public void PlaySoundAt3D(string file, float x, float y, LuaTable? opts = null)
    {
        float maxDist = TableFloat(opts, "maxDist", 600f);
        float volume = TableFloat(opts, "volume", 1f);
        string rolloff = opts?["rolloff"] as string ?? "quadratic";
        bool occlusion = opts?["occlusion"] is bool b && b;

        float camCx = Camera2D.Instance.X + Camera2D.Instance.ViewWidth * 0.5f;
        float camCy = Camera2D.Instance.Y + Camera2D.Instance.ViewHeight * 0.5f;

        float dx = x - camCx;
        float dy = y - camCy;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist >= maxDist) return;

        float atten = rolloff == "quadratic"
            ? MathF.Pow(1f - dist / maxDist, 2f)
            : 1f - dist / maxDist;

        if (occlusion)
        {
            var hit = CollisionSystem.Instance.Raycast(
                new Vector2(camCx, camCy),
                new Vector2(dx, dy),
                dist,
                ~0);
            if (hit is not null) atten *= 0.25f;
        }

        float pan = Math.Clamp(dx / (Camera2D.Instance.ViewWidth * 0.5f), -1f, 1f);
        pan = MathF.Sign(pan) * MathF.Pow(MathF.Abs(pan), 0.7f);

        AudioManager.PlaySound(file, volume * atten, 0f, pan);
    }
}
