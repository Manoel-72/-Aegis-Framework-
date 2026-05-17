using Aegis.Scripting;

namespace Aegis.Core;

/// <summary>
/// Ponto de entrada do Aegis Engine.
/// O desenvolvedor roda seu jogo Lua através desta classe.
/// Equivalente ao hxd.App do Heaps / love no LÖVE2D.
/// </summary>
public sealed class App
{
    public static App Instance { get; private set; } = null!;

    /// <summary>Raiz da cena de mundo (afetada pela câmera quando ativa).</summary>
    public Scene.Scene2D S2D          { get; internal set; } = null!;
    /// <summary>Raiz da camada UI/HUD (sempre em espaço de tela; desenhada em aegis_draw_ui).</summary>
    public Scene.Scene2D Ui2D       { get; internal set; } = null!;
    public LuaRuntime    Lua          { get; private set; }  = null!;
    public string        Title        { get; }
    public int           ScreenWidth  { get; }
    public int           ScreenHeight { get; }

    public App(string title = "Aegis Game", int width = 1280, int height = 720)
    {
        Instance     = this;
        Title        = string.IsNullOrWhiteSpace(title) ? "Aegis Game" : title;
        ScreenWidth  = Math.Clamp(width, 320, 7680);
        ScreenHeight = Math.Clamp(height, 240, 4320);
    }

    /// <summary>
    /// Inicia o engine, registra a API Lua e executa o script principal.
    /// Bloqueia até a janela ser fechada.
    /// </summary>
    public void Run(string luaEntryPoint)
    {
        Lua = new LuaRuntime(this);
        using var game = new AegisGame(this, luaEntryPoint);
        game.Run();
    }
}
