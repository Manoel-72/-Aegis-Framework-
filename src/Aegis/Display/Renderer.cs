using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Display;

/// <summary>
/// Wrapper singleton do SpriteBatch.
/// Garante que Begin/End sejam chamados uma vez por frame.
/// </summary>
public static class Renderer
{
    private static SpriteBatch    _sb  = null!;
    private static GraphicsDevice _gd  = null!;
    private static bool           _on;

    public static void Initialize(SpriteBatch sb, GraphicsDevice gd)
    { _sb = sb; _gd = gd; }

    public static void Begin()
    {
        if (_on) return;
        _sb.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            null, null, null, null
        );
        _on = true;
    }

    public static void End()
    {
        if (!_on) return;
        _sb.End();
        _on = false;
    }

    public static SpriteBatch    SpriteBatch    => _sb;
    public static GraphicsDevice GraphicsDevice => _gd;
}
