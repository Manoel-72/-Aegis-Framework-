using Aegis.Display;
using Aegis.Resource;
using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLua;

namespace Aegis.Scripting;

public sealed partial class LuaRuntime
{
    public Label NewLabel(string text, bool hud = false)
        => _components.CreateLabel(text, hud);
    public Label NewLabelSize(string text, int size, bool hud = false)
        => _components.CreateLabel(text, size, hud);
    public void SetText(Label l, string t)                                        => l.Text = t;
    /// <summary>Define cor do Label. Alpha opcional (padrão 1.0 = opaco).
    /// BUG #6 fix: parâmetro alpha adicionado para suportar texto semitransparente.</summary>
    public void SetColor(Label l, float r, float g, float b, float a = 1f)        => l.Color = new Color(r, g, b, a);

    // ── AnimatedSprite ────────────────────────────────────────────────
    public AnimatedSprite NewAnim(string path, int fw, int fh)
        => _components.CreateAnimatedSprite(path, fw, fh);
    public void PlayAnim(AnimatedSprite a, int s, int e, bool loop, float fps)
        => a.Play(s, e, loop, fps);
    public void StopAnim(AnimatedSprite a)    => a.Stop();
    public void ResumeAnim(AnimatedSprite a)  => a.Resume();
    public int  AnimFrame(AnimatedSprite a)   => a.CurrentFrame;
    public bool AnimPlaying(AnimatedSprite a) => a.IsPlaying;

    // ── Camera2D ──────────────────────────────────────────────────────
    public void  SetCameraTarget(Object2D t, float speed = 5f) => Camera2D.Instance.SetTarget(t, speed);
    public void  SetCameraOff()                => Camera2D.Instance.Deactivate();
    public void  SetCameraZoom(float z)        => Camera2D.Instance.SetZoom(z);
    public void  SetCameraOffset(float ox, float oy) => Camera2D.Instance.SetOffset(ox, oy);
    public void  SetCameraLimits(float l, float t, float r, float b) => Camera2D.Instance.SetLimits(l, t, r, b);
    public void  SetCameraDeadzone(float width, float height) => Camera2D.Instance.SetDeadzone(width, height);
    public void  SetCameraLookahead(float distance, float speed = 4f) => Camera2D.Instance.SetLookahead(distance, speed);
    public float GetCameraX()                  => Camera2D.Instance.X;
    public float GetCameraY()                  => Camera2D.Instance.Y;
    public float ScreenToWorldX(float sx, float sy) => Camera2D.Instance.ScreenToWorld(sx, sy).X;
    public float ScreenToWorldY(float sx, float sy) => Camera2D.Instance.ScreenToWorld(sx, sy).Y;

    // ── ScreenShake ───────────────────────────────────────────────────
    public void ScreenShake(float intensity, float duration)
    { _shakeIntensity = intensity; _shakeTime = duration; }

    internal void UpdateShake(float dt)
    {
        if (_shakeTime <= 0f) return;
        _shakeTime -= dt;
        var cam = Camera2D.Instance;
        if (_shakeTime > 0f)
            cam.SetOffset(
                (float)(_rng.NextDouble() * 2 - 1) * _shakeIntensity,
                (float)(_rng.NextDouble() * 2 - 1) * _shakeIntensity);
        else { _shakeTime = 0f; cam.SetOffset(0f, 0f); }
    }

    // ── RichLabel ─────────────────────────────────────────────────────
    public RichLabel NewRichLabel(string markup)
        => _components.CreateRichLabel(markup);
    public RichLabel NewRichLabelSize(string markup, int size)
        => _components.CreateRichLabel(markup, size);
    public void SetMarkup(RichLabel rl, string m)             => rl.Markup = m;
    public void SetPivotRich(RichLabel rl, float px, float py) => rl.Pivot = new Vector2(px, py);

    // ── Font ──────────────────────────────────────────────────────────
    public SpriteFont LoadFont(string file, int size) => FontManager.Load(file, size);
    public SpriteFont LoadDefaultFont(int size) => FontManager.LoadDefault(size);
    public void SetFont(Label l, SpriteFont font)     => l.Font = font;
    public void SetFontRich(RichLabel rl, SpriteFont font) => rl.Font = font;

    // ── NineSlice ─────────────────────────────────────────────────────
    public NineSlice NewPanel(string path, int border)
        => _components.CreatePanel(path, border);
    public void SetPanelSize(NineSlice p, int w, int h) { p.Width = w; p.Height = h; }

    public FlowContainer NewFlow(string direction, LuaTable? opts = null)
    {
        return _components.CreateFlow(direction, opts);
    }

    public void FlowAdd(FlowContainer flow, Object2D child)
    {
        Require(flow, nameof(FlowAdd));
        Require(child, nameof(FlowAdd));
        flow.AddFlowChild(child);
    }

    public void FlowLayout(FlowContainer flow)
    {
        Require(flow, nameof(FlowLayout));
        flow.Layout();
    }

    public void FlowSet(FlowContainer flow, LuaTable opts)
    {
        Require(flow, nameof(FlowSet));
        Require(opts, nameof(FlowSet));
        flow.Configure(
            TableString(opts, "direction", flow.Direction),
            TableFloat(opts, "gap", flow.Gap),
            TableFloat(opts, "padding", flow.Padding),
            TableString(opts, "align", flow.Align));
    }

    // ════════════════════════════════════════════════════════════════
    //  v0.5 — Sprites & Assets
    // ════════════════════════════════════════════════════════════════

    /// aegis.setFrame(sprite, x, y, w, h) — recorte de spritesheet em pixels.
    public void SetFrame(SpriteNode sprite, int x, int y, int w, int h)
        => sprite.SetFrame(x, y, w, h);

    public void ClearFrame(SpriteNode sprite) => sprite.ClearFrame();

    /// aegis.loadAtlas("player.json") — carrega JSON exportado pelo Aseprite dentro da pasta res/.
    public SpriteAtlas LoadAtlas(string jsonPath) => SpriteAtlas.Load(jsonPath);

    /// aegis.setAtlasFrame(sprite, atlas, "run_00") — aplica frame nomeado do atlas no sprite.
    public void SetAtlasFrame(SpriteNode sprite, SpriteAtlas atlas, string frameName)
        => sprite.SourceRect = atlas.GetFrame(frameName);

    // ════════════════════════════════════════════════════════════════
    //  v0.6 — Animator
    // ════════════════════════════════════════════════════════════════

    /// aegis.newAnimator(sprite, frameW, frameH)
    public Animator NewAnimator(SpriteNode sprite, int frameW, int frameH)
        => new Animator(sprite, frameW, frameH);

    /// aegis.newAtlasAnimator(sprite, atlas)
    public Animator NewAtlasAnimator(SpriteNode sprite, SpriteAtlas atlas)
        => new Animator(sprite, atlas);

    /// aegis.addClip(anim, name, {0,1,2,3}, fps [, loop])
    public void AddClip(Animator anim, string name, LuaTable frames, float fps, bool loop = true)
    {
        var list = new List<int>();
        foreach (var value in frames.Values)
        {
            try { list.Add(Convert.ToInt32(value)); }
            catch { /* ignora valores inválidos vindos do Lua */ }
        }
        anim.AddClip(name, list, fps, loop);
    }

    /// aegis.addAtlasClip(anim, "run", {"run_00", "run_01"}, fps [, loop])
    public void AddAtlasClip(Animator anim, string name, LuaTable frames, float fps, bool loop = true)
    {
        var list = new List<string>();
        foreach (var value in frames.Values)
        {
            if (value is null) continue;
            var frame = Convert.ToString(value);
            if (!string.IsNullOrWhiteSpace(frame)) list.Add(frame);
        }
        anim.AddAtlasClip(name, list, fps, loop);
    }

    /// aegis.play(anim, "idle" [, restart])
    public bool Play(Animator anim, string name, bool restart = false)
        => anim.Play(name, restart);

    public void StopAnimator(Animator anim) => anim.Stop();
    public string CurrentClip(Animator anim) => anim.CurrentClip ?? string.Empty;

    // ── Utils ─────────────────────────────────────────────────────────
    public void DrawText(string text, float x, float y, float r = 1f, float g = 1f, float b = 1f, float a = 1f)
    {
        if (FontManager.Default is null) return;
        Renderer.SpriteBatch.DrawString(FontManager.Default, text, new Vector2(x, y), new Color(r, g, b, a));
    }

    public void DrawRect(float x, float y, float w, float h, float r = 1f, float g = 1f, float b = 1f, float a = 1f)
    {
        if (w <= 0f || h <= 0f) return;
        Renderer.SpriteBatch.Draw(ResManager.Pixel, new Rectangle((int)x, (int)y, (int)w, (int)h), new Color(r, g, b, a));
    }

    /// <summary>Desenha textura em espaço de tela (HUD). path relativo a res/.</summary>
    public void DrawSprite(string path, float x, float y, float scale = 1f, float r = 1f, float g = 1f, float b = 1f, float a = 1f)
    {
        var tex = ResManager.LoadTexture(path);
        if (tex is null) return;
        scale = MathF.Max(0.1f, scale);
        var w = tex.Width * scale;
        var h = tex.Height * scale;
        Renderer.SpriteBatch.Draw(tex, new Rectangle((int)x, (int)y, (int)w, (int)h), new Color(r, g, b, a));
    }

    public void DrawLine(float x1, float y1, float x2, float y2, float thickness = 1f, float r = 1f, float g = 1f, float b = 1f)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var len = MathF.Sqrt(dx * dx + dy * dy);
        if (len <= 0.001f) return;
        var rot = MathF.Atan2(dy, dx);

        Renderer.SpriteBatch.Draw(
            ResManager.Pixel,
            new Vector2(x1, y1),
            null,
            new Color(r, g, b),
            rot,
            Vector2.Zero,
            new Vector2(len, MathF.Max(1f, thickness)),
            SpriteEffects.None,
            0f);
    }

    public void DrawCircle(float cx, float cy, float radius, float r = 1f, float g = 1f, float b = 1f)
    {
        var rr = (int)MathF.Max(0f, radius);
        if (rr <= 0) return;

        var col = new Color(r, g, b);
        int r2 = rr * rr;
        for (int y = -rr; y <= rr; y++)
        {
            int x = (int)MathF.Sqrt(MathF.Max(0f, r2 - y * y));
            Renderer.SpriteBatch.Draw(ResManager.Pixel, new Rectangle((int)cx - x, (int)cy + y, x * 2 + 1, 1), col);
        }
    }

    // ── Script ────────────────────────────────────────────────────────
    // ════════════════════════════════════════════════════════════════
    //  v0.8 — Save, Config, Effects, Hot Reload
    // ════════════════════════════════════════════════════════════════
}