using Aegis.Display;
using Aegis.Input;
using Aegis.Physics;
using Aegis.Resource;
using Aegis.Scene;
using Aegis.Scripting.Components;
using Microsoft.Xna.Framework;
using NLua;

namespace Aegis.Scripting;

public sealed partial class LuaRuntime
{
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
        /// <summary>Callback C# para botões criados internamente (ex: upgrade menu).</summary>
        public Action? ClickOverride;

        public Button(Object2D obj) => Obj = obj;

        public bool HitTest(int mx, int my, Func<Object2D, (float x, float y)> screenPos, Func<Object2D, (float w, float h)> size)
        {
            var (sx, sy) = screenPos(Obj);
            var (w, h) = size(Obj);
            return mx >= sx && mx <= sx + w && my >= sy && my <= sy + h;
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
            bool hovered = btn.HitTest(mx, my, GetObjectScreenPosition, GetObjectScreenSize);
            bool pressed = hovered && leftDown;

            if (hovered && !btn.WasHovered) btn.OnHover?.Call(btn.Obj);
            if (pressed && !btn.WasPressed) btn.OnPress?.Call(btn.Obj);
            if (hovered && leftJust)
            {
                btn.ClickOverride?.Invoke();  // callback C# (upgrade menu etc.)
                btn.OnClick?.Call(btn.Obj);   // callback Lua
            }

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

        var ui = ComponentFactory.IsUiLayer(opts)
            || TableBool(opts, "screen", false);
        var lbl = new Label(FontManager.Default, _components.ResolveRoot(ui))
        {
            Text  = text,
            X     = x,
            Y     = y,
            Color = new Color(r, g, b),
            Z     = 900,
        };

        _floatTexts.Add(new FloatTextEntry(lbl, duration, speed, y));
    }

    private bool IsUiObject(Object2D obj)
    {
        if (_app.Ui2D is null) return false;
        for (var n = obj; n is not null; n = n.Parent)
        {
            if (ReferenceEquals(n, _app.Ui2D)) return true;
        }
        return false;
    }

    private (float x, float y) GetObjectScreenPosition(Object2D obj)
    {
        var m = obj.GetWorldMatrix();
        var wx = m.M41;
        var wy = m.M42;
        if (IsUiObject(obj)) return (wx, wy);
        var s = Camera2D.Instance.WorldToScreen(wx, wy);
        return (s.X, s.Y);
    }

    private (float w, float h) GetObjectScreenSize(Object2D obj)
    {
        var m = obj.GetWorldMatrix();
        var scaleX = MathF.Sqrt(m.M11 * m.M11 + m.M12 * m.M12);
        var scaleY = MathF.Sqrt(m.M21 * m.M21 + m.M22 * m.M22);
        if (obj is Bitmap b)
            return (b.TextureWidth * scaleX, b.TextureHeight * scaleY);
        if (obj is Label label && label.Font is not null && !string.IsNullOrEmpty(label.Text))
        {
            var size = label.Font.MeasureString(label.Text);
            return (size.X * scaleX, size.Y * scaleY);
        }
        return (64f * scaleX, 64f * scaleY);
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

    /// <summary>aegis.newProgressBar(x, y, w, h) → objeto ProgressBar.
    /// Retorna o objeto de fundo (bg). Use setBarValue/setBarColors para controlar.</summary>
    public Object2D NewProgressBar(float x, float y, int w, int h)
        => _components.CreateProgressBar(x, y, w, h, new Color(0.15f, 0.15f, 0.15f), new Color(0.2f, 1f, 0.3f));

    /// <summary>aegis.setBarValue(barObj, current, max) — atualiza o preenchimento da barra.</summary>
    public void SetBarValue(Object2D barObj, float current, float max)
        => _components.SetProgressValue(barObj, current, max);

    /// <summary>aegis.setBarColors(barObj, opts) — define cores da barra.
    /// opts: {bg={r,g,b}, fill={r,g,b}}</summary>
    public void SetBarColors(Object2D barObj, LuaTable opts)
        => _components.SetProgressColors(barObj, opts);
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

    // ══════════════════════════════════════════════════════════════════════
    // DRAG & DROP
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>aegis.bringToFront(obj) — coloca o objeto na frente de todos os irmãos.</summary>
    public void BringToFront(Object2D obj)
    {
        if (obj.Parent is null) return;
        var siblings = obj.Parent.Children;
        var maxZ = siblings.Where(s => s != obj).Select(s => s.Z).DefaultIfEmpty(0).Max();
        obj.Z = maxZ + 1;
    }

    /// <summary>aegis.sendToBack(obj) — coloca o objeto atrás de todos os irmãos.</summary>
    public void SendToBack(Object2D obj)
    {
        if (obj.Parent is null) return;
        var siblings = obj.Parent.Children;
        var minZ = siblings.Where(s => s != obj).Select(s => s.Z).DefaultIfEmpty(0).Min();
        obj.Z = minZ - 1;
    }

    /// <summary>aegis.setZRelative(obj, delta) — incrementa/decrementa Z em delta.</summary>
    public void SetZRelative(Object2D obj, float delta) => obj.Z = (int)MathF.Round(obj.Z + delta);

    // ══════════════════════════════════════════════════════════════════════
    // HAND / CARD LAYOUT
    // ══════════════════════════════════════════════════════════════════════
}
