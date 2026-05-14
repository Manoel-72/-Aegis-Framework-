using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AegisEditor.Services;

/// <summary>Localiza a raiz do repositório Aegis onde está <c>src/Aegis.CLI/Aegis.CLI.csproj</c>.</summary>
internal static class AegisEngineLocator
{
    internal static string? FindRepoRootContainingCli()
    {
        var candidates = new[]
        {
            AppContext.BaseDirectory,
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
        };

        foreach (var startRaw in candidates)
        {
            if (string.IsNullOrWhiteSpace(startRaw)) continue;
            var dir = Path.GetFullPath(startRaw.Replace('\\', Path.DirectorySeparatorChar));
            for (var depth = 0; depth < 24 && Directory.Exists(dir); depth++)
            {
                var cli = Path.Combine(dir, "src", "Aegis.CLI", "Aegis.CLI.csproj");
                if (File.Exists(cli))
                    return dir;

                var parent = Directory.GetParent(dir);
                dir = parent?.FullName ?? string.Empty;
            }
        }

        return null;
    }

    /// <summary>Executável <c>aegis-cli</c> já compilado (evita flaky <c>dotnet run</c> quando o pai é uma app GUI).</summary>
    internal static string? FindPrebuiltAegisCliExecutable(string repoRoot)
    {
        var name = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "aegis-cli.exe" : "aegis-cli";
        var candidates = new[]
        {
            Path.Combine(repoRoot, "src", "Aegis.CLI", "bin", "Debug", "net8.0", name),
            Path.Combine(repoRoot, "src", "Aegis.CLI", "bin", "Release", "net8.0", name),
        };

        foreach (var p in candidates)
        {
            if (File.Exists(p))
                return p;
        }

        return null;
    }

    internal static string? DefaultDemoNearRepo(string repoRoot)
    {
        var demo = Path.Combine(repoRoot, "demo-platformer");
        return Directory.Exists(demo) && File.Exists(Path.Combine(demo, "main.lua"))
            ? demo
            : null;
    }
}
