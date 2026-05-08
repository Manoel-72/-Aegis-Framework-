using Aegis.Core;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

static int Fail(string msg)
{
    Console.Error.WriteLine($"[aegis] ERRO: {msg}");
    return 1;
}

static void Info(string msg) => Console.WriteLine($"[aegis] {msg}");

static void PrintHelp()
{
    Console.WriteLine("Aegis Engine CLI");
    Console.WriteLine();
    Console.WriteLine("Uso:");
    Console.WriteLine("  aegis run <pasta-do-jogo>");
    Console.WriteLine("  aegis new <nome>");
    Console.WriteLine("  aegis new platformer|topdown|puzzle <nome>");
    Console.WriteLine("  aegis build [pasta-do-jogo] --target win-x64|linux-x64|osx|osx-x64|web");
    Console.WriteLine("  aegis publish --itch user/jogo [--target win-x64]");
    Console.WriteLine("  aegis doctor");
    Console.WriteLine("  aegis update");
    Console.WriteLine();
    Console.WriteLine("Exemplos:");
    Console.WriteLine("  aegis build physics-lab --target win-x64");
    Console.WriteLine("  aegis publish --itch manoel/meu-jogo --target win-x64");
}

static string? ArgValue(string[] args, string name)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            return args[i + 1];
        if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
            return args[i][(name.Length + 1)..];
    }
    return null;
}

static string? PositionalAfterCommand(string[] args)
{
    for (var i = 1; i < args.Length; i++)
    {
        if (args[i].StartsWith("--")) { i++; continue; }
        return args[i];
    }
    return null;
}

static void CopyOfficialLogo(string targetResDir)
{
    Directory.CreateDirectory(targetResDir);
    var sourceResCandidates = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "res"),
        Path.Combine(FindRepoRoot(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory(), "res"),
        Path.Combine(FindRepoRoot(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory(), "src", "Aegis.CLI", "res")
    };

    foreach (var file in new[] { "aegis-logo.png", "aegis-logo.svg", "aegis-logo.ico" })
    {
        foreach (var sourceRes in sourceResCandidates)
        {
            var src = Path.Combine(sourceRes, file);
            if (File.Exists(src))
            {
                File.Copy(src, Path.Combine(targetResDir, file), overwrite: true);
                break;
            }
        }
    }
}

static (string title, int width, int height) ReadToml(string toml)
{
    var title = "Aegis Game"; var width = 1280; var height = 720;
    if (!File.Exists(toml)) return (title, width, height);
    foreach (var raw in File.ReadAllLines(toml))
    {
        var line = raw.Trim();
        if (line.StartsWith('#') || !line.Contains('=')) continue;
        var parts = line.Split('=', 2);
        var key = parts[0].Trim().ToLowerInvariant();
        var val = parts[1].Trim().Trim('"');
        if (key == "title") title = val;
        else if (key == "width" && int.TryParse(val, out var w)) width = Math.Clamp(w, 320, 7680);
        else if (key == "height" && int.TryParse(val, out var h)) height = Math.Clamp(h, 240, 4320);
    }
    return (title, width, height);
}

static string SanitizeName(string value)
{
    var invalid = Path.GetInvalidFileNameChars().Concat(new[] { ' ', '/', '\\', ':', ';', ',', '\'', '"' }).ToHashSet();
    var sb = new StringBuilder();
    foreach (var ch in value.Trim())
    {
        if (invalid.Contains(ch)) sb.Append('-');
        else if (char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.') sb.Append(ch);
    }
    var s = sb.ToString().Trim('-', '_', '.');
    return string.IsNullOrWhiteSpace(s) ? "aegis-game" : s.ToLowerInvariant();
}

static string? FindRepoRoot(string start)
{
    var dir = new DirectoryInfo(Path.GetFullPath(start));
    while (dir != null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Aegis.sln")) && File.Exists(Path.Combine(dir.FullName, "src", "Aegis.CLI", "Aegis.CLI.csproj")))
            return dir.FullName;
        dir = dir.Parent;
    }
    return null;
}

static string NormalizeTarget(string target)
{
    target = target.Trim().ToLowerInvariant();
    return target switch
    {
        "win" or "windows" => "win-x64",
        "linux" => "linux-x64",
        "mac" or "macos" or "osx" => "osx-x64",
        "web" or "wasm" => "web",
        _ => target
    };
}

static string ChannelForTarget(string target) => NormalizeTarget(target) switch
{
    "win-x64" => "windows",
    "linux-x64" => "linux",
    "osx-x64" or "osx-arm64" => "mac",
    "web" => "html5",
    var t => t
};

static bool ShouldSkipDir(string name)
{
    name = name.ToLowerInvariant();
    return name is "bin" or "obj" or "dist" or ".git" or ".vs" or ".idea" or ".vscode" or "packages" or "node_modules";
}

static bool ShouldSkipFile(string name)
{
    var lower = name.ToLowerInvariant();
    return lower.EndsWith(".zip") || lower.EndsWith(".nupkg") || lower.EndsWith(".user") || lower.EndsWith(".suo") || lower.EndsWith(".tmp");
}

static void CopyGameFiles(string sourceDir, string destDir)
{
    Directory.CreateDirectory(destDir);
    foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
    {
        var rel = Path.GetRelativePath(sourceDir, dir);
        if (rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(ShouldSkipDir)) continue;
        Directory.CreateDirectory(Path.Combine(destDir, rel));
    }
    foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
    {
        var rel = Path.GetRelativePath(sourceDir, file);
        var parts = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (parts.Any(ShouldSkipDir) || ShouldSkipFile(Path.GetFileName(file))) continue;
        var dst = Path.Combine(destDir, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
        File.Copy(file, dst, overwrite: true);
    }
}

static int RunProcess(string exe, string arguments, string? workingDir = null)
{
    Info($"> {exe} {arguments}");
    try
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = arguments,
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory(),
            UseShellExecute = false
        });
        if (p == null) return 1;
        p.WaitForExit();
        return p.ExitCode;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

static int Doctor()
{
    Console.WriteLine("Aegis Doctor");
    Console.WriteLine($"OS: {Environment.OSVersion}");
    Console.WriteLine($".NET runtime: {Environment.Version}");
    Console.WriteLine($"Dir: {Directory.GetCurrentDirectory()}");
    Console.WriteLine(File.Exists("main.lua") ? "OK main.lua" : "WARN main.lua não encontrado no diretório atual");
    Console.WriteLine(Directory.Exists("res") ? "OK res/" : "INFO res/ não encontrado");
    Console.WriteLine(File.Exists("aegis.toml") ? "OK aegis.toml" : "INFO aegis.toml ausente, padrões serão usados");
    Console.WriteLine(File.Exists("aegis.cfg") ? "OK aegis.cfg" : "INFO aegis.cfg será criado ao rodar");

    var dotnet = RunProcess("dotnet", "--version");
    Console.WriteLine(dotnet == 0 ? "OK dotnet encontrado" : "WARN dotnet não encontrado no PATH");

    var butler = RunProcess("butler", "-V");
    Console.WriteLine(butler == 0 ? "OK butler encontrado" : "INFO butler não encontrado; necessário para aegis publish --itch");
    return 0;
}

static int UpdateSelf()
{
    Info("tentando atualizar a ferramenta global AegisEngine.CLI");
    var code = RunProcess("dotnet", "tool update -g AegisEngine.CLI");
    if (code == 0) return 0;
    Console.WriteLine("Se estiver usando build local, rode:");
    Console.WriteLine("  dotnet pack src/Aegis.CLI -c Release");
    Console.WriteLine("  dotnet tool uninstall -g AegisEngine.CLI");
    Console.WriteLine("  dotnet tool install -g --add-source ./src/Aegis.CLI/bin/Release AegisEngine.CLI");
    return code;
}

static int BuildWebStub(string gameDir, string distRoot, string packageName)
{
    var outDir = Path.Combine(distRoot, packageName + "-web");
    if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
    Directory.CreateDirectory(outDir);
    CopyGameFiles(gameDir, Path.Combine(outDir, "game"));
    File.WriteAllText(Path.Combine(outDir, "index.html"), """
<!doctype html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Aegis Web Export</title>
  <style>body{font-family:system-ui;background:#101512;color:#d8ffe3;display:grid;place-items:center;min-height:100vh;margin:0}.card{max-width:760px;padding:32px;border:1px solid #315840;border-radius:18px;background:#172019}code{background:#0b0f0c;padding:2px 6px;border-radius:6px}</style>
</head>
<body>
  <main class="card">
    <h1>Aegis Web/WASM ainda não está ativo</h1>
    <p>Este pacote foi gerado pelo comando <code>aegis build --target web</code>, mas a engine atual usa backend DesktopGL/MonoGame, que não exporta este jogo para WASM automaticamente.</p>
    <p>Para itch.io hoje, publique o build Windows/Linux/macOS como downloadable. Para HTML5 real, a Aegis precisa de um backend WebGL/WASM separado.</p>
  </main>
</body>
</html>
""");
    File.WriteAllText(Path.Combine(outDir, "README-WEB.txt"), "Aegis Web/WASM: export preparado, mas backend WebGL/WASM ainda não implementado. Publique nativo no itch.io por enquanto.\n");

    var zipPath = Path.Combine(distRoot, packageName + "-web.zip");
    if (File.Exists(zipPath)) File.Delete(zipPath);
    ZipFile.CreateFromDirectory(outDir, zipPath, CompressionLevel.SmallestSize, includeBaseDirectory: false);
    Info($"web stub gerado: {zipPath}");
    return 0;
}

static int Build(string[] args)
{
    var target = NormalizeTarget(ArgValue(args, "--target") ?? "win-x64");
    if (target is not ("win-x64" or "linux-x64" or "osx-x64" or "osx-arm64" or "web"))
        return Fail($"target inválido: {target}. Use win-x64, linux-x64, osx, osx-x64, osx-arm64 ou web.");

    var gameArg = PositionalAfterCommand(args) ?? ".";
    var gameDir = Path.GetFullPath(gameArg);
    if (!Directory.Exists(gameDir)) return Fail($"pasta do jogo não encontrada: {gameDir}");
    if (!File.Exists(Path.Combine(gameDir, "main.lua"))) return Fail($"main.lua não encontrado em: {gameDir}");

    var cfg = ReadToml(Path.Combine(gameDir, "aegis.toml"));
    var packageName = SanitizeName(cfg.title == "Aegis Game" ? new DirectoryInfo(gameDir).Name : cfg.title);
    var repoRoot = FindRepoRoot(Directory.GetCurrentDirectory()) ?? FindRepoRoot(AppContext.BaseDirectory);
    if (repoRoot == null) return Fail("não encontrei Aegis.sln/src/Aegis.CLI. Rode o build dentro do repositório da engine.");

    var distRoot = Path.Combine(repoRoot, "dist");
    Directory.CreateDirectory(distRoot);

    if (target == "web") return BuildWebStub(gameDir, distRoot, packageName);

    var outDir = Path.Combine(distRoot, packageName + "-" + target);
    if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
    Directory.CreateDirectory(outDir);

    var csproj = Path.Combine(repoRoot, "src", "Aegis.CLI", "Aegis.CLI.csproj");
    var publishArgs = $"publish \"{csproj}\" -c Release -r {target} --self-contained true " +
                      $"-p:PublishSingleFile=false -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -o \"{outDir}\"";
    var code = RunProcess("dotnet", publishArgs, repoRoot);
    if (code != 0) return Fail("dotnet publish falhou. Rode aegis doctor para diagnosticar.");

    CopyGameFiles(gameDir, outDir);
    CopyOfficialLogo(Path.Combine(outDir, "res"));

    if (target.StartsWith("win", StringComparison.OrdinalIgnoreCase))
    {
        var bat = "@echo off\r\n" +
                  "cd /d %~dp0\r\n" +
                  $"echo Iniciando {packageName}...\r\n" +
                  "\"%~dp0aegis-cli.exe\"\r\n" +
                  "if errorlevel 1 (\r\n" +
                  "  echo.\r\n" +
                  "  echo O jogo fechou com erro. Veja crash.log nesta pasta.\r\n" +
                  "  pause\r\n" +
                  ")\r\n";
        File.WriteAllText(Path.Combine(outDir, "JOGAR.bat"), bat, Encoding.UTF8);
    }

    File.WriteAllText(Path.Combine(outDir, "README-RUN.txt"),
        $"Build Aegis: {packageName}\nTarget: {target}\n\n" +
        "Windows: abra JOGAR.bat para ver erros, ou execute aegis-cli.exe na pasta do build.\n" +
        "Não copie apenas o .exe: mantenha DLLs, runtimes, res/, main.lua e aegis.toml juntos.\n" +
        "Se fechar, leia crash.log.\n");
    var zipPath = Path.Combine(distRoot, packageName + "-" + target + ".zip");
    if (File.Exists(zipPath)) File.Delete(zipPath);
    ZipFile.CreateFromDirectory(outDir, zipPath, CompressionLevel.SmallestSize, includeBaseDirectory: false);

    Info($"build pronto: {outDir}");
    Info($"zip pronto: {zipPath}");
    return 0;
}

static int PublishItch(string[] args)
{
    var itch = ArgValue(args, "--itch");
    if (string.IsNullOrWhiteSpace(itch) || !itch.Contains('/'))
        return Fail("use: aegis publish --itch user/jogo [--target win-x64]");

    var target = NormalizeTarget(ArgValue(args, "--target") ?? "win-x64");
    var buildCode = Build(new[] { "build", PositionalAfterCommand(args) ?? ".", "--target", target });
    if (buildCode != 0) return buildCode;

    var gameArg = PositionalAfterCommand(args) ?? ".";
    var gameDir = Path.GetFullPath(gameArg);
    var cfg = ReadToml(Path.Combine(gameDir, "aegis.toml"));
    var packageName = SanitizeName(cfg.title == "Aegis Game" ? new DirectoryInfo(gameDir).Name : cfg.title);
    var repoRoot = FindRepoRoot(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();
    var outDir = Path.Combine(repoRoot, "dist", packageName + "-" + target);
    var channel = ChannelForTarget(target);

    var code = RunProcess("butler", $"push \"{outDir}\" {itch}:{channel}", repoRoot);
    if (code != 0)
        return Fail("butler falhou. Instale e faça login: butler login");

    Info($"publicado no itch: {itch}:{channel}");
    return 0;
}

static int RunGame(string gameDirArg)
{
    var fullGameDir = Path.GetFullPath(gameDirArg);

    // Em build exportado/atalho, o CurrentDirectory pode não ser a pasta do .exe.
    // Então tentamos também a pasta real do executável.
    if (!File.Exists(Path.Combine(fullGameDir, "main.lua")))
    {
        var exeDir = AppContext.BaseDirectory;
        if (File.Exists(Path.Combine(exeDir, "main.lua")))
            fullGameDir = exeDir;
    }

    var entry = Path.Combine(fullGameDir, "main.lua");
    var toml = Path.Combine(fullGameDir, "aegis.toml");
    if (!Directory.Exists(fullGameDir)) return Fail($"Pasta do jogo não encontrada: {fullGameDir}");
    if (!File.Exists(entry)) return Fail($"main.lua não encontrado em: {fullGameDir}");

    try
    {
        Directory.SetCurrentDirectory(fullGameDir);
        var cfg = ReadToml(toml);
        new App(cfg.title, cfg.width, cfg.height).Run(entry);
        return 0;
    }
    catch (Exception ex)
    {
        var log = Path.Combine(fullGameDir, "crash.log");
        File.WriteAllText(log,
            "Aegis crash\n" +
            $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
            $"GameDir: {fullGameDir}\n" +
            $"Entry: {entry}\n\n" +
            ex + "\n");
        Console.Error.WriteLine("[aegis] O jogo fechou com erro.");
        Console.Error.WriteLine($"[aegis] Log: {log}");
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}
if (args.Length == 0)
{
    // Build exportado: abre se main.lua estiver na pasta atual ou na pasta real do executável.
    if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "main.lua")))
        return RunGame(".");
    if (File.Exists(Path.Combine(AppContext.BaseDirectory, "main.lua")))
        return RunGame(AppContext.BaseDirectory);
    PrintHelp();
    return 0;
}

if (args[0] is "--help" or "-h") { PrintHelp(); return 0; }
var cmd = args[0].ToLowerInvariant();

if (cmd == "doctor") return Doctor();
if (cmd == "update") return UpdateSelf();
if (cmd == "build") return Build(args);
if (cmd == "publish") return PublishItch(args);

if (cmd == "run")
{
    var gameDir = args.Length >= 2 ? args[1] : ".";
    return RunGame(gameDir);
}

if (cmd == "new")
{
    if (args.Length < 2) return Fail("Use: aegis new <nome>  ou  aegis new platformer|topdown|puzzle <nome>");

    var templateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "platformer", "topdown", "puzzle" };
    var template = templateNames.Contains(args[1]) ? args[1] : "basic";
    var projectName = template == "basic" ? args[1] : (args.Length >= 3 ? args[2] : "meu-jogo");
    var dir = Path.GetFullPath(projectName);

    if (Directory.Exists(dir) && Directory.EnumerateFileSystemEntries(dir).Any())
        return Fail($"A pasta já existe e não está vazia: {dir}");

    if (template != "basic")
    {
        var repoRoot = FindRepoRoot(Directory.GetCurrentDirectory()) ?? FindRepoRoot(AppContext.BaseDirectory);
        var source = repoRoot is null ? null : Path.Combine(repoRoot, "templates", template);
        if (source is null || !Directory.Exists(source))
            return Fail($"Template '{template}' não encontrado. Verifique a pasta templates/{template}.");
        CopyGameFiles(source, dir);
        Console.WriteLine($"Projeto {template} criado: {dir}");
        return 0;
    }

    var resDir = Path.Combine(dir, "res");
    Directory.CreateDirectory(resDir);
    CopyOfficialLogo(resDir);
    File.WriteAllText(Path.Combine(dir, "aegis.toml"), "title = \"Aegis Game\"\nwidth = 1280\nheight = 720\n");
    File.WriteAllText(Path.Combine(dir, "main.lua"), "function aegis_init()\n    aegis.log(\"Aegis iniciado\")\n    local logo = aegis.newSprite(\"aegis-logo.png\")\n    aegis.setPosition(logo, 640, 360)\n    aegis.setPivot(logo, 0.5, 0.5)\nend\n\nfunction aegis_update(dt) end\nfunction aegis_draw() end\n");
    Console.WriteLine($"Projeto criado: {dir}");
    return 0;
}

return Fail($"Comando desconhecido: {args[0]}");
