using Aegis.Input;
using Aegis.Physics;
using Aegis.Resource;
using Aegis.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Systems;

public sealed class DebugOverlay
{
    public static DebugOverlay Instance { get; } = new();
    private DebugOverlay() { }

    public bool Visible { get; private set; }
    private float _fpsTimer; private int _frames; private int _fps;

    public void Update(float dt)
    {
        if (InputManager.JustPressed("F1")) Visible = !Visible;
        _frames++; _fpsTimer += dt;
        if (_fpsTimer >= 1f) { _fps = _frames; _frames = 0; _fpsTimer = 0f; }
    }

    public void DrawHitboxes(SpriteBatch sb)
    {
        if (!Visible) return;
        foreach (var c in CollisionSystem.Instance.Colliders)
        {
            var b = c.Bounds;
            var col = c.IsTrigger ? Color.Yellow : Color.LimeGreen;
            DrawLine(sb, b.Left, b.Top, b.Right, b.Top, col);
            DrawLine(sb, b.Right, b.Top, b.Right, b.Bottom, col);
            DrawLine(sb, b.Right, b.Bottom, b.Left, b.Bottom, col);
            DrawLine(sb, b.Left, b.Bottom, b.Left, b.Top, col);
        }
    }

    public void Draw(SpriteBatch sb, Scene2D scene)
    {
        if (!Visible || FontManager.Default is null) return;
        var text = $"Aegis Debug F1\nFPS: {_fps}\nObjects: {CountObjects(scene)}\nBodies: {PhysicsWorld.Instance.BodyCount}\nColliders: {CollisionSystem.Instance.ColliderCount}\n{HotReloadManager.Instance.LastStatus}";
        sb.Draw(ResManager.Pixel, new Rectangle(8, 8, 350, 116), Color.Black * 0.65f);
        sb.DrawString(FontManager.Default, text, new Vector2(16, 14), Color.LimeGreen);
    }

    public int CountObjects(Object2D root)
    {
        var total = 1;
        foreach (var c in root.Children) total += CountObjects(c);
        return total;
    }

    private static void DrawLine(SpriteBatch sb, float x1, float y1, float x2, float y2, Color color)
    {
        var dx = x2 - x1; var dy = y2 - y1; var len = MathF.Sqrt(dx * dx + dy * dy);
        if (len <= 0.001f) return;
        sb.Draw(ResManager.Pixel, new Vector2(x1, y1), null, color, MathF.Atan2(dy, dx), Vector2.Zero, new Vector2(len, 2f), SpriteEffects.None, 0f);
    }
}
