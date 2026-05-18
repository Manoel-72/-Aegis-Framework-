using System.Numerics;

namespace Aegis;

public class Camera
{
    public float TargetX, TargetY;
    public float OffsetX, OffsetY;
    public float Zoom   = 1f;
    public float Smooth = 8f;

    public float LimMinX = float.MinValue, LimMinY = float.MinValue;
    public float LimMaxX = float.MaxValue, LimMaxY = float.MaxValue;

    private Node? _target;
    private float _cx, _cy;
    private readonly int _winW, _winH;

    public Camera(int winW, int winH)
    {
        _winW = winW;
        _winH = winH;
        _cx   = winW * 0.5f;
        _cy   = winH * 0.5f;
    }

    public void Follow(Node n, float smooth) { _target = n; Smooth = smooth; }

    public void Unfollow() => _target = null;

    /// Reseta todo o estado da câmera — chamado pelo LuaBridge.ClearAll()
    public void Reset()
    {
        _target  = null;
        Zoom     = 1f;
        OffsetX  = 0f;
        OffsetY  = 0f;
        LimMinX  = float.MinValue; LimMinY = float.MinValue;
        LimMaxX  = float.MaxValue; LimMaxY = float.MaxValue;
        _cx      = _winW * 0.5f;
        _cy      = _winH * 0.5f;
        TargetX  = _cx;
        TargetY  = _cy;
    }

    public void Update(float dt)
    {
        if (_target is null) return;

        float tx = _target.X + OffsetX;
        float ty = _target.Y + OffsetY;

        tx = Math.Clamp(tx, LimMinX, LimMaxX);
        ty = Math.Clamp(ty, LimMinY, LimMaxY);

        _cx += (tx - _cx) * Math.Min(Smooth * dt, 1f);
        _cy += (ty - _cy) * Math.Min(Smooth * dt, 1f);

        TargetX = _cx;
        TargetY = _cy;
    }
}
