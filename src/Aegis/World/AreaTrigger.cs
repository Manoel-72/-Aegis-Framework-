using Aegis.Scene;

namespace Aegis.World;

/// <summary>Trigger retangular leve para portais e áreas, sem depender da física.</summary>
public sealed class AreaTrigger
{
    public string Name { get; }
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public bool OneShot { get; set; }
    public bool Enabled { get; set; } = true;
    public Action<AreaTrigger, Object2D>? OnEnter { get; set; }
    public Action<AreaTrigger, Object2D>? OnStay { get; set; }
    public Action<AreaTrigger, Object2D>? OnExit { get; set; }

    private readonly HashSet<Object2D> _inside = new();

    public AreaTrigger(string name, float x, float y, float width, float height)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "trigger" : name;
        X = x; Y = y; Width = Math.Max(1, width); Height = Math.Max(1, height);
    }

    public void Check(Object2D obj)
    {
        if (!Enabled) return;
        var hit = obj.X >= X && obj.X <= X + Width && obj.Y >= Y && obj.Y <= Y + Height;
        var was = _inside.Contains(obj);
        if (hit)
        {
            if (!was)
            {
                _inside.Add(obj);
                OnEnter?.Invoke(this, obj);
                if (OneShot) Enabled = false;
            }
            else OnStay?.Invoke(this, obj);
        }
        else if (was)
        {
            _inside.Remove(obj);
            OnExit?.Invoke(this, obj);
        }
    }

    public void ResetContacts() => _inside.Clear();
}
