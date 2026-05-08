using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Aegis.Input;

/// <summary>
/// Estado de teclado e mouse com detecção de "pressionado neste frame".
/// Equivalente ao hxd.Key do Heaps.
/// </summary>
public static class InputManager
{
    private static KeyboardState _kPrev, _kCurr;
    private static MouseState    _mPrev, _mCurr;
    private static readonly GamePadState[] _padPrev = new GamePadState[4];
    private static readonly GamePadState[] _padCurr = new GamePadState[4];
    private static readonly DateTime[] _vibrateUntil = new DateTime[4];

    public static void Update()
    {
        _kPrev = _kCurr; _kCurr = Keyboard.GetState();
        _mPrev = _mCurr; _mCurr = Mouse.GetState();
        for (int i = 0; i < 4; i++) { _padPrev[i] = _padCurr[i]; _padCurr[i] = GamePad.GetState((PlayerIndex)i); if (_vibrateUntil[i] != default && DateTime.UtcNow >= _vibrateUntil[i]) { GamePad.SetVibration((PlayerIndex)i, 0f, 0f); _vibrateUntil[i] = default; } }
    }

    /// Alinha prev/curr ao hardware real (evita JustPressed falso após reload).
    public static void HardSyncFromHardware()
    {
        _kCurr = Keyboard.GetState();
        _kPrev = _kCurr;
        _mCurr = Mouse.GetState();
        _mPrev = _mCurr;
        for (int i = 0; i < 4; i++) { _padCurr[i] = GamePad.GetState((PlayerIndex)i); _padPrev[i] = _padCurr[i]; }
    }

    // ── Teclado ──────────────────────────────────────────────────────
    public static bool IsDown(Keys k)        => _kCurr.IsKeyDown(k);
    public static bool IsUp(Keys k)          => _kCurr.IsKeyUp(k);
    public static bool JustPressed(Keys k)   => _kCurr.IsKeyDown(k) && _kPrev.IsKeyUp(k);
    public static bool JustReleased(Keys k)  => _kCurr.IsKeyUp(k)   && _kPrev.IsKeyDown(k);

    // Overloads por string (usados pelo binding Lua)
    public static bool IsDown(string name)
        => Enum.TryParse<Keys>(name, true, out var k) && IsDown(k);
    public static bool JustPressed(string name)
        => Enum.TryParse<Keys>(name, true, out var k) && JustPressed(k);

    // ── Mouse ────────────────────────────────────────────────────────
    public static int  MouseX     => _mCurr.X;
    public static int  MouseY     => _mCurr.Y;
    public static bool LeftDown   => _mCurr.LeftButton  == ButtonState.Pressed;
    public static bool RightDown  => _mCurr.RightButton == ButtonState.Pressed;
    public static bool LeftJust   => _mCurr.LeftButton  == ButtonState.Pressed
                                  && _mPrev.LeftButton   == ButtonState.Released;
    public static bool RightJust  => _mCurr.RightButton == ButtonState.Pressed
                                  && _mPrev.RightButton  == ButtonState.Released;
    public static int  ScrollDelta => _mCurr.ScrollWheelValue - _mPrev.ScrollWheelValue;

    // ── Gamepad ──────────────────────────────────────────────────────
    public static bool PadConnected(int index) => ValidPad(index) && _padCurr[index].IsConnected;

    public static bool PadDown(int index, string button)
    {
        if (!ValidPad(index)) return false;
        return IsPadButtonDown(_padCurr[index], button);
    }

    public static bool PadPressed(int index, string button)
    {
        if (!ValidPad(index)) return false;
        return IsPadButtonDown(_padCurr[index], button) && !IsPadButtonDown(_padPrev[index], button);
    }

    public static float PadAxis(int index, string axis)
    {
        if (!ValidPad(index)) return 0f;
        var s = _padCurr[index];
        return axis.ToLowerInvariant() switch
        {
            "leftx" => s.ThumbSticks.Left.X,
            "lefty" => s.ThumbSticks.Left.Y,
            "rightx" => s.ThumbSticks.Right.X,
            "righty" => s.ThumbSticks.Right.Y,
            "lefttrigger" => s.Triggers.Left,
            "righttrigger" => s.Triggers.Right,
            _ => 0f
        };
    }

    public static void PadVibrate(int index, float left, float right, float seconds = 0f)
    {
        if (!ValidPad(index)) return;
        GamePad.SetVibration((PlayerIndex)index, Math.Clamp(left, 0f, 1f), Math.Clamp(right, 0f, 1f));
        _vibrateUntil[index] = seconds > 0f ? DateTime.UtcNow.AddSeconds(seconds) : default;
    }

    private static bool ValidPad(int index) => index >= 0 && index < 4;

    private static bool IsPadButtonDown(GamePadState s, string button)
    {
        var b = button.Replace("_", "").Replace("-", "").ToLowerInvariant();
        return b switch
        {
            "a" => s.Buttons.A == ButtonState.Pressed,
            "b" => s.Buttons.B == ButtonState.Pressed,
            "x" => s.Buttons.X == ButtonState.Pressed,
            "y" => s.Buttons.Y == ButtonState.Pressed,
            "start" => s.Buttons.Start == ButtonState.Pressed,
            "back" => s.Buttons.Back == ButtonState.Pressed,
            "lb" or "leftshoulder" => s.Buttons.LeftShoulder == ButtonState.Pressed,
            "rb" or "rightshoulder" => s.Buttons.RightShoulder == ButtonState.Pressed,
            "ls" or "leftstick" => s.Buttons.LeftStick == ButtonState.Pressed,
            "rs" or "rightstick" => s.Buttons.RightStick == ButtonState.Pressed,
            "dpadup" => s.DPad.Up == ButtonState.Pressed,
            "dpaddown" => s.DPad.Down == ButtonState.Pressed,
            "dpadleft" => s.DPad.Left == ButtonState.Pressed,
            "dpadright" => s.DPad.Right == ButtonState.Pressed,
            _ => false
        };
    }
}
