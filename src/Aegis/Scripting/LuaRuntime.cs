using Aegis.Audio;
using Aegis.Core;
using Aegis.Display;
using Aegis.Input;
using Aegis.Physics;
using Aegis.Resource;
using Aegis.Scene;
using Aegis.World;
using Aegis.Effects;
using Aegis.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLua;

namespace Aegis.Scripting;

/// <summary>
/// Integração NLua: registra a API aegis.* e gerencia ciclo de vida dos scripts.
/// v0.4: CollisionSystem AABB · layers · onCollide · Rigidbody2D · Raycast
/// </summary>
public sealed class LuaRuntime : IDisposable
{
    private readonly Lua _lua;
    private readonly App _app;
    private static readonly Random _rng = new();

    private float _shakeTime;
    private float _shakeIntensity;

    // Mapa de Colliders para manter referência e permitir remoção
    private readonly Dictionary<int, Collider> _colliders = new();
    private int _colliderIdSeq = 0;
    private string _gameRoot = Directory.GetCurrentDirectory();

    private string _mainLuaFullPath = string.Empty;
    private ParticleSystem2D? _particles;

    public LuaRuntime(App app)
    {
        _app = app;
        _lua = new Lua();
        _lua.State.Encoding = System.Text.Encoding.UTF8;
        SceneManager.Instance.Initialize(app, this);
    }

    public void RegisterAll()
    {
        _lua.NewTable("aegis");
        _lua["aegis_init"]    = null;
        _lua["aegis_update"]  = null;
        _lua["aegis_draw"]    = null;
        _lua["aegis_draw_ui"] = null; // BUG #3: UI layer sem transformação da câmera

        // ── Core ────────────────────────────────────────────────────
        Reg("aegis.newSprite",       nameof(NewSprite));
        Reg("aegis.newRect",         nameof(NewRect));
        Reg("aegis.removeObject",    nameof(RemoveObject));

        // ── Transform ───────────────────────────────────────────────
        Reg("aegis.setPosition",     nameof(SetPosition));
        Reg("aegis.setPositionNorm", nameof(SetPositionNorm));
        Reg("aegis.centerX",         nameof(CenterX));
        Reg("aegis.setZ",            nameof(SetZ));
        Reg("aegis.getZ",            nameof(GetZ));
        Reg("aegis.setZOrder",       nameof(SetZ));
        Reg("aegis.getZOrder",       nameof(GetZ));
        Reg("aegis.move",            nameof(Move));
        Reg("aegis.setScale",        nameof(SetScale));
        Reg("aegis.setRotation",     nameof(SetRotation));
        Reg("aegis.setAlpha",        nameof(SetAlpha));
        Reg("aegis.setVisible",      nameof(SetVisible));
        Reg("aegis.setPivot",        nameof(SetPivot));
        Reg("aegis.getX",            nameof(GetX));
        Reg("aegis.getY",            nameof(GetY));
        Reg("aegis.getWidth",        nameof(GetWidth));
        Reg("aegis.getHeight",       nameof(GetHeight));

        // ── Label ───────────────────────────────────────────────────
        Reg("aegis.newLabel",        nameof(NewLabel));
        Reg("aegis.setText",         nameof(SetText));
        Reg("aegis.setColor",        nameof(SetColor));

        // ── Input ───────────────────────────────────────────────────
        Reg("aegis.keyDown",         nameof(KeyDown));
        Reg("aegis.keyPressed",      nameof(KeyPressed));
        Reg("aegis.mouseX",          nameof(GetMouseX));
        Reg("aegis.mouseY",          nameof(GetMouseY));
        Reg("aegis.mouseLeft",       nameof(MouseLeft));
        Reg("aegis.mouseLeftJust",   nameof(MouseLeftJust));
        Reg("aegis.mouseRight",      nameof(MouseRight));
        Reg("aegis.mouseRightJust",  nameof(MouseRightJust));
        Reg("aegis.mouseScroll",     nameof(GetScrollDelta));
        Reg("aegis.padConnected",     nameof(PadConnected));
        Reg("aegis.padDown",          nameof(PadDown));
        Reg("aegis.padPressed",       nameof(PadPressed));
        Reg("aegis.padAxis",          nameof(PadAxis));
        Reg("aegis.padVibrate",       nameof(PadVibrate));

        // ── Screen ──────────────────────────────────────────────────
        Reg("aegis.screenWidth",     nameof(GetScreenWidth));
        Reg("aegis.screenHeight",    nameof(GetScreenHeight));

        // ── Utils ───────────────────────────────────────────────────
        Reg("aegis.log",             nameof(Log));
        Reg("aegis.randomInt",       nameof(RandomInt));
        Reg("aegis.randomFloat",     nameof(RandomFloat));
        Reg("aegis.clearAll",        nameof(ClearAll));
        Reg("aegis.worldClear",      nameof(WorldClear));
        Reg("aegis.drawText",        nameof(DrawText));
        Reg("aegis.drawRect",        nameof(DrawRect));
        Reg("aegis.drawLine",        nameof(DrawLine));
        Reg("aegis.drawCircle",      nameof(DrawCircle));

        // ── v0.2 AnimatedSprite ─────────────────────────────────────
        Reg("aegis.newAnim",         nameof(NewAnim));
        Reg("aegis.playAnim",        nameof(PlayAnim));
        Reg("aegis.stopAnim",        nameof(StopAnim));
        Reg("aegis.resumeAnim",      nameof(ResumeAnim));
        Reg("aegis.animFrame",       nameof(AnimFrame));
        Reg("aegis.animPlaying",     nameof(AnimPlaying));

        // ── v0.2 Camera2D ───────────────────────────────────────────
        Reg("aegis.setCameraTarget", nameof(SetCameraTarget));
        Reg("aegis.setCameraOff",    nameof(SetCameraOff));
        Reg("aegis.setCameraZoom",   nameof(SetCameraZoom));
        Reg("aegis.setCameraOffset", nameof(SetCameraOffset));
        Reg("aegis.setCameraLimits", nameof(SetCameraLimits));
        Reg("aegis.setCameraDeadzone", nameof(SetCameraDeadzone));
        Reg("aegis.setCameraLookahead", nameof(SetCameraLookahead));
        Reg("aegis.cameraX",         nameof(GetCameraX));
        Reg("aegis.cameraY",         nameof(GetCameraY));
        Reg("aegis.screenToWorldX",  nameof(ScreenToWorldX));
        Reg("aegis.screenToWorldY",  nameof(ScreenToWorldY));

        // ── v0.2 ScreenShake ────────────────────────────────────────
        Reg("aegis.screenShake",     nameof(ScreenShake));

        // ── v0.3 Audio ──────────────────────────────────────────────
        Reg("aegis.playSound",       nameof(PlaySound));
        Reg("aegis.playSoundEx",     nameof(PlaySoundEx));
        Reg("aegis.playMusic",       nameof(PlayMusic));
        Reg("aegis.stopMusic",       nameof(StopMusic));
        Reg("aegis.pauseMusic",      nameof(PauseMusic));
        Reg("aegis.resumeMusic",     nameof(ResumeMusic));
        Reg("aegis.setSfxVolume",    nameof(SetSfxVolume));
        Reg("aegis.setMusicVolume",  nameof(SetMusicVolume));
        Reg("aegis.musicPlaying",    nameof(MusicPlaying));
        Reg("aegis.playSoundAt",      nameof(PlaySoundAt));
        Reg("aegis.setGroupVolume",   nameof(SetGroupVolume));
        Reg("aegis.crossfadeTo",      nameof(CrossfadeTo));
        Reg("aegis.playMusicLooped",  nameof(PlayMusicLooped));

        // ── v0.3 RichLabel ──────────────────────────────────────────
        Reg("aegis.newRichLabel",    nameof(NewRichLabel));
        Reg("aegis.setMarkup",       nameof(SetMarkup));
        Reg("aegis.setPivotRich",    nameof(SetPivotRich));

        // ── v0.3 Font TTF ───────────────────────────────────────────
        Reg("aegis.loadFont",        nameof(LoadFont));
        Reg("aegis.setFont",         nameof(SetFont));
        Reg("aegis.setFontRich",     nameof(SetFontRich));

        // ── v0.3 NineSlice ──────────────────────────────────────────
        Reg("aegis.newPanel",        nameof(NewPanel));
        Reg("aegis.setPanelSize",    nameof(SetPanelSize));
        Reg("aegis.newFlow",         nameof(NewFlow));
        Reg("aegis.flowAdd",         nameof(FlowAdd));
        Reg("aegis.flowLayout",      nameof(FlowLayout));
        Reg("aegis.flowSet",         nameof(FlowSet));

        // ── v0.4 Collider & CollisionSystem ─────────────────────────
        Reg("aegis.addCollider",     nameof(AddCollider));
        Reg("aegis.addCircleCollider", nameof(AddCircleCollider));
        Reg("aegis.removeCollider",  nameof(RemoveCollider));
        Reg("aegis.setColliderLayer",nameof(SetColliderLayer));
        Reg("aegis.setColliderMask", nameof(SetColliderMask));
        Reg("aegis.setTrigger",      nameof(SetTrigger));
        Reg("aegis.setColliderOffset",nameof(SetColliderOffset));
        Reg("aegis.onCollide",       nameof(OnCollide));
        Reg("aegis.onCollideEnter",  nameof(OnCollideEnter));
        Reg("aegis.onCollideExit",   nameof(OnCollideExit));
        Reg("aegis.getColliderOf",   nameof(GetColliderOf));

        // ── v0.4 Rigidbody2D ────────────────────────────────────────
        Reg("aegis.addRigidbody",    nameof(AddRigidbody));
        Reg("aegis.removeRigidbody", nameof(RemoveRigidbody));
        Reg("aegis.setVelocity",     nameof(SetVelocity));
        Reg("aegis.setVelocityX",    nameof(SetVelocityX));
        Reg("aegis.addImpulseY",     nameof(AddImpulseY));
        Reg("aegis.setVel",          nameof(SetVel));
        Reg("aegis.setVelX",         nameof(SetVelX));
        Reg("aegis.jumpY",           nameof(JumpY));
        Reg("aegis.addVelocity",     nameof(AddVelocity));
        Reg("aegis.getVelocityX",    nameof(GetVelocityX));
        Reg("aegis.getVelocityY",    nameof(GetVelocityY));
        Reg("aegis.setGravity",      nameof(SetGravity));
        Reg("aegis.setGroundFriction", nameof(SetGroundFriction));
        Reg("aegis.setGlobalGravity",nameof(SetGlobalGravity));
        Reg("aegis.setKinematic",    nameof(SetKinematic));
        Reg("aegis.isGrounded",      nameof(IsGrounded));

        // ── v0.5 Sprites & Assets ──────────────────────────────────
        Reg("aegis.setFrame",        nameof(SetFrame));
        Reg("aegis.clearFrame",      nameof(ClearFrame));
        Reg("aegis.loadAtlas",       nameof(LoadAtlas));
        Reg("aegis.setAtlasFrame",   nameof(SetAtlasFrame));

        // ── v0.6 Animator ───────────────────────────────────────────
        Reg("aegis.newAnimator",     nameof(NewAnimator));
        Reg("aegis.addClip",         nameof(AddClip));
        Reg("aegis.play",            nameof(Play));
        Reg("aegis.stopAnimator",    nameof(StopAnimator));
        Reg("aegis.currentClip",     nameof(CurrentClip));
        Reg("aegis.newAtlasAnimator", nameof(NewAtlasAnimator));
        Reg("aegis.addAtlasClip",    nameof(AddAtlasClip));

        // ── v0.4 Raycast ────────────────────────────────────────────
        Reg("aegis.raycast",         nameof(DoRaycast));
        Reg("aegis.raycastMask",     nameof(DoRaycastMask));
        Reg("aegis.lineOfSight",     nameof(LineOfSight));

        // ── v0.7 Tilemaps, Procedural, SceneManager ──────────────────
        Reg("aegis.loadTilemap",     nameof(LoadTilemap));
        Reg("aegis.generateTilemap", nameof(GenerateTilemap));
        Reg("aegis.setTile",         nameof(SetTile));
        Reg("aegis.getTile",         nameof(GetTile));
        Reg("aegis.setTileCulling",  nameof(SetTileCulling));
        Reg("aegis.buildTilemapColliders", nameof(BuildTilemapColliders));
        Reg("aegis.clearTilemapColliders", nameof(ClearTilemapColliders));
        Reg("aegis.tilemapColliderCount",  nameof(TilemapColliderCount));
        Reg("aegis.newNavGrid",      nameof(NewNavGrid));
        Reg("aegis.navFromTilemap",  nameof(NavFromTilemap));
        Reg("aegis.navFindPath",     nameof(NavFindPath));
        Reg("aegis.navSetSolid",     nameof(NavSetSolid));
        Reg("aegis.navIsSolid",      nameof(NavIsSolid));
        Reg("aegis.perlin",          nameof(Perlin));
        Reg("aegis.registerScene",   nameof(RegisterScene));
        Reg("aegis.transitionTo",    nameof(TransitionTo));
        Reg("aegis.newAreaTrigger",  nameof(NewAreaTrigger));
        Reg("aegis.onTriggerEnter",  nameof(OnTriggerEnter));
        Reg("aegis.onTriggerStay",   nameof(OnTriggerStay));
        Reg("aegis.onTriggerExit",   nameof(OnTriggerExit));
        Reg("aegis.checkTrigger",    nameof(CheckTrigger));
        Reg("aegis.clearTriggers",   nameof(ClearTriggers));

        // ── v0.8 Save/Config/Effects/Debug ───────────────────────
        Reg("aegis.save",            nameof(Save));
        Reg("aegis.load",            nameof(Load));
        Reg("aegis.loadConfig",      nameof(LoadConfig));
        Reg("aegis.setFullscreen",   nameof(SetFullscreen));
        Reg("aegis.setResolution",   nameof(SetResolution));
        Reg("aegis.burst",           nameof(Burst));
        Reg("aegis.newEmitter",      nameof(NewEmitter));
        Reg("aegis.stopEmitter",     nameof(StopEmitter));
        Reg("aegis.tween",           nameof(Tween));
        Reg("aegis.newSequence",     nameof(NewSequence));
        Reg("aegis.seqAdd",          nameof(SeqAdd));
        Reg("aegis.seqWait",         nameof(SeqWait));
        Reg("aegis.seqPlay",         nameof(SeqPlay));
        Reg("aegis.seqStop",         nameof(SeqStop));
        Reg("aegis.fadeIn",          nameof(FadeIn));
        Reg("aegis.fadeOut",         nameof(FadeOut));
        Reg("aegis.flashScreen",     nameof(FlashScreen));
        Reg("aegis.setHotReload",    nameof(SetHotReload));
        Reg("aegis.setShader",       nameof(SetShader));
        Reg("aegis.clearShader",     nameof(ClearShader));
        Reg("aegis.setScreenShader", nameof(SetScreenShader));
        Reg("aegis.clearScreenShader", nameof(ClearScreenShader));

        // ── Sprint 2 — APIs de jogo urgentes ─────────────────────
        Reg("aegis.getTime",         nameof(GetTime));
        Reg("aegis.lookAt",          nameof(LookAt));
        Reg("aegis.overlapCircle",   nameof(OverlapCircle));
        Reg("aegis.overlapRect",     nameof(OverlapRect));
        Reg("aegis.newButton",       nameof(NewButton));
        Reg("aegis.onHover",         nameof(OnHover));
        Reg("aegis.onPress",         nameof(OnPress));
        Reg("aegis.floatText",       nameof(FloatText));
        Reg("aegis.newProgressBar",  nameof(NewProgressBar));
        Reg("aegis.setBarValue",     nameof(SetBarValue));
        Reg("aegis.setBarColors",    nameof(SetBarColors));

        // ── Sprint 3 — Física e gameplay ─────────────────────────
        Reg("aegis.isTouchingWall",  nameof(IsTouchingWall));
        Reg("aegis.wallSide",        nameof(WallSide));
        Reg("aegis.newPool",         nameof(NewPool));
        Reg("aegis.poolGet",         nameof(PoolGet));
        Reg("aegis.poolReturn",      nameof(PoolReturn));
        Reg("aegis.poolClear",       nameof(PoolClear));
        Reg("aegis.poolCount",       nameof(PoolCount));
    }

    private void Reg(string lua, string method)
        => _lua.RegisterFunction(lua, this, GetType().GetMethod(method)!);

    private static T Require<T>(T? value, string apiName) where T : class
        => value ?? throw new ArgumentNullException(apiName, $"[Aegis|Lua] Argumento obrigatório nulo em {apiName}.");

    private static float RequireFinite(float value, string argumentName, float fallback = 0f)
    {
        if (float.IsFinite(value)) return value;
        AegisLog.Warn("Lua", $"Valor inválido em {argumentName}; usando {fallback}.");
        return fallback;
    }

    // ── Core ──────────────────────────────────────────────────────────
    public SpriteNode NewSprite(string path)
        => new SpriteNode(ResManager.LoadTexture(path), _app.S2D);

    public Bitmap NewRect(int w, int h, float r, float g, float b)
    {
        var tex  = new Texture2D(Renderer.GraphicsDevice, w, h);
        var data = new Color[w * h];
        Array.Fill(data, new Color(r, g, b));
        tex.SetData(data);
        return new Bitmap(tex, _app.S2D);
    }

    public void RemoveObject(Object2D obj)
    {
        // Remove componentes físicos primeiro. Se o objeto sai da cena mas o
        // collider continua registrado, ele vira uma colisão invisível.
        RemoveRigidbody(obj);

        var ownedColliderIds = _colliders
            .Where(kv => kv.Value.Owner == obj)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var id in ownedColliderIds)
        {
            CollisionSystem.Instance.Unregister(_colliders[id]);
            _colliders.Remove(id);
        }

        obj.RemoveFromParent();
    }

    // ── Transform ─────────────────────────────────────────────────────
    public void  SetPosition(Object2D o, float x, float y)
    {
        Require(o, nameof(SetPosition));
        o.X = RequireFinite(x, nameof(x));
        o.Y = RequireFinite(y, nameof(y));
    }
    public void  SetPositionNorm(Object2D o, float nx, float ny)
    {
        Require(o, nameof(SetPositionNorm));
        o.X = RequireFinite(nx, nameof(nx)) * _app.ScreenWidth;
        o.Y = RequireFinite(ny, nameof(ny)) * _app.ScreenHeight;
    }
    public float CenterX(Object2D o)
    {
        Require(o, nameof(CenterX));
        return o.X + (o is Bitmap b ? b.TextureWidth * 0.5f : 0f);
    }
    public void  SetZ(Object2D o, int z)
    {
        Require(o, nameof(SetZ));
        o.Z = z;
    }
    public int   GetZ(Object2D o)
    {
        Require(o, nameof(GetZ));
        return o.Z;
    }
    public void  Move(Object2D o, float dx, float dy)      { Require(o, nameof(Move)); o.X += RequireFinite(dx, nameof(dx)); o.Y += RequireFinite(dy, nameof(dy)); }
    public void  SetScale(Object2D o, float sx, float sy)  { Require(o, nameof(SetScale)); o.ScaleX = RequireFinite(sx, nameof(sx), 1f); o.ScaleY = RequireFinite(sy, nameof(sy), 1f); }
    public void  SetRotation(Object2D o, float rad)        { Require(o, nameof(SetRotation)); o.Rotation = RequireFinite(rad, nameof(rad)); }
    public void  SetAlpha(Object2D o, float a)             => o.Alpha = Math.Clamp(a, 0f, 1f);
    public void  SetVisible(Object2D o, bool v)            => o.Visible = v;
    public void  SetPivot(Bitmap b, float px, float py)    => b.Pivot = new Vector2(px, py);
    public float GetX(Object2D o)    => o.X;
    public float GetY(Object2D o)    => o.Y;
    public int   GetWidth(Bitmap b)  => b.TextureWidth;
    public int   GetHeight(Bitmap b) => b.TextureHeight;

    // ── Label ─────────────────────────────────────────────────────────
    public Label NewLabel(string text)
        => new Label(FontManager.Default, _app.S2D) { Text = text };
    public void SetText(Label l, string t)                                        => l.Text = t;
    /// <summary>Define cor do Label. Alpha opcional (padrão 1.0 = opaco).
    /// BUG #6 fix: parâmetro alpha adicionado para suportar texto semitransparente.</summary>
    public void SetColor(Label l, float r, float g, float b, float a = 1f)        => l.Color = new Color(r, g, b, a);

    // ── AnimatedSprite ────────────────────────────────────────────────
    public AnimatedSprite NewAnim(string path, int fw, int fh)
        => new AnimatedSprite(ResManager.LoadTexture(path), fw, fh, _app.S2D);
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

    // ── Audio ─────────────────────────────────────────────────────────
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
    public void StopMusic()                                       => AudioManager.StopMusic();
    public void PauseMusic()                                      => AudioManager.PauseMusic();
    public void ResumeMusic()                                     => AudioManager.ResumeMusic();
    public void SetSfxVolume(float v)                             => AudioManager.SetSfxVolume(v);
    public void SetMusicVolume(float v)                           => AudioManager.SetMusicVolume(v);
    public bool MusicPlaying()                                    => AudioManager.IsMusicPlaying;
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

    // ── RichLabel ─────────────────────────────────────────────────────
    public RichLabel NewRichLabel(string markup)
    {
        var rl = new RichLabel(_app.S2D) { Markup = markup };
        if (FontManager.Default is not null) rl.Font = FontManager.Default;
        return rl;
    }
    public void SetMarkup(RichLabel rl, string m)             => rl.Markup = m;
    public void SetPivotRich(RichLabel rl, float px, float py) => rl.Pivot = new Vector2(px, py);

    // ── Font ──────────────────────────────────────────────────────────
    public SpriteFont LoadFont(string file, int size) => FontManager.Load(file, size);
    public void SetFont(Label l, SpriteFont font)     => l.Font = font;
    public void SetFontRich(RichLabel rl, SpriteFont font) => rl.Font = font;

    // ── NineSlice ─────────────────────────────────────────────────────
    public NineSlice NewPanel(string path, int border)
        => new NineSlice(ResManager.LoadTexture(path), border, _app.S2D);
    public void SetPanelSize(NineSlice p, int w, int h) { p.Width = w; p.Height = h; }

    public FlowContainer NewFlow(string direction, LuaTable? opts = null)
    {
        var flow = new FlowContainer(
            direction,
            TableFloat(opts, "gap", 0f),
            TableFloat(opts, "padding", 0f),
            TableString(opts, "align", "start"),
            _app.S2D);
        return flow;
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
    //  v0.7 — Tilemaps, Tiled JSON, Procedural, SceneManager
    // ════════════════════════════════════════════════════════════════

    public TilemapNode LoadTilemap(string tiledJsonPath)
        => TilemapNode.LoadTiledJson(tiledJsonPath, _app.S2D);

    public TilemapNode GenerateTilemap(string tilesetPath, int width, int height, int tileW, int tileH,
                                       int seed = 1337, float scale = 0.08f)
        => TilemapNode.GenerateProcedural(tilesetPath, width, height, tileW, tileH, seed, scale, parent: _app.S2D);

    public void SetTile(TilemapNode map, int layer, int x, int y, int gid) => map.SetTile(layer, x, y, gid);
    public int  GetTile(TilemapNode map, int layer, int x, int y) => map.GetTile(layer, x, y);
    public void SetTileCulling(TilemapNode map, bool enabled, int padding = 2)
    {
        map.CameraCulling = enabled;
        map.CullingPaddingTiles = Math.Clamp(padding, 0, 32);
    }


    public int BuildTilemapColliders(TilemapNode map, LuaTable opts)
    {
        var gids = ReadIntArray(opts["solidGids"] as LuaTable);
        var merge = TableBool(opts, "merge", true);
        var useTiledProperty = TableBool(opts, "useTiledProperty", false);
        var layer = opts["layer"] is null ? 1 : (opts["layer"] is string ls ? ParseLayer(ls) : Convert.ToInt32(opts["layer"]));
        var mask = opts["mask"] is null ? ~0 : (opts["mask"] is string ms ? ParseMaskString(ms) : Convert.ToInt32(opts["mask"]));
        return map.BuildColliders(gids, merge, layer, mask, useTiledProperty);
    }

    public void ClearTilemapColliders(TilemapNode map) => map.ClearColliders();
    public int TilemapColliderCount(TilemapNode map) => map.GeneratedColliderCount;

    public NavGrid NewNavGrid(int width, int height, int cellSize, bool diagonal = false)
        => new NavGrid(width, height, cellSize, diagonal);

    public NavGrid NavFromTilemap(TilemapNode map, LuaTable opts)
    {
        var gids = ReadIntArray(opts["solidGids"] as LuaTable);
        var diagonal = TableBool(opts, "diagonal", false);
        return NavGrid.FromTilemap(map, gids, diagonal);
    }

    public LuaTable? NavFindPath(NavGrid nav, float startX, float startY, float goalX, float goalY)
    {
        var path = nav.FindPath(new Vector2(startX, startY), new Vector2(goalX, goalY));
        if (path is null) return null;
        _lua.NewTable("_aegis_path");
        var t = (LuaTable)_lua["_aegis_path"];
        for (int i = 0; i < path.Count; i++)
        {
            _lua.NewTable("_aegis_path_point");
            var p = (LuaTable)_lua["_aegis_path_point"];
            p["x"] = path[i].X;
            p["y"] = path[i].Y;
            t[i + 1] = p;
        }
        return t;
    }

    public void NavSetSolid(NavGrid nav, int x, int y, bool solid) => nav.SetSolid(x, y, solid);
    public bool NavIsSolid(NavGrid nav, int x, int y) => nav.IsSolid(x, y);

    public float Perlin(float x, float y, int seed = 1337, int octaves = 4, float scale = 0.08f)
        => new PerlinNoise(seed).Fractal(x * scale, y * scale, octaves);

    public void RegisterScene(string name, string luaFile) => SceneManager.Instance.RegisterScene(name, luaFile);
    public void TransitionTo(string scene, string mode = "fade", float seconds = 0.35f)
        => SceneManager.Instance.TransitionTo(scene, mode, seconds);

    public AreaTrigger NewAreaTrigger(string name, float x, float y, float w, float h, bool oneShot = false)
    {
        var t = new AreaTrigger(name, x, y, w, h) { OneShot = oneShot };
        SceneManager.Instance.AddTrigger(t);
        return t;
    }

    public void OnTriggerEnter(AreaTrigger trigger, LuaFunction cb)
        => trigger.OnEnter = (t, o) => cb.Call(t, o);
    public void OnTriggerStay(AreaTrigger trigger, LuaFunction cb)
        => trigger.OnStay = (t, o) => cb.Call(t, o);
    public void OnTriggerExit(AreaTrigger trigger, LuaFunction cb)
        => trigger.OnExit = (t, o) => cb.Call(t, o);
    public void CheckTrigger(AreaTrigger trigger, Object2D obj) => trigger.Check(obj);
    public void ClearTriggers() => SceneManager.Instance.ClearTriggers();


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

    // ════════════════════════════════════════════════════════════════
    //  v0.4 — Collider API
    // ════════════════════════════════════════════════════════════════

    /// aegis.addCollider(obj, w, h [, offX, offY]) → collider
    public Collider AddCollider(Object2D obj, float w, float h,
                                float offX = 0f, float offY = 0f)
    {
        Require(obj, nameof(AddCollider));
        var safeW = MathF.Max(0.001f, RequireFinite(w, nameof(w), 1f));
        var safeH = MathF.Max(0.001f, RequireFinite(h, nameof(h), 1f));
        var c = new Collider(obj, safeW, safeH,
            RequireFinite(offX, nameof(offX)),
            RequireFinite(offY, nameof(offY)));
        CollisionSystem.Instance.Register(c);
        _colliders[++_colliderIdSeq] = c;
        return c;
    }

    /// aegis.addCircleCollider(obj, radius [, offX, offY]) -> collider
    public Collider AddCircleCollider(Object2D obj, float radius,
                                      float offX = 0f, float offY = 0f)
    {
        Require(obj, nameof(AddCircleCollider));
        var r = MathF.Max(0.001f, RequireFinite(radius, nameof(radius), 1f));
        var c = new Collider(obj, r * 2f, r * 2f,
            RequireFinite(offX, nameof(offX)),
            RequireFinite(offY, nameof(offY)))
        {
            Shape = ColliderShape.Circle,
            Radius = r
        };
        CollisionSystem.Instance.Register(c);
        _colliders[++_colliderIdSeq] = c;
        return c;
    }

    public void RemoveCollider(Collider c)
    {
        CollisionSystem.Instance.Unregister(c);
        var key = _colliders.FirstOrDefault(kv => kv.Value == c).Key;
        if (key != 0) _colliders.Remove(key);
    }

    /// Layers pré-definidas acessíveis por string no Lua
    private static int ParseLayer(string name) => name.ToUpper() switch
    {
        "WORLD"  => 1,
        "PLAYER" => 2,
        "ENEMY"  => 4,
        "BULLET" => 8,
        "PICKUP" => 16,
        "UI"     => 32,
        _        => 1
    };

    /// aegis.setColliderLayer(c, "PLAYER")  ou  aegis.setColliderLayer(c, 2)
    public void SetColliderLayer(Collider c, object layer)
    {
        var value = layer is string s ? ParseLayer(s) : Convert.ToInt32(layer);
        if (value == 0) AegisLog.Warn("Collision", "Collider layer ficou 0; ele pode não colidir com nada.");
        c.Layer = value;
    }

    /// aegis.setColliderMask(c, "WORLD|ENEMY")  ou numérico
    public void SetColliderMask(Collider c, object mask)
    {
        if (mask is string s)
        {
            int m = 0;
            foreach (var part in s.Split('|'))
                m |= ParseLayer(part.Trim());
            c.Mask = m;
        }
        else c.Mask = Convert.ToInt32(mask);
        if (c.Mask == 0) AegisLog.Warn("Collision", "Collider mask ficou 0; ele não detectará colisões.");
    }

    public void SetTrigger(Collider c, bool v)                      => c.IsTrigger = v;
    public void SetColliderOffset(Collider c, float ox, float oy)   { c.OffsetX = ox; c.OffsetY = oy; }

    /// Retorna o Collider anexado ao Object2D (busca por Owner)
    public Collider? GetColliderOf(Object2D obj)
    {
        foreach (var c in _colliders.Values)
            if (c.Owner == obj) return c;
        return null;
    }

    // ── Callbacks de colisão ──────────────────────────────────────────

    /// aegis.onCollide(collider, function(a, b) ... end)
    /// → dispara tanto em enter quanto em stay
    public void OnCollide(Collider c, LuaFunction cb)
    {
        c.OnCollideEnter = (a, b) => cb.Call(a, b);
        c.OnCollideStay  = (a, b) => cb.Call(a, b);
    }

    /// aegis.onCollideEnter(collider, function(a, b) ... end)
    public void OnCollideEnter(Collider c, LuaFunction cb)
        => c.OnCollideEnter = (a, b) => cb.Call(a, b);

    /// aegis.onCollideExit(collider, function(a, b) ... end)
    public void OnCollideExit(Collider c, LuaFunction cb)
        => c.OnCollideExit = (a, b) => cb.Call(a, b);

    // ════════════════════════════════════════════════════════════════
    //  v0.4 — Rigidbody2D API
    // ════════════════════════════════════════════════════════════════

    private readonly Dictionary<Object2D, Rigidbody2D> _rigidbodies = new();

    /// aegis.addRigidbody(obj) → rigidbody
    public Rigidbody2D AddRigidbody(Object2D obj)
    {
        if (_rigidbodies.TryGetValue(obj, out var existing)) return existing;

        // BUG #2: Avisar quando CircleCollider é combinado com Rigidbody.
        // A resolução de colisão sólida (ResolveBodyAxis) é AABB-only;
        // um corpo circular com Rigidbody atravessará paredes silenciosamente.
        var circleCol = _colliders.Values.FirstOrDefault(
            c => c.Owner == obj && c.Shape == ColliderShape.Circle);
        if (circleCol != null)
            AegisLog.Warn("Physics",
                "Rigidbody adicionado em objeto com CircleCollider. " +
                "Resolução de colisão sólida não suporta círculo — " +
                "use AABB para corpos físicos sólidos. " +
                "CircleCollider funciona apenas como trigger/detecção.");

        var rb = new Rigidbody2D(obj);
        PhysicsWorld.Instance.AddBody(rb);
        _rigidbodies[obj] = rb;
        return rb;
    }

    public void RemoveRigidbody(Object2D obj)
    {
        if (_rigidbodies.TryGetValue(obj, out var rb))
        {
            PhysicsWorld.Instance.RemoveBody(rb);
            _rigidbodies.Remove(obj);
        }
    }

    private const float MaxVel = 8000f;

    private static float ClampVel(float v)
    {
        if (!float.IsFinite(v)) return 0f;
        if (v >  MaxVel) return  MaxVel;
        if (v < -MaxVel) return -MaxVel;
        return v;
    }

    /// aegis.setVelocity(rb, vx, vy)
    public void SetVelocity(Rigidbody2D rb, float vx, float vy)
    {
        rb.VelocityX = ClampVel(vx);
        rb.VelocityY = ClampVel(vy);
    }

    /// aegis.setVelocityX(rb, vx) — só horizontal, não toca VelocityY (gravidade fica intacta)
    public void SetVelocityX(Rigidbody2D rb, float vx)
        => rb.VelocityX = ClampVel(vx);

    /// aegis.addImpulseY(rb, vy) — impulso vertical (pulo). Substitui setVelocity para pulos.
    public void AddImpulseY(Rigidbody2D rb, float vy)
        => rb.ApplyImpulseY(ClampVel(vy));

    /// Compat roadmap: setVel(rb, vx, vy) sem matar Y em voo quando vy=0.
    public void SetVel(Rigidbody2D rb, float vx, float vy)
    {
        rb.VelocityX = ClampVel(vx);
        if (MathF.Abs(vy) < 1e-5f && !rb.IsGrounded) return;
        rb.VelocityY = ClampVel(vy);
    }

    /// Alias retrocompat.
    public void SetVelX(Rigidbody2D rb, float vx) => SetVelocityX(rb, vx);

    /// Alias retrocompat (pulo por impulso).
    public void JumpY(Rigidbody2D rb, float vy) => AddImpulseY(rb, vy);

    /// aegis.addVelocity(rb, dvx, dvy)
    public void AddVelocity(Rigidbody2D rb, float dvx, float dvy)
    {
        rb.VelocityX = ClampVel(rb.VelocityX + dvx);
        rb.VelocityY = ClampVel(rb.VelocityY + dvy);
    }

    public float GetVelocityX(Rigidbody2D rb) => rb.VelocityX;
    public float GetVelocityY(Rigidbody2D rb) => rb.VelocityY;

    /// aegis.setGravity(rb, scale)  — multiplicador por instância
    public void SetGravity(Rigidbody2D rb, float scale) => rb.GravityScale = scale;

    /// aegis.setGroundFriction(rb, k)  — 0=off; ~12–20 = paragem suave no chão
    public void SetGroundFriction(Rigidbody2D rb, float k)
    {
        if (!float.IsFinite(k) || k < 0f) k = 0f;
        rb.GroundFriction = MathF.Min(k, 200f);
    }

    /// aegis.setGlobalGravity(900)  — altera gravidade global (padrão 800)
    public void SetGlobalGravity(float g)
    {
        if (!float.IsFinite(g) || g < 0f) g = 800f;
        g = MathF.Min(g, 20_000f);
        Rigidbody2D.Gravity = g;
    }

    public void SetKinematic(Rigidbody2D rb, bool v) => rb.IsKinematic = v;

    /// aegis.isGrounded(rb) → bool
    public bool IsGrounded(Rigidbody2D rb) => rb.IsGrounded;

    // ════════════════════════════════════════════════════════════════
    //  v0.4 — Raycast API
    // ════════════════════════════════════════════════════════════════

    /// aegis.raycast(ox, oy, dx, dy, length) → {hit, x, y, nx, ny, dist} ou nil
    public LuaTable? DoRaycast(float ox, float oy, float dx, float dy, float len)
        => BuildRayResult(CollisionSystem.Instance.Raycast(
               new Vector2(ox, oy), new Vector2(dx, dy), len));

    /// aegis.raycastMask(ox, oy, dx, dy, length, mask)
    public LuaTable? DoRaycastMask(float ox, float oy, float dx, float dy,
                                    float len, object mask)
    {
        int m = mask is string s ? ParseMaskString(s) : Convert.ToInt32(mask);
        return BuildRayResult(CollisionSystem.Instance.Raycast(
               new Vector2(ox, oy), new Vector2(dx, dy), len, m));
    }

    /// aegis.lineOfSight(ax, ay, bx, by [, mask]) → bool
    /// Retorna true se NÃO houver obstáculo entre A e B.
    public bool LineOfSight(float ax, float ay, float bx, float by,
                             object? mask = null)
    {
        int m = mask is string s ? ParseMaskString(s)
              : mask is not null  ? Convert.ToInt32(mask)
              : ~0;
        var dir = new Vector2(bx - ax, by - ay);
        float len = dir.Length();
        if (len < 0.001f) return true;
        var hit = CollisionSystem.Instance.Raycast(
            new Vector2(ax, ay), dir / len, len, m);
        return hit is null;
    }

    // ── Helpers Raycast ───────────────────────────────────────────────
    private static int ParseMaskString(string s)
    {
        int m = 0;
        foreach (var part in s.Split('|'))
            m |= part.Trim().ToUpper() switch
            {
                "WORLD"  => 1, "PLAYER" => 2, "ENEMY"  => 4,
                "BULLET" => 8, "PICKUP" => 16, "UI"    => 32,
                _        => 1
            };
        return m;
    }

    private LuaTable? BuildRayResult(RaycastHit? hit)
    {
        if (hit is null) return null;
        var h = hit.Value;
        _lua.NewTable("_ray_result");
        var t = (LuaTable)_lua["_ray_result"];
        t["hit"]      = true;
        t["collider"] = h.Collider;
        t["x"]        = h.Point.X;
        t["y"]        = h.Point.Y;
        t["nx"]       = h.Normal.X;
        t["ny"]       = h.Normal.Y;
        t["dist"]     = h.Distance;
        return t;
    }

    // ── Input ─────────────────────────────────────────────────────────
    public bool KeyDown(string k)    => InputManager.IsDown(k);
    public bool KeyPressed(string k) => InputManager.JustPressed(k);
    public int  GetMouseX()          => InputManager.MouseX;
    public int  GetMouseY()          => InputManager.MouseY;
    public bool MouseLeft()          => InputManager.LeftDown;
    public bool MouseLeftJust()      => InputManager.LeftJust;
    public bool MouseRight()         => InputManager.RightDown;
    public bool MouseRightJust()     => InputManager.RightJust;
    public int  GetScrollDelta()     => InputManager.ScrollDelta;
    public bool PadConnected(int index) => InputManager.PadConnected(index);
    public bool PadDown(int index, string button) => InputManager.PadDown(index, button);
    public bool PadPressed(int index, string button) => InputManager.PadPressed(index, button);
    public float PadAxis(int index, string axis) => InputManager.PadAxis(index, axis);
    public void PadVibrate(int index, float left, float right, float seconds = 0f)
        => InputManager.PadVibrate(index, left, right, seconds);

    // ── Screen ────────────────────────────────────────────────────────
    public int GetScreenWidth()  => _app.ScreenWidth;
    public int GetScreenHeight() => _app.ScreenHeight;

    // ── Utils ─────────────────────────────────────────────────────────
    public void  Log(string msg)                   => AegisLog.Info("Lua", msg);
    public int   RandomInt(int min, int max)        => _rng.Next(min, max + 1);
    public float RandomFloat(float min, float max)  =>
        min + (float)_rng.NextDouble() * (max - min);

    /// Equivalente a clearAll/World.Clear do roadmap antigo.
    public void ClearAll()
    {
        // BUG #4 fix: matar todos os tweens antes de destruir objetos,
        // evitando tweens zumbis que atualizam Object2D já removidos da cena.
        TweenManager.Instance.KillAll();

        PhysicsWorld.Instance.Reset();
        _rigidbodies.Clear();
        _colliders.Clear();

        foreach (var child in _app.S2D.Children.ToArray())
            child.RemoveFromParent();

        Camera2D.Instance.ResetForNewSession();
        SceneManager.Instance.ClearTriggers();

        // BUG #4 fix: limpar partículas em voo antes de soltar referência,
        // evitando que o ParticleSystem2D antigo continue rodando.
        _particles?.ClearAll();
        _particles = null;

        // Sprint 2+3: limpar buttons, pools, float texts e resetar timer
        _buttons.Clear();
        foreach (var ft in _floatTexts) ft.Lbl.RemoveFromParent();
        _floatTexts.Clear();
        foreach (var pool in _pools.Values) pool.Clear();
        _pools.Clear();
        _poolIdSeq   = 0;
        _progressBars.Clear();
        _totalTime   = 0f;
    }

    /// Alias explícito para scripts antigos.
    public void WorldClear() => ClearAll();

    /// Primitivas imediatas de debug no frame atual.
    public void DrawText(string text, float x, float y, float r = 1f, float g = 1f, float b = 1f)
    {
        if (FontManager.Default is null) return;
        Renderer.SpriteBatch.DrawString(FontManager.Default, text, new Vector2(x, y), new Color(r, g, b));
    }

    public void DrawRect(float x, float y, float w, float h, float r = 1f, float g = 1f, float b = 1f)
    {
        if (w <= 0f || h <= 0f) return;
        Renderer.SpriteBatch.Draw(ResManager.Pixel, new Rectangle((int)x, (int)y, (int)w, (int)h), new Color(r, g, b));
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

    public void Save(string key, object? value) => SaveManager.Save(key, value);
    public object? Load(string key) => SaveManager.Load(key);
    public object? LoadConfig(string key) => ConfigManager.Load(key);
    public void SetFullscreen(bool value) => ConfigManager.SetFullscreen(value);
    public void SetResolution(int width, int height) => ConfigManager.SetResolution(width, height);

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

    public void SetHotReload(bool enabled) => HotReloadManager.Instance.Enabled = enabled;

    public void ReloadMainScript(string path)
    {
        ClearAll();
        _lua["aegis_init"]    = null;
        _lua["aegis_update"]  = null;
        _lua["aegis_draw"]    = null;
        _lua["aegis_draw_ui"] = null;
        if (!HasFunction("aegis_init"))
            throw new InvalidOperationException("[Aegis|Lua] Função obrigatória aegis_init não encontrada após reload.");
        CallFunction("aegis_init");
        InputManager.HardSyncFromHardware();
    }

    public void ExecuteFile(string path)
    {
        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
            throw new FileNotFoundException($"[Aegis] Script não encontrado: '{full}'");
        _gameRoot = Directory.GetCurrentDirectory();
        _mainLuaFullPath = full;
        _lua.DoFile(full);
    }

    /// <summary>HOT_RELOAD vindo do Aegis Editor: recarrega module ou main.lua dentro da pasta do jogo.</summary>
    public void EditorHotReloadFile(string relativeOrGameRelativePath)
    {
        var root = Path.GetFullPath(_gameRoot);
        var safe = relativeOrGameRelativePath.Replace('\\', '/').TrimStart('/');
        var full = Path.GetFullPath(Path.Combine(root, safe));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"[Aegis|Scene] Script fora da pasta do jogo: '{relativeOrGameRelativePath}'");

        if (!File.Exists(full))
            throw new FileNotFoundException($"[Aegis|EditorIPC] Script não encontrado: '{full}'");

        if (string.Equals(full, _mainLuaFullPath, StringComparison.OrdinalIgnoreCase))
            ReloadMainScript(full);
        else
            _lua.DoFile(full);
    }

    public void LoadSceneFile(string path)
    {
        var root = Path.GetFullPath(_gameRoot);
        var safePath = path.Replace('\\', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(root, safePath));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"[Aegis|Scene] Cena fora da pasta do jogo: '{path}'");
        if (!File.Exists(full))
            throw new FileNotFoundException($"[Aegis|Scene] Cena não encontrada: '{full}'");

        ClearAll();
        _lua["aegis_init"]    = null;
        _lua["aegis_update"]  = null;
        _lua["aegis_draw"]    = null;
        _lua["aegis_draw_ui"] = null;
        if (!HasFunction("aegis_init"))
            throw new InvalidOperationException($"[Aegis|Lua] Função obrigatória aegis_init não encontrada na cena: {path}");
        CallFunction("aegis_init");
    }

    public void ExecuteString(string code)            => _lua.DoString(code);

    public bool HasFunction(string name) => _lua[name] is LuaFunction;

    public void CallFunction(string name, params object[] args)
    {
        if (_lua[name] is LuaFunction fn) fn.Call(args);
    }


    private static float TableFloat(LuaTable? table, string key, float fallback)
    {
        try { return table?[key] is null ? fallback : Convert.ToSingle(table[key]); }
        catch { return fallback; }
    }

    private static bool TableBool(LuaTable? table, string key, bool fallback)
    {
        try { return table?[key] is null ? fallback : Convert.ToBoolean(table[key]); }
        catch { return fallback; }
    }


    private static string TableString(LuaTable? table, string key, string fallback)
    {
        try { return table?[key]?.ToString() ?? fallback; }
        catch { return fallback; }
    }

    private static int[] ReadIntArray(LuaTable? table)
    {
        if (table is null) return Array.Empty<int>();
        var list = new List<int>();
        foreach (var value in table.Values)
        {
            try { list.Add(Convert.ToInt32(value)); }
            catch { }
        }
        return list.ToArray();
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

    private float _totalTime = 0f;

    /// <summary>Atualiza o timer acumulado — chamado pelo AegisGame a cada frame.</summary>
    internal void TickTime(float dt) => _totalTime += dt;

    /// <summary>aegis.getTime() → tempo total acumulado desde o início da sessão (segundos).
    /// Útil para timers de sobrevivência, cooldowns, animações sinusoidais.</summary>
    public float GetTime() => _totalTime;

    /// <summary>aegis.lookAt(obj, tx, ty) — rotaciona o objeto em direção ao ponto (tx, ty).
    /// Retorna o ângulo em radianos para uso posterior.</summary>
    public float LookAt(Object2D obj, float tx, float ty)
    {
        Require(obj, nameof(LookAt));
        var angle = MathF.Atan2(ty - obj.Y, tx - obj.X);
        obj.Rotation = angle;
        return angle;
    }

    /// <summary>aegis.overlapCircle(cx, cy, radius, mask) → array de Colliders dentro do círculo.
    /// Útil para explosões em AoE, detecção de inimigos próximos.</summary>
    public LuaTable OverlapCircle(float cx, float cy, float radius, object? mask = null)
    {
        int m = mask is string s ? ParseMaskString(s)
              : mask is not null ? Convert.ToInt32(mask)
              : ~0;

        _lua.NewTable("_aegis_overlap");
        var result = (LuaTable)_lua["_aegis_overlap"];
        int idx = 1;
        float r2 = radius * radius;

        foreach (var c in CollisionSystem.Instance.Colliders)
        {
            if (!c.IsActive) continue;
            if ((c.Layer & m) == 0) continue;
            var bounds = c.Bounds;
            float nearX = Math.Clamp(cx, bounds.Left, bounds.Right);
            float nearY = Math.Clamp(cy, bounds.Top, bounds.Bottom);
            float dx = cx - nearX;
            float dy = cy - nearY;
            if (dx * dx + dy * dy <= r2)
                result[idx++] = c;
        }
        return result;
    }

    /// <summary>aegis.overlapRect(x, y, w, h, mask) → array de Colliders dentro do retângulo.
    /// Útil para detecção em área, câmera de frustum, etc.</summary>
    public LuaTable OverlapRect(float x, float y, float w, float h, object? mask = null)
    {
        int m = mask is string s ? ParseMaskString(s)
              : mask is not null ? Convert.ToInt32(mask)
              : ~0;

        _lua.NewTable("_aegis_overlaprect");
        var result = (LuaTable)_lua["_aegis_overlaprect"];
        int idx = 1;
        var query = new RectangleF(x, y, w, h);

        foreach (var c in CollisionSystem.Instance.Colliders)
        {
            if (!c.IsActive) continue;
            if ((c.Layer & m) == 0) continue;
            if (c.Bounds.Intersects(query))
                result[idx++] = c;
        }
        return result;
    }

    // ── Button ───────────────────────────────────────────────────────

    private sealed class Button
    {
        public Object2D Obj;
        public LuaFunction? OnClick;
        public LuaFunction? OnHover;
        public LuaFunction? OnPress;
        public bool WasHovered;
        public bool WasPressed;

        public Button(Object2D obj) => Obj = obj;

        public bool HitTest(int mx, int my)
        {
            if (Obj is Bitmap b)
            {
                float w = b.TextureWidth * b.ScaleX;
                float h = b.TextureHeight * b.ScaleY;
                return mx >= b.X && mx <= b.X + w && my >= b.Y && my <= b.Y + h;
            }
            return mx >= Obj.X && mx <= Obj.X + 64 && my >= Obj.Y && my <= Obj.Y + 64;
        }
    }

    private readonly List<Button> _buttons = new();

    /// <summary>aegis.newButton(obj, onClick) → registra um objeto como botão interativo.
    /// Detecta hover, press e click automaticamente a cada frame.</summary>
    public Object2D NewButton(Object2D obj, LuaFunction? onClick = null)
    {
        Require(obj, nameof(NewButton));
        var btn = new Button(obj) { OnClick = onClick };
        _buttons.Add(btn);
        return obj;
    }

    /// <summary>aegis.onHover(obj, callback) — define callback de hover para um botão.</summary>
    public void OnHover(Object2D obj, LuaFunction cb)
    {
        var btn = _buttons.FirstOrDefault(b => b.Obj == obj);
        if (btn != null) btn.OnHover = cb;
    }

    /// <summary>aegis.onPress(obj, callback) — define callback de press para um botão.</summary>
    public void OnPress(Object2D obj, LuaFunction cb)
    {
        var btn = _buttons.FirstOrDefault(b => b.Obj == obj);
        if (btn != null) btn.OnPress = cb;
    }

    internal void UpdateButtons()
    {
        int mx = InputManager.MouseX;
        int my = InputManager.MouseY;
        bool leftJust = InputManager.LeftJust;
        bool leftDown = InputManager.LeftDown;

        foreach (var btn in _buttons)
        {
            bool hovered = btn.HitTest(mx, my);
            bool pressed = hovered && leftDown;

            if (hovered && !btn.WasHovered) btn.OnHover?.Call(btn.Obj);
            if (pressed && !btn.WasPressed) btn.OnPress?.Call(btn.Obj);
            if (hovered && leftJust)        btn.OnClick?.Call(btn.Obj);

            btn.WasHovered = hovered;
            btn.WasPressed = pressed;
        }
    }

    // ── FloatText — tracked list, sem dependência de tween p/ remoção ─────

    private sealed class FloatTextEntry
    {
        public Label  Lbl;
        public float  TimeLeft;
        public float  Duration;
        public float  Speed;      // px/s para cima
        public float  OriginY;

        public FloatTextEntry(Label lbl, float duration, float speed, float y)
        { Lbl = lbl; TimeLeft = duration; Duration = duration; Speed = speed; OriginY = y; }
    }

    private readonly List<FloatTextEntry> _floatTexts = new();

    /// <summary>aegis.floatText(x, y, text, opts) — exibe texto flutuante que sobe e desaparece.
    /// opts: {r, g, b, speed, duration}
    /// Útil para dano, XP, eventos de jogo.</summary>
    public void FloatText(float x, float y, string text, LuaTable? opts = null)
    {
        if (FontManager.Default is null) return;
        float speed    = TableFloat(opts, "speed",    45f);
        float duration = TableFloat(opts, "duration", 0.9f);
        float r        = TableFloat(opts, "r",        1f);
        float g        = TableFloat(opts, "g",        1f);
        float b        = TableFloat(opts, "b",        1f);

        var lbl = new Label(FontManager.Default, _app.S2D)
        {
            Text  = text,
            X     = x,
            Y     = y,
            Color = new Color(r, g, b),
            Z     = 900,
        };

        _floatTexts.Add(new FloatTextEntry(lbl, duration, speed, y));
    }

    /// <summary>Chamado pelo AegisGame a cada frame — move, faz fade e remove labels expirados.</summary>
    internal void UpdateFloatTexts(float dt)
    {
        for (int i = _floatTexts.Count - 1; i >= 0; i--)
        {
            var ft = _floatTexts[i];
            ft.TimeLeft -= dt;

            // Progresso 0→1 conforme o tempo passa
            float t = 1f - Math.Clamp(ft.TimeLeft / ft.Duration, 0f, 1f);
            ft.Lbl.Y     = ft.OriginY - ft.Speed * ft.Duration * t;
            ft.Lbl.Alpha = 1f - t;   // fade out linear

            if (ft.TimeLeft <= 0f)
            {
                ft.Lbl.RemoveFromParent();
                _floatTexts.RemoveAt(i);
            }
        }
    }

    // ── ProgressBar ───────────────────────────────────────────────────

    private sealed class ProgressBar
    {
        public Bitmap Bg;
        public Bitmap Fill;
        public float Current = 1f;
        public float Max = 1f;
        public int MaxWidth;
        public int MaxHeight;

        public ProgressBar(Bitmap bg, Bitmap fill, int maxWidth, int maxHeight)
        { Bg = bg; Fill = fill; MaxWidth = maxWidth; MaxHeight = maxHeight; }

        public void UpdateFill()
        {
            float ratio = Max > 0f ? Math.Clamp(Current / Max, 0f, 1f) : 0f;
            // ScaleX com Pivot=(0,0) cresce da esquerda para a direita sem deslocar
            Fill.ScaleX = Math.Max(0.001f, ratio);
        }
    }

    private readonly List<ProgressBar> _progressBars = new();

    /// <summary>aegis.newProgressBar(x, y, w, h) → objeto ProgressBar.
    /// Retorna o objeto de fundo (bg). Use setBarValue/setBarColors para controlar.</summary>
    public Object2D NewProgressBar(float x, float y, int w, int h)
    {
        var bgTex  = new Texture2D(Renderer.GraphicsDevice, w, h);
        var fgTex  = new Texture2D(Renderer.GraphicsDevice, w, h);
        bgTex.SetData(Enumerable.Repeat(new Color(0.15f, 0.15f, 0.15f), w * h).ToArray());
        fgTex.SetData(Enumerable.Repeat(new Color(0.2f, 1.0f, 0.3f), w * h).ToArray());

        var bg   = new Bitmap(bgTex,  _app.S2D) { X = x, Y = y };
        // Fill usa Pivot=(0,0) e ScaleX para crescer da esquerda p/ direita
        var fill = new Bitmap(fgTex,  _app.S2D) { X = x, Y = y };
        fill.Pivot = Vector2.Zero;   // ancora no canto esquerdo antes de escalar

        var bar = new ProgressBar(bg, fill, w, h);
        _progressBars.Add(bar);
        return bg;
    }

    /// <summary>aegis.setBarValue(barObj, current, max) — atualiza o preenchimento da barra.</summary>
    public void SetBarValue(Object2D barObj, float current, float max)
    {
        var bar = _progressBars.FirstOrDefault(b => b.Bg == barObj);
        if (bar is null) return;
        bar.Current = current;
        bar.Max = max;
        bar.UpdateFill();
    }

    /// <summary>aegis.setBarColors(barObj, opts) — define cores da barra.
    /// opts: {bg={r,g,b}, fill={r,g,b}}</summary>
    public void SetBarColors(Object2D barObj, LuaTable opts)
    {
        var bar = _progressBars.FirstOrDefault(b => b.Bg == barObj);
        if (bar is null) return;

        if (opts["bg"] is LuaTable bgColor)
        {
            float r = TableFloat(bgColor, "1", TableFloat(bgColor, "r", 0.15f));
            float g = TableFloat(bgColor, "2", TableFloat(bgColor, "g", 0.15f));
            float b = TableFloat(bgColor, "3", TableFloat(bgColor, "b", 0.15f));
            var pixels = Enumerable.Repeat(new Color(r, g, b), bar.MaxWidth * bar.MaxHeight).ToArray();
            bar.Bg.Texture.SetData(pixels);
        }
        if (opts["fill"] is LuaTable fillColor)
        {
            float r = TableFloat(fillColor, "1", TableFloat(fillColor, "r", 0.2f));
            float g = TableFloat(fillColor, "2", TableFloat(fillColor, "g", 1.0f));
            float b = TableFloat(fillColor, "3", TableFloat(fillColor, "b", 0.3f));
            var pixels = Enumerable.Repeat(new Color(r, g, b), bar.MaxWidth * bar.MaxHeight).ToArray();
            bar.Fill.Texture.SetData(pixels);
        }
    }
    // ════════════════════════════════════════════════════════════════
    //  Sprint 3 — Física e gameplay
    // ════════════════════════════════════════════════════════════════

    /// <summary>aegis.isTouchingWall(rb) → bool.
    /// Retorna true se o rigidbody estiver tocando uma parede lateral neste frame.</summary>
    public bool IsTouchingWall(Rigidbody2D rb) => rb.TouchingWall;

    /// <summary>aegis.wallSide(rb) → -1 (parede à esquerda), +1 (parede à direita), 0 (nenhuma).
    /// Útil para wall jump e wall slide em plataformers.</summary>
    public int WallSide(Rigidbody2D rb) => rb.WallSide;

    // ── Object Pool ───────────────────────────────────────────────────

    private sealed class ObjectPool
    {
        public readonly string SpritePath;
        public readonly Queue<SpriteNode> Available = new();
        public readonly List<SpriteNode> InUse = new();
        public readonly Scene2D Scene;

        public ObjectPool(string path, int initialSize, Scene2D scene)
        {
            SpritePath = path;
            Scene = scene;
            for (int i = 0; i < initialSize; i++)
            {
                var sprite = new SpriteNode(ResManager.LoadTexture(path), scene);
                sprite.Visible = false;
                Available.Enqueue(sprite);
            }
        }

        public SpriteNode Get(float x, float y)
        {
            SpriteNode sprite;
            if (Available.Count > 0)
            {
                sprite = Available.Dequeue();
            }
            else
            {
                sprite = new SpriteNode(ResManager.LoadTexture(SpritePath), Scene);
            }
            sprite.X = x;
            sprite.Y = y;
            sprite.Visible = true;
            sprite.Alpha = 1f;
            InUse.Add(sprite);
            return sprite;
        }

        public void Return(SpriteNode sprite)
        {
            if (!InUse.Remove(sprite)) return;
            // Resetar estado visual para evitar sprites "sujos" no próximo Get()
            sprite.Visible  = false;
            sprite.ScaleX   = 1f;
            sprite.ScaleY   = 1f;
            sprite.Rotation = 0f;
            sprite.Alpha    = 1f;
            Available.Enqueue(sprite);
        }

        public int AvailableCount => Available.Count;
        public int TotalCount     => Available.Count + InUse.Count;

        public void Clear()
        {
            foreach (var s in InUse)
            {
                s.Visible  = false;
                s.ScaleX   = 1f;
                s.ScaleY   = 1f;
                s.Rotation = 0f;
                s.Alpha    = 1f;
                Available.Enqueue(s);
            }
            InUse.Clear();
        }
    }

    private readonly Dictionary<int, ObjectPool> _pools = new();
    private int _poolIdSeq = 0;

    /// <summary>aegis.newPool(spritePath, initialSize) → pool.
    /// Cria um object pool pré-alocado para evitar GC pressure em jogos com muitos objetos.</summary>
    public int NewPool(string spritePath, int initialSize = 32)
    {
        var pool = new ObjectPool(spritePath, initialSize, _app.S2D);
        _pools[++_poolIdSeq] = pool;
        return _poolIdSeq;
    }

    /// <summary>aegis.poolGet(pool, x, y) → sprite retirado do pool.</summary>
    public SpriteNode PoolGet(int poolId, float x = 0f, float y = 0f)
    {
        if (!_pools.TryGetValue(poolId, out var pool))
            throw new ArgumentException($"[Aegis|Pool] Pool ID {poolId} não encontrado.");
        return pool.Get(x, y);
    }

    /// <summary>aegis.poolReturn(pool, sprite) — devolve um sprite ao pool.</summary>
    public void PoolReturn(int poolId, SpriteNode sprite)
    {
        if (!_pools.TryGetValue(poolId, out var pool)) return;
        pool.Return(sprite);
    }

    /// <summary>aegis.poolClear(pool) — devolve todos os sprites em uso de volta ao pool.</summary>
    public void PoolClear(int poolId)
    {
        if (!_pools.TryGetValue(poolId, out var pool)) return;
        pool.Clear();
    }

    /// <summary>aegis.poolCount(pool) → {available, total} — tamanho atual do pool.</summary>
    public LuaTable PoolCount(int poolId)
    {
        _lua.NewTable("_aegis_poolcount");
        var t = (LuaTable)_lua["_aegis_poolcount"];
        if (_pools.TryGetValue(poolId, out var pool))
        {
            t["available"] = pool.AvailableCount;
            t["total"]     = pool.TotalCount;
        }
        else
        {
            t["available"] = 0;
            t["total"]     = 0;
        }
        return t;
    }

    public void Dispose() => _lua.Dispose();
}
