using Aegis.Display;
using Aegis.Input;
using Aegis.Physics;
using Aegis.Resource;
using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLua;

namespace Aegis.Scripting;

public sealed partial class LuaRuntime
{
    private sealed class DraggableEntry
    {
        public Object2D Obj;
        public bool     IsDragging;
        public float    OffX, OffY;        // offset do ponto de pega em relação ao objeto
        public LuaFunction? OnStart;
        public LuaFunction? OnMove;
        public LuaFunction? OnEnd;
        public DraggableEntry(Object2D obj) { Obj = obj; }
    }

    private readonly List<DraggableEntry> _draggables = new();
    private DraggableEntry?               _activeDrag;

    /// <summary>aegis.newDraggable(obj) — marca objeto como arrastável com mouse.</summary>
    public void NewDraggable(Object2D obj)
    {
        if (_draggables.Any(d => d.Obj == obj)) return;
        _draggables.Add(new DraggableEntry(obj));
    }

    /// <summary>aegis.onDragStart(obj, cb) / aegis.onDragMove(obj, cb) / aegis.onDragEnd(obj, cb)</summary>
    public void OnDragStart(Object2D obj, LuaFunction cb)
        => (_draggables.FirstOrDefault(d => d.Obj == obj) ?? throw new("newDraggable não chamado")).OnStart = cb;
    public void OnDragMove(Object2D obj, LuaFunction cb)
        => (_draggables.FirstOrDefault(d => d.Obj == obj) ?? throw new("newDraggable não chamado")).OnMove = cb;
    public void OnDragEnd(Object2D obj, LuaFunction cb)
        => (_draggables.FirstOrDefault(d => d.Obj == obj) ?? throw new("newDraggable não chamado")).OnEnd = cb;

    /// <summary>aegis.getDragTarget() → obj ou nil — retorna o objeto sendo arrastado.</summary>
    public Object2D? GetDragTarget() => _activeDrag?.Obj;

    /// <summary>Chamado pelo AegisGame — processa estado de drag via InputManager.</summary>
    internal void UpdateDraggables()
    {
        float mx = InputManager.MouseX;
        float my = InputManager.MouseY;
        // Converter mouse screen → world
        var world = Camera2D.Instance.ScreenToWorld(mx, my);
        float wx = world.X, wy = world.Y;

        if (InputManager.LeftJust)
        {
            // Inicia drag: verificar se mouse está sobre algum draggable (do topo do Z p/ baixo)
            var sorted = _draggables.OrderByDescending(d => d.Obj.Z).ToList();
            foreach (var dr in sorted)
            {
                var col = CollisionSystem.Instance.GetFirstCollider(dr.Obj);
                bool hit;
                if (col is not null)
                {
                    var bounds = col.Bounds;
                    hit = wx >= bounds.Left && wx <= bounds.Right &&
                          wy >= bounds.Top && wy <= bounds.Bottom;
                }
                else
                {
                    hit = wx >= dr.Obj.X && wx <= dr.Obj.X + 32 &&
                          wy >= dr.Obj.Y && wy <= dr.Obj.Y + 32;
                }
                if (!hit) continue;
                dr.IsDragging  = true;
                dr.OffX        = dr.Obj.X - wx;
                dr.OffY        = dr.Obj.Y - wy;
                _activeDrag    = dr;
                dr.OnStart?.Call(dr.Obj, wx, wy);
                break;
            }
        }

        if (_activeDrag is not null && InputManager.LeftDown)
        {
            _activeDrag.Obj.X = wx + _activeDrag.OffX;
            _activeDrag.Obj.Y = wy + _activeDrag.OffY;
            _activeDrag.OnMove?.Call(_activeDrag.Obj, wx, wy);
        }

        if (_activeDrag is not null && !InputManager.LeftDown)
        {
            _activeDrag.IsDragging = false;
            _activeDrag.OnEnd?.Call(_activeDrag.Obj, wx, wy);
            _activeDrag = null;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Z-ORDER DINÂMICO
    // ══════════════════════════════════════════════════════════════════════

    private sealed class HandLayoutState
    {
        public List<Object2D> Cards = new();
        public float CenterX, BaseY;
        public float FanAngle;   // ângulo total do leque em graus
        public float Spacing;    // espaço horizontal entre cartas
        public float HoverLift;  // quanto a carta sobe no hover (px)
        public int   HoveredIdx = -1;
    }

    private readonly Dictionary<int, HandLayoutState> _hands = new();
    private int _handNextId = 1;

    /// <summary>aegis.newHand(cx, cy, opts) → handId
    /// opts: {fanAngle=30, spacing=60, hoverLift=20}</summary>
    public int NewHand(float cx, float cy, LuaTable? opts = null)
    {
        int id = _handNextId++;
        _hands[id] = new HandLayoutState
        {
            CenterX   = cx,
            BaseY     = cy,
            FanAngle  = TableFloat(opts, "fanAngle",  30f),
            Spacing   = TableFloat(opts, "spacing",   60f),
            HoverLift = TableFloat(opts, "hoverLift", 20f),
        };
        return id;
    }

    /// <summary>aegis.handAdd(handId, obj) — adiciona carta/objeto à mão.</summary>
    public void HandAdd(int handId, Object2D obj)
    {
        if (!_hands.TryGetValue(handId, out var h)) return;
        h.Cards.Add(obj);
        ApplyHandLayout(h);
    }

    /// <summary>aegis.handRemove(handId, obj) — remove carta da mão.</summary>
    public void HandRemove(int handId, Object2D obj)
    {
        if (!_hands.TryGetValue(handId, out var h)) return;
        h.Cards.Remove(obj);
        ApplyHandLayout(h);
    }

    /// <summary>aegis.handLayout(handId) — força re-layout (após mover cartas manualmente).</summary>
    public void HandLayout(int handId)
    {
        if (_hands.TryGetValue(handId, out var h)) ApplyHandLayout(h);
    }

    /// <summary>aegis.handSetHover(handId, idx) — eleva a carta idx (1-based), -1 para nenhuma.</summary>
    public void HandSetHover(int handId, int idx)
    {
        if (!_hands.TryGetValue(handId, out var h)) return;
        h.HoveredIdx = idx - 1; // converter para 0-based
        ApplyHandLayout(h);
    }

    private static void ApplyHandLayout(HandLayoutState h)
    {
        int n = h.Cards.Count;
        if (n == 0) return;

        float totalW = h.Spacing * (n - 1);
        float startX = h.CenterX - totalW * 0.5f;
        float halfFan = h.FanAngle * 0.5f;

        for (int i = 0; i < n; i++)
        {
            var card  = h.Cards[i];
            float t   = n == 1 ? 0f : (float)i / (n - 1) * 2f - 1f; // -1..+1
            float rot = t * halfFan;
            float liftY = (i == h.HoveredIdx) ? -h.HoverLift : 0f;
            // Tween suave até posição final
            card.X        = startX + i * h.Spacing;
            card.Y        = h.BaseY + MathF.Abs(t) * 8f + liftY; // leve arco
            card.Rotation = rot * (MathF.PI / 180f);
            card.Z        = i; // Z incremental para sobreposição correta
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // CÂMERA AUTOZOOM POR DENSIDADE
    // ══════════════════════════════════════════════════════════════════════

    private bool  _autozoomEnabled;
    private float _autozoomMinZoom = 0.5f;
    private float _autozoomMaxZoom = 1.5f;
    private float _autozoomRadius  = 400f;   // raio em torno do player para contar inimigos
    private float _autozoomTarget  = 1f;
    private int   _autozoomDensityThreshold = 5; // inimigos para começar a zoom out

    /// <summary>aegis.setCameraAutozoom(enable, opts)
    /// opts: {minZoom=0.5, maxZoom=1.5, radius=400, densityThreshold=5}</summary>
    public void SetCameraAutozoom(bool enable, LuaTable? opts = null)
    {
        _autozoomEnabled           = enable;
        _autozoomMinZoom           = TableFloat(opts, "minZoom",           0.5f);
        _autozoomMaxZoom           = TableFloat(opts, "maxZoom",           1.5f);
        _autozoomRadius            = TableFloat(opts, "radius",            400f);
        _autozoomDensityThreshold  = (int)TableFloat(opts, "densityThreshold", 5f);
    }

    /// <summary>Chamado pelo AegisGame — recalcula zoom alvo por densidade de colliders próximos.</summary>
    internal void UpdateAutozoom(float dt)
    {
        if (!_autozoomEnabled) return;

        float cx = Camera2D.Instance.X + Camera2D.Instance.ViewWidth  * 0.5f;
        float cy = Camera2D.Instance.Y + Camera2D.Instance.ViewHeight * 0.5f;

        // Conta colliders ativos no raio (usa OverlapCircle já implementado)
        int nearby = CollisionSystem.Instance.Colliders
            .Count(c => c.IsActive && !c.IsTrigger
                && MathF.Sqrt(MathF.Pow(c.Bounds.Left + c.Bounds.Width * 0.5f - cx, 2)
                            + MathF.Pow(c.Bounds.Top  + c.Bounds.Height* 0.5f - cy, 2))
                   <= _autozoomRadius);

        // Interpola zoom: muitos inimigos → zoom out (zoom menor)
        float t = Math.Clamp((nearby - _autozoomDensityThreshold) / 20f, 0f, 1f);
        _autozoomTarget = _autozoomMaxZoom + (_autozoomMinZoom - _autozoomMaxZoom) * t;

        float currentZoom = Camera2D.Instance.Zoom;
        float newZoom = currentZoom + (_autozoomTarget - currentZoom) * Math.Clamp(dt * 2f, 0f, 1f);
        Camera2D.Instance.SetZoom(newZoom);
    }

    // ══════════════════════════════════════════════════════════════════════
    // SISTEMA DE UPGRADES / SKILL TREE
    // ══════════════════════════════════════════════════════════════════════

    private sealed class UpgradeOption
    {
        public string  Id;
        public string  Title;
        public string  Desc;
        public string? Icon;   // path de sprite opcional
        public int     Level;
        public int     MaxLevel;
        public UpgradeOption(string id, string title, string desc, int maxLevel)
        { Id = id; Title = title; Desc = desc; MaxLevel = maxLevel; }
    }

    private readonly Dictionary<string, UpgradeOption> _upgrades  = new();
    private LuaFunction?                                _onUpgradeChosen;
    private Object2D?                                   _upgradeMenuRoot;

    /// <summary>aegis.addUpgrade(id, title, desc, opts)
    /// opts: {maxLevel=3, icon="path"}</summary>
    public void AddUpgrade(string id, string title, string desc, LuaTable? opts = null)
    {
        int maxLvl = (int)TableFloat(opts, "maxLevel", 3f);
        var up = new UpgradeOption(id, title, desc, maxLvl);
        up.Icon = opts?["icon"] as string;
        _upgrades[id] = up;
    }

    /// <summary>aegis.onUpgradeChosen(cb) — cb(id, level) chamado quando player escolhe upgrade.</summary>
    public void OnUpgradeChosen(LuaFunction cb) => _onUpgradeChosen = cb;

    /// <summary>aegis.getUpgradeLevel(id) → level atual (0 = não adquirido).</summary>
    public int GetUpgradeLevel(string id)
        => _upgrades.TryGetValue(id, out var u) ? u.Level : 0;

    /// <summary>aegis.showUpgrades(count) — exibe menu de escolha com `count` opções aleatórias não-maxadas.</summary>
    public void ShowUpgrades(int count)
    {
        HideUpgrades();

        var available = _upgrades.Values
            .Where(u => u.Level < u.MaxLevel)
            .OrderBy(_ => Guid.NewGuid())
            .Take(count)
            .ToList();

        if (available.Count == 0) return;

        // Constrói UI nativa com botões
        float panelW = 280f * available.Count + 40f;
        float panelH = 200f;
        float sx = Camera2D.Instance.ViewWidth  * 0.5f - panelW * 0.5f;
        float sy = Camera2D.Instance.ViewHeight * 0.5f - panelH * 0.5f;

        // Root sem texture — só container lógico
        var root = new Object2D(_app.S2D) { X = sx, Y = sy, Z = 1000 };
        _upgradeMenuRoot = root;

        for (int i = 0; i < available.Count; i++)
        {
            var up  = available[i];
            var capturedUp = up;
            float bx = i * 290f;

            // Painel de fundo
            var bgTex = new Microsoft.Xna.Framework.Graphics.Texture2D(Renderer.GraphicsDevice, 270, (int)panelH);
            bgTex.SetData(Enumerable.Repeat(new Microsoft.Xna.Framework.Color(0.1f, 0.1f, 0.2f, 0.92f),
                270 * (int)panelH).ToArray());
            var bg = new Bitmap(bgTex, root) { X = bx };

            // Título
            if (FontManager.Default is not null)
            {
                var titleLbl = new Label(FontManager.Default, root)
                {
                    Text = $"{up.Title}  (Lv {up.Level + 1})",
                    X    = bx + 10, Y = 12, Z = 1001,
                    Color = Microsoft.Xna.Framework.Color.White
                };
                var descLbl = new Label(FontManager.Default, root)
                {
                    Text = up.Desc,
                    X    = bx + 10, Y = 48, Z = 1001,
                    Color = new Microsoft.Xna.Framework.Color(0.8f, 0.8f, 0.8f)
                };
            }

            // Botão invisível cobrindo o painel — Object2D simples com dimensões do painel
            var btnObj = new Object2D(root) { X = bx, Y = 0f, Z = 1002 };
            var btn = new Button(btnObj);
            btn.ClickOverride = () =>
            {
                capturedUp.Level++;
                _onUpgradeChosen?.Call(capturedUp.Id, capturedUp.Level);
                HideUpgrades();
            };
            _buttons.Add(btn);
        }
    }

    /// <summary>aegis.hideUpgrades() — fecha o menu de upgrades manualmente.</summary>
    public void HideUpgrades()
    {
        _upgradeMenuRoot?.RemoveFromParent();
        _upgradeMenuRoot = null;
    }
}