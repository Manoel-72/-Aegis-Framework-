using NLua;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Aegis;

/// <summary>
/// Ponte Lua ↔ C#  (v0.5 — adiciona Sprites + ResManager)
///
/// Novas funções de sprite:
///   newSprite(path)              → SpriteNode (PNG/JPG)
///   newSprite(path, true)        → SpriteNode na camada HUD
///   setFrame(node, x, y, w, h)  → recorte de spritesheet (pixels)
///   clearFrame(node)             → volta para textura inteira
///   setFlip(node, flipH, flipV)  → espelha o sprite
///   getWidth(node)               → largura do frame atual (ou rect.W)
///   getHeight(node)              → altura do frame atual (ou rect.H)
///   setFilter(path, "pixel"|"linear") → filtro da textura no cache
/// </summary>
public sealed class LuaBridge
{
    private readonly Lua           _lua = new();
    private readonly Engine        _eng;
    private readonly Physics.World _phys;
    private readonly Input         _inp;
    private readonly Camera        _cam;

    public LuaBridge(Engine eng, Physics.World phys, Input inp, Camera cam)
    {
        _eng  = eng;
        _phys = phys;
        _inp  = inp;
        _cam  = cam;
        _lua.State.Encoding = System.Text.Encoding.UTF8;
        RegisterAll();
    }

    // ── Ciclo de vida ─────────────────────────────────────────────────────
    public void Load(string dir)
    {
        ResManager.SetBaseDir(dir);           // ← v0.5: define base de texturas
        string entry = Path.Combine(dir, "main.lua");
        if (File.Exists(entry)) _lua.DoFile(entry);
    }

    public void CallInit()           => Call("init");
    public void CallUpdate(float dt) => Call("update", dt);
    public void CallDraw()           => Call("draw");

    private void Call(string fn, params object[] args)
    {
        try   { if (_lua[fn] is LuaFunction f) f.Call(args); }
        catch (Exception ex) { Console.WriteLine($"[Lua:{fn}] {ex.Message}"); }
    }

    private void Reg(string name, string method) =>
        _lua.RegisterFunction(name, this, GetType().GetMethod(method)!);

    private void RegisterAll()
    {
        // Tela / tempo
        Reg("screenW",      nameof(ScreenW));
        Reg("screenH",      nameof(ScreenH));
        Reg("getTime",      nameof(GetTime));

        // Nodes ── retângulos e labels
        Reg("newRect",      nameof(NewRect));
        Reg("newLabel",     nameof(NewLabel));
        Reg("remove",       nameof(Remove));
        Reg("clearAll",     nameof(ClearAll));
        Reg("setPos",       nameof(SetPos));
        Reg("move",         nameof(Move));
        Reg("setPivot",     nameof(SetPivot));
        Reg("setScale",     nameof(SetScale));
        Reg("setColor",     nameof(SetColor));
        Reg("setAlpha",     nameof(SetAlpha));
        Reg("setVisible",   nameof(SetVisible));
        Reg("getX",         nameof(GetX));
        Reg("getY",         nameof(GetY));
        Reg("getWidth",     nameof(GetWidth));   // funciona para Rect e Sprite
        Reg("getHeight",    nameof(GetHeight));  // funciona para Rect e Sprite
        Reg("setLabel",     nameof(SetLabel));
        Reg("setFontSize",  nameof(SetFontSize));

        // Sprites ── v0.5
        Reg("newSprite",    nameof(NewSprite));
        Reg("setFrame",     nameof(SetFrame));
        Reg("clearFrame",   nameof(ClearFrame));
        Reg("setFlip",      nameof(SetFlip));
        Reg("setFilter",    nameof(SetFilter));

        // Física
        Reg("addBody",      nameof(AddBody));
        Reg("addShape",     nameof(AddShape));
        Reg("removeBody",   nameof(RemoveBody));
        Reg("removeShape",  nameof(RemoveShape));
        Reg("setVelX",      nameof(SetVelX));
        Reg("setVelY",      nameof(SetVelY));
        Reg("jumpY",        nameof(JumpY));
        Reg("setVel",       nameof(SetVel));
        Reg("getVelX",      nameof(GetVelX));
        Reg("getVelY",      nameof(GetVelY));
        Reg("isGrounded",   nameof(IsGrounded));
        Reg("setGravScale", nameof(SetGravScale));
        Reg("setGravity",   nameof(SetGravity));
        Reg("setLayer",     nameof(SetLayer));
        Reg("setMask",      nameof(SetMask));
        Reg("setTrigger",   nameof(SetTrigger));
        Reg("onEnter",      nameof(OnEnter));
        Reg("onStay",       nameof(OnStay));
        Reg("onExit",       nameof(OnExit));

        // Input
        Reg("keyDown",      nameof(KeyDown));
        Reg("keyPressed",   nameof(KeyPressed));
        Reg("mouseX",       nameof(MouseX));
        Reg("mouseY",       nameof(MouseY));
        Reg("mousePressed", nameof(MousePressed));

        // Câmera
        Reg("cameraFollow", nameof(CameraFollow));
        Reg("cameraLimits", nameof(CameraLimits));
        Reg("cameraZoom",   nameof(CameraZoom));
        Reg("cameraOff",    nameof(CameraOff));
        Reg("cameraOffset", nameof(CameraOffset));

        // Misc
        Reg("shake",        nameof(Shake));
        Reg("log",          nameof(Log));
        Reg("randomInt",    nameof(RandomInt));
        Reg("randomFloat",  nameof(RandomFloat));

        // Draw helpers
        Reg("drawText",     nameof(DrawText_));
        Reg("drawRect",     nameof(DrawRect_));
        Reg("drawLine",     nameof(DrawLine_));
        Reg("drawCircle",   nameof(DrawCircle_));
    }

    // ── Tela ──────────────────────────────────────────────────────────────
    public int   ScreenW() => _eng.Width;
    public int   ScreenH() => _eng.Height;
    public float GetTime() => _eng.Time;

    // ── Nodes ─────────────────────────────────────────────────────────────
    public RectNode NewRect(float w, float h,
                            float r = 1f, float g = 1f, float b = 1f,
                            bool hud = false)
    {
        var n = new RectNode
        {
            W = w, H = h,
            R = (byte)(r*255), G = (byte)(g*255),
            B = (byte)(b*255), A = 255
        };
        _eng.AddNode(n, hud);
        return n;
    }

    public LabelNode NewLabel(string text, int fontSize = 20, bool hud = true)
    {
        var n = new LabelNode { Text = text, FontSize = fontSize };
        _eng.AddNode(n, hud);
        return n;
    }

    public void Remove(Node n)  => _eng.RemoveNode(n);
    public void ClearAll()      { _eng.ClearNodes(); _phys.Clear(); _cam.Reset(); }

    public void  SetPos(Node n, float x, float y)     { n.X = x; n.Y = y; }
    public void  Move(Node n, float dx, float dy)     { n.X += dx; n.Y += dy; }
    public void  SetPivot(Node n, float px, float py) { n.PivotX = px; n.PivotY = py; }
    public void  SetScale(Node n, float sx, float sy) { n.ScaleX = sx; n.ScaleY = sy; }
    public void  SetColor(Node n, float r, float g, float b)
    { n.R=(byte)(r*255); n.G=(byte)(g*255); n.B=(byte)(b*255); }
    public void  SetAlpha(Node n, float a)   => n.A = (byte)(a * 255);
    public void  SetVisible(Node n, bool v)  => n.Visible = v;
    public float GetX(Node n)                => n.X;
    public float GetY(Node n)                => n.Y;

    public int GetWidth(Node n) => n switch
    {
        SpriteNode s => s.FrameW,
        RectNode   r => (int)r.W,
        _            => 0
    };

    public int GetHeight(Node n) => n switch
    {
        SpriteNode s => s.FrameH,
        RectNode   r => (int)r.H,
        _            => 0
    };

    public void SetLabel(LabelNode n, string t)  => n.Text = t;
    public void SetFontSize(LabelNode n, int sz) => n.FontSize = sz;

    // ── Sprites ── v0.5 ────────────────────────────────────────────────────

    /// Carrega um PNG/JPG e cria um SpriteNode.
    /// path: relativo ao diretório do jogo   ex: "res/sprites/player.png"
    /// hud:  true → desenha na camada HUD (ignora câmera)
    public SpriteNode NewSprite(string path, bool hud = false)
    {
        var tex = ResManager.LoadTex(path);
        var n   = new SpriteNode(tex);
        _eng.AddNode(n, hud);
        return n;
    }

    /// Define um recorte de spritesheet em pixels.
    /// x, y = posição do frame na imagem; w, h = tamanho do frame.
    /// Exemplo: setFrame(node, 0, 0, 32, 32) → primeiro frame 32x32
    public void SetFrame(SpriteNode n, float x, float y, float w, float h)
        => n.Frame = new Rectangle(x, y, w, h);

    /// Volta para renderizar a textura inteira (sem recorte)
    public void ClearFrame(SpriteNode n) => n.Frame = null;

    /// Espelha o sprite horizontal e/ou verticalmente
    public void SetFlip(SpriteNode n, bool flipH, bool flipV)
    { n.FlipH = flipH; n.FlipV = flipV; }

    /// Muda o filtro de uma textura: "pixel" (nítido) ou "linear" (suave)
    public void SetFilter(string path, string mode)
    {
        var filter = mode.ToLower() == "linear"
            ? TextureFilter.Bilinear
            : TextureFilter.Point;
        ResManager.SetFilter(path, filter);
    }

    // ── Física ────────────────────────────────────────────────────────────
    public Physics.Body  AddBody(Node n) => _phys.AddBody(n);
    public Physics.Shape AddShape(Node n, float w, float h,
                                  float ox = 0f, float oy = 0f)
        => _phys.AddShape(n, w, h, ox, oy);
    public void RemoveBody(Physics.Body b)   => _phys.RemoveBody(b);
    public void RemoveShape(Physics.Shape s) => _phys.RemoveShape(s);

    public void  SetVelX(Physics.Body b, float vx) => b.VelocityX = vx;
    public void  SetVelY(Physics.Body b, float vy) => b.VelocityY = vy;
    public void  JumpY(Physics.Body b, float vy)   => b.VelocityY = vy;
    public void  SetVel(Physics.Body b, float vx, float vy)
    { b.VelocityX = vx; if (vy != 0f || b.IsGrounded) b.VelocityY = vy; }

    public float GetVelX(Physics.Body b)    => b.VelocityX;
    public float GetVelY(Physics.Body b)    => b.VelocityY;
    public bool  IsGrounded(Physics.Body b) => b.IsGrounded;

    public void SetGravScale(Physics.Body b, float s) => b.GravityScale = s;
    public void SetGravity(float g)                   => Physics.Body.Gravity = g;
    public void SetLayer(Physics.Shape s, int l)      => s.Layer   = l;
    public void SetMask(Physics.Shape s, int m)       => s.Mask    = m;
    public void SetTrigger(Physics.Shape s, bool t)   => s.Trigger = t;
    public void OnEnter(Physics.Shape s, LuaFunction f) => s.OnEnter=(a,b)=>f.Call(a,b);
    public void OnStay(Physics.Shape s, LuaFunction f)  => s.OnStay =(a,b)=>f.Call(a,b);
    public void OnExit(Physics.Shape s, LuaFunction f)  => s.OnExit =(a,b)=>f.Call(a,b);

    // ── Input ─────────────────────────────────────────────────────────────
    public bool  KeyDown(string k)         => _inp.KeyDown(k);
    public bool  KeyPressed(string k)      => _inp.KeyPressed(k);
    public float MouseX()                  => _inp.MouseX();
    public float MouseY()                  => _inp.MouseY();
    public bool  MousePressed(int b = 0)   => _inp.MousePressed(b);

    // ── Câmera ────────────────────────────────────────────────────────────
    public void CameraFollow(Node n, float smooth = 8f) => _cam.Follow(n, smooth);
    public void CameraLimits(float x0, float y0, float x1, float y1)
    { _cam.LimMinX=x0; _cam.LimMinY=y0; _cam.LimMaxX=x1; _cam.LimMaxY=y1; }
    public void CameraZoom(float z)               => _cam.Zoom = z;
    public void CameraOff()                       => _cam.Reset();
    public void CameraOffset(float ox, float oy) { _cam.OffsetX=ox; _cam.OffsetY=oy; }

    // ── Misc ──────────────────────────────────────────────────────────────
    public void  Shake(float amt, float dur) => _eng.ScreenShake(amt, dur);
    public void  Log(object msg)             => Console.WriteLine($"[Lua] {msg}");
    public int   RandomInt(int a, int b)     => new Random().Next(a, b+1);
    public float RandomFloat(float a, float b)
        => a + (float)new Random().NextDouble() * (b - a);

    // ── Draw helpers ──────────────────────────────────────────────────────
    public void DrawText_(string t,float x,float y,int sz,
                          float r=1f,float g=1f,float b=1f)
        => Raylib.DrawText(t,(int)x,(int)y,sz,
               new Color((byte)(r*255),(byte)(g*255),(byte)(b*255),255));
    public void DrawRect_(float x,float y,float w,float h,
                          float r=1f,float g=1f,float b=1f,float a=1f)
        => Raylib.DrawRectangle((int)x,(int)y,(int)w,(int)h,
               new Color((byte)(r*255),(byte)(g*255),(byte)(b*255),(byte)(a*255)));
    public void DrawLine_(float x1,float y1,float x2,float y2,
                          float r=1f,float g=1f,float b=1f)
        => Raylib.DrawLine((int)x1,(int)y1,(int)x2,(int)y2,
               new Color((byte)(r*255),(byte)(g*255),(byte)(b*255),255));
    public void DrawCircle_(float cx,float cy,float rad,
                            float r=1f,float g=1f,float b=1f)
        => Raylib.DrawCircle((int)cx,(int)cy,rad,
               new Color((byte)(r*255),(byte)(g*255),(byte)(b*255),255));
}
