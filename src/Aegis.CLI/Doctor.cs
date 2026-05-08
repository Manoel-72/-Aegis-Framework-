using System.Diagnostics;

namespace Aegis.CLI;

public static class Doctor
{
    private const string ToolVersion = "0.1.2";

    public static void Run(string[] args)
    {
        var cwd = Directory.GetCurrentDirectory();
        Console.WriteLine("[Aegis Doctor] Diagnostico rapido");
        Console.WriteLine($"- Pasta atual: {cwd}");

        var hasToml = File.Exists(Path.Combine(cwd, "aegis.toml"));
        var hasMain = File.Exists(Path.Combine(cwd, "main.lua"));
        var scriptFromToml = GetEntryFromToml(Path.Combine(cwd, "aegis.toml")) ?? "main.lua";
        var hasEntryScript = File.Exists(Path.Combine(cwd, scriptFromToml));

        PrintCheck("Projeto Aegis detectado (aegis.toml)", hasToml);
        PrintCheck("main.lua presente", hasMain);
        PrintCheck($"Script de entrada '{scriptFromToml}' presente", hasEntryScript);

        var globalToolVersion = TryGetGlobalToolVersion("aegisengine.cli");
        PrintCheck("CLI global instalada", !string.IsNullOrWhiteSpace(globalToolVersion));
        if (!string.IsNullOrWhiteSpace(globalToolVersion))
        {
            Console.WriteLine($"  - Versao global detectada: {globalToolVersion}");
        }

        var repoRoot = FindRepoRoot(cwd);
        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            Console.WriteLine($"- Root da engine detectada: {repoRoot}");
            Console.WriteLine();
            Console.WriteLine("Correcao curta (2 comandos):");
            Console.WriteLine($"  cd \"{repoRoot}\"");
            Console.WriteLine("  dotnet pack .\\src\\Aegis.CLI\\Aegis.CLI.csproj -c Release -o .\\nupkg");
            Console.WriteLine($"  dotnet tool update -g AegisEngine.CLI --version {ToolVersion} --add-source .\\nupkg");
        }
        else
        {
            Console.WriteLine("- Root da engine nao encontrada a partir desta pasta.");
            Console.WriteLine("  Dica: rode o doctor de dentro do repo da engine para receber comandos de update prontos.");
        }

        if (args.Contains("--fix", StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                Console.WriteLine();
                Console.WriteLine("[Aegis Doctor] --fix ignorado: root da engine nao localizada.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("[Aegis Doctor] Aplicando correcao automatica...");
            var okPack = RunProcess("dotnet", "pack .\\src\\Aegis.CLI\\Aegis.CLI.csproj -c Release -o .\\nupkg", repoRoot);
            if (!okPack)
            {
                Console.WriteLine("[Aegis Doctor] Falha no pack. Correcao abortada.");
                return;
            }

            var okUpdate = RunProcess("dotnet", $"tool update -g AegisEngine.CLI --version {ToolVersion} --add-source .\\nupkg", repoRoot);
            if (!okUpdate)
            {
                Console.WriteLine("[Aegis Doctor] Update falhou. Tentando fallback uninstall + install...");
                var okUninstall = RunProcess("dotnet", "tool uninstall -g AegisEngine.CLI", repoRoot);
                var okInstall = RunProcess("dotnet", $"tool install -g AegisEngine.CLI --version {ToolVersion} --add-source .\\nupkg", repoRoot);

                Console.WriteLine(okUninstall && okInstall
                    ? "[Aegis Doctor] Correcao concluida com fallback. Rode: aegis run"
                    : "[Aegis Doctor] Falha ao aplicar fallback (uninstall/install).");
                return;
            }

            Console.WriteLine("[Aegis Doctor] Correcao concluida via update. Rode: aegis run");
        }
    }

    private static void PrintCheck(string title, bool ok)
        => Console.WriteLine($"- {(ok ? "OK" : "ERRO")} {title}");

    private static string? GetEntryFromToml(string tomlPath)
    {
        if (!File.Exists(tomlPath)) return null;
        foreach (var line in File.ReadAllLines(tomlPath))
        {
            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;
            if (parts[0].Trim() == "entry")
                return parts[1].Trim().Trim('"');
        }
        return null;
    }

    private static string? TryGetGlobalToolVersion(string packageId)
    {
        try
        {
            var psi = new ProcessStartInfo("dotnet", "tool list -g")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p is null) return null;
            var stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            if (p.ExitCode != 0) return null;

            foreach (var line in stdout.Split(Environment.NewLine))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith(packageId, StringComparison.OrdinalIgnoreCase))
                    continue;
                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) return parts[1];
            }
        }
        catch
        {
            // ignored on purpose: doctor should continue with best effort
        }
        return null;
    }

    private static string? FindRepoRoot(string startDir)
    {
        var current = new DirectoryInfo(startDir);
        while (current is not null)
        {
            var cliCsproj = Path.Combine(current.FullName, "src", "Aegis.CLI", "Aegis.CLI.csproj");
            if (File.Exists(cliCsproj)) return current.FullName;
            current = current.Parent;
        }
        return null;
    }

    private static bool RunProcess(string fileName, string arguments, string workingDir)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            if (!string.IsNullOrWhiteSpace(stdout)) Console.WriteLine(stdout.TrimEnd());
            if (!string.IsNullOrWhiteSpace(stderr)) Console.WriteLine(stderr.TrimEnd());
            return p.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Aegis Doctor] Erro ao executar comando: {ex.Message}");
            return false;
        }
    }
}
