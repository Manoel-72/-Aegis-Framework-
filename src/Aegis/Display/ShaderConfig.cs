using Microsoft.Xna.Framework;

namespace Aegis.Display;

public sealed class ObjectShaderConfig
{
    public string Name { get; set; } = string.Empty;
    public Color Color { get; set; } = Color.White;
    public float Width { get; set; } = 1f;
    public float Progress { get; set; } = 0f;
}

public sealed class ScreenShaderConfig
{
    public string Name { get; set; } = string.Empty;
    public float Intensity { get; set; } = 0.5f;
}

public static class ShaderManager
{
    public static ScreenShaderConfig? ScreenShader { get; private set; }
    public static void SetScreenShader(string name, float intensity = 0.5f)
        => ScreenShader = new ScreenShaderConfig { Name = name.ToLowerInvariant(), Intensity = Math.Clamp(intensity, 0f, 5f) };
    public static void ClearScreenShader() => ScreenShader = null;
}
