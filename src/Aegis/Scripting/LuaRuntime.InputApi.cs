using Aegis.Input;

namespace Aegis.Scripting;

public sealed partial class LuaRuntime
{
    public bool KeyDown(string k) => InputManager.IsDown(k);

    public bool KeyPressed(string k) => InputManager.JustPressed(k);

    public int GetMouseX() => InputManager.MouseX;

    public int GetMouseY() => InputManager.MouseY;

    public bool MouseLeft() => InputManager.LeftDown;

    public bool MouseLeftJust() => InputManager.LeftJust;

    public bool MouseRight() => InputManager.RightDown;

    public bool MouseRightJust() => InputManager.RightJust;

    public int GetScrollDelta() => InputManager.ScrollDelta;

    public bool PadConnected(int index) => InputManager.PadConnected(index);

    public bool PadDown(int index, string button) => InputManager.PadDown(index, button);

    public bool PadPressed(int index, string button) => InputManager.PadPressed(index, button);

    public float PadAxis(int index, string axis) => InputManager.PadAxis(index, axis);

    public void PadVibrate(int index, float left, float right, float seconds = 0f)
        => InputManager.PadVibrate(index, left, right, seconds);

    public int GetScreenWidth() => _app.ScreenWidth;

    public int GetScreenHeight() => _app.ScreenHeight;
}
