using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aegis.Platform;

/// <summary>
/// Define o ícone da janela em runtime quando o backend DesktopGL/SDL permite.
///
/// Importante:
/// - Falhas são silenciosas para não quebrar o jogo em Linux/macOS/Windows.
/// - O ícone do executável/taskbar é configurado no .csproj via ApplicationIcon.
/// - Este helper cuida apenas do ícone da janela já criada pelo MonoGame.
/// </summary>
internal static class WindowIcon
{
    public static void TrySet(IntPtr windowHandle, string iconPath, GraphicsDevice graphicsDevice)
    {
        if (windowHandle == IntPtr.Zero || string.IsNullOrWhiteSpace(iconPath))
            return;

        try
        {
            if (!File.Exists(iconPath))
                return;

            using var stream = File.OpenRead(iconPath);
            using var texture = Texture2D.FromStream(graphicsDevice, stream);

            if (texture.Width <= 0 || texture.Height <= 0)
                return;

            var colors = new Color[texture.Width * texture.Height];
            texture.GetData(colors);

            // SDL espera pixels contínuos. Montamos RGBA explícito para evitar depender
            // do layout interno do struct Microsoft.Xna.Framework.Color.
            var pixels = new byte[colors.Length * 4];
            for (var i = 0; i < colors.Length; i++)
            {
                var p = i * 4;
                pixels[p + 0] = colors[i].R;
                pixels[p + 1] = colors[i].G;
                pixels[p + 2] = colors[i].B;
                pixels[p + 3] = colors[i].A;
            }

            var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            try
            {
                var surface = SDL_CreateRGBSurfaceFrom(
                    handle.AddrOfPinnedObject(),
                    texture.Width,
                    texture.Height,
                    32,
                    texture.Width * 4,
                    0x000000ff,
                    0x0000ff00,
                    0x00ff0000,
                    unchecked((int)0xff000000));

                if (surface == IntPtr.Zero)
                    return;

                try
                {
                    SDL_SetWindowIcon(windowHandle, surface);
                }
                finally
                {
                    SDL_FreeSurface(surface);
                }
            }
            finally
            {
                handle.Free();
            }
        }
        catch
        {
            // Ícone nunca deve impedir o jogo de abrir.
        }
    }

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SDL_CreateRGBSurfaceFrom(
        IntPtr pixels,
        int width,
        int height,
        int depth,
        int pitch,
        int rmask,
        int gmask,
        int bmask,
        int amask);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_SetWindowIcon(IntPtr window, IntPtr icon);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_FreeSurface(IntPtr surface);
}
