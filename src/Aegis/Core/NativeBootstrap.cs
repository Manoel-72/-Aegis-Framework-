using System.Runtime.InteropServices;

namespace Aegis.Core;

/// <summary>
/// MonoGame carrega SDL/OpenAL por nome simples numa pasta fixa.
/// Casos em que <c>runtimes/{rid}/native/</c> existe mas <c>SDL2.dll</c> não está ao lado do host:
/// <c>dotnet tool</c> (só em subpastas), <c>PublishSingleFile</c> (BaseDirectory vs pasta do .exe).
/// Copiamos as DLLs nativas para cada pasta candidata antes de instanciar <see cref="AegisGame"/>.
/// </summary>
internal static class NativeBootstrap
{
    private static bool _done;

    public static void EnsureForCurrentPlatform()
    {
        if (_done) return;
        _done = true;

        var roots = LayoutRoots();
        if (roots.Length == 0) return;

        foreach (var rid in RuntimeRidCandidates())
        {
            string? nativeSource = null;
            foreach (var root in roots)
            {
                var nd = Path.Combine(root, "runtimes", rid, "native");
                if (!Directory.Exists(nd)) continue;
                nativeSource = nd;
                break;
            }

            if (nativeSource == null) continue;

            foreach (var dest in roots)
            {
                foreach (var src in Directory.EnumerateFiles(nativeSource))
                {
                    var name = Path.GetFileName(src);
                    if (string.IsNullOrEmpty(name)) continue;
                    var dst = Path.Combine(dest, name);
                    try
                    {
                        if (File.Exists(dst)) continue;
                        File.Copy(src, dst, overwrite: false);
                    }
                    catch
                    {
                        // Pasta só leitura ou ficheiro bloqueado — ignorar.
                    }
                }
            }

            return;
        }
    }

    private static string[] LayoutRoots()
    {
        var list = new List<string>();

        void TryAdd(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var full = Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!list.Exists(p => p.Equals(full, StringComparison.OrdinalIgnoreCase)))
                list.Add(full);
        }

        TryAdd(AppContext.BaseDirectory);
        var ep = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(ep))
            TryAdd(Path.GetDirectoryName(ep));

        return list.ToArray();
    }

    private static IEnumerable<string> RuntimeRidCandidates()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "win-x64",
                Architecture.X86 => "win-x86",
                Architecture.Arm64 => "win-arm64",
                Architecture.Arm => "win-arm",
                _ => "win-x64"
            };
            yield break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return "osx";
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                yield return "osx-arm64";
            else
                yield return "osx-x64";
            yield break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            yield return "linux-x64";
            yield break;
        }
    }
}
