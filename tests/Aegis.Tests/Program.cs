using Aegis.CLI;
using Aegis.Core;
using Aegis.Resource;
using Aegis.Scene;
using Aegis.Scripting.Components;
using Aegis.Systems;
using Aegis.World;
using NLua;
using System.Diagnostics;

namespace Aegis.Tests;

internal static class Program
{
    private static readonly List<string> Failures = [];

    public static int Main()
    {
        Run("ConfigManager sanitizes resolution and display mode", ConfigManagerSanitizesConfig);
        Run("ConfigManager writes and loads aegis.cfg", ConfigManagerPersistsConfig);
        Run("ComponentFactory creates group on world and UI roots", ComponentFactoryCreatesGroups);
        Run("FontManager resolves project fallback font candidates", FontManagerResolvesProjectFallbackFont);
        Run("FontManager normalizes font size", FontManagerNormalizesFontSize);
        Run("AssetValidator validates project assets", AssetValidatorValidatesProjectAssets);
        Run("AssetValidator reports missing references", AssetValidatorReportsMissingReferences);
        Run("ProjectCreator creates a runnable project skeleton", ProjectCreatorCreatesSkeleton);
        Run("SceneManager transition none loads registered scene", SceneManagerTransitionLoadsScene);
        Run("CLI build web creates validated package", CliBuildWebCreatesPackage);

        if (Failures.Count == 0)
        {
            Console.WriteLine("[Aegis.Tests] OK");
            return 0;
        }

        Console.Error.WriteLine("[Aegis.Tests] FAILED");
        foreach (var failure in Failures)
            Console.Error.WriteLine("- " + failure);
        return 1;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine("[PASS] " + name);
        }
        catch (Exception ex)
        {
            Failures.Add($"{name}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void ConfigManagerSanitizesConfig()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "aegis.cfg"), """
{
  "windowWidth": 10,
  "windowHeight": 99999,
  "displayMode": "fullscreen",
  "fullscreen": false,
  "masterVolume": 5
}
""");

        ConfigManager.Initialize(dir.Path, 1280, 720);

        AssertEqual(320, ConfigManager.Current.windowWidth, "windowWidth should be clamped");
        AssertEqual(4320, ConfigManager.Current.windowHeight, "windowHeight should be clamped");
        AssertEqual("borderless", ConfigManager.Current.displayMode, "fullscreen alias should normalize to borderless");
        AssertTrue(ConfigManager.Current.fullscreen, "borderless mode should set fullscreen compatibility flag");
        AssertEqual(1f, ConfigManager.Current.masterVolume, "masterVolume should be clamped");
    }

    private static void ConfigManagerPersistsConfig()
    {
        using var dir = new TempDir();
        ConfigManager.Initialize(dir.Path, 800, 600);
        ConfigManager.SetDisplayMode("window");
        ConfigManager.SetResolution(1440, 900);

        var text = File.ReadAllText(Path.Combine(dir.Path, "aegis.cfg"));
        AssertContains(text, "\"windowWidth\": 1440", "width should be saved");
        AssertContains(text, "\"windowHeight\": 900", "height should be saved");
        AssertContains(text, "\"displayMode\": \"windowed\"", "display mode should be saved normalized");
    }

    private static void ComponentFactoryCreatesGroups()
    {
        var app = new App("Tests", 640, 480)
        {
            S2D = new Scene2D(),
            Ui2D = new Scene2D(),
        };
        var factory = new ComponentFactory(app);

        using var lua = new Lua();
        lua.DoString("opts = { x = 12, y = 34, z = 7, alpha = 0.5, scale = 2 }");
        var worldObj = factory.Create("group", (LuaTable)lua["opts"]);

        AssertTrue(ReferenceEquals(app.S2D, worldObj.Parent), "default group should be parented to world root");
        AssertEqual(12f, worldObj.X, "x should be applied");
        AssertEqual(34f, worldObj.Y, "y should be applied");
        AssertEqual(7, worldObj.Z, "z should be applied");
        AssertEqual(0.5f, worldObj.Alpha, "alpha should be applied");
        AssertEqual(2f, worldObj.ScaleX, "scaleX should use scale fallback");
        AssertEqual(2f, worldObj.ScaleY, "scaleY should use scale fallback");

        lua.DoString("ui_opts = { layer = 'ui' }");
        var uiObj = factory.Create("group", (LuaTable)lua["ui_opts"]);
        AssertTrue(ReferenceEquals(app.Ui2D, uiObj.Parent), "ui group should be parented to UI root");
    }

    private static void FontManagerResolvesProjectFallbackFont()
    {
        using var dir = new TempDir();
        var previousRoot = FontManager.FontRoot;
        try
        {
            var fontRoot = Path.Combine(dir.Path, "res", "fonts");
            Directory.CreateDirectory(fontRoot);
            var expected = Path.Combine(fontRoot, "Inter-Regular.ttf");
            File.WriteAllBytes(expected, [0, 1, 2, 3]);

            FontManager.FontRoot = fontRoot;
            var resolved = FontManager.ResolveDefaultFontPathForTests();

            AssertEqual(Path.GetFullPath(expected), Path.GetFullPath(resolved), "default font should prefer Inter-Regular.ttf from project font root");
        }
        finally
        {
            FontManager.FontRoot = previousRoot;
        }
    }

    private static void FontManagerNormalizesFontSize()
    {
        AssertEqual(8, FontManager.NormalizeSizeForTests(1), "font size should clamp to minimum");
        AssertEqual(24, FontManager.NormalizeSizeForTests(24), "font size should preserve valid value");
        AssertEqual(96, FontManager.NormalizeSizeForTests(999), "font size should clamp to maximum");
    }

    private static void AssetValidatorValidatesProjectAssets()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "main.lua"), """
function aegis_init()
    aegis.playSound("jump.wav")
end
""");
        File.WriteAllText(Path.Combine(dir.Path, "aegis.toml"), "entry = \"main.lua\"\n");
        File.WriteAllText(Path.Combine(dir.Path, "aegis.cfg"), "{}\n");
        Directory.CreateDirectory(Path.Combine(dir.Path, "res", "audio"));
        WriteMinimalWav(Path.Combine(dir.Path, "res", "audio", "jump.wav"));

        var report = AssetValidator.ValidateProject(dir.Path);

        AssertEqual(0, report.ErrorCount, "valid project should have no asset errors");
    }

    private static void AssetValidatorReportsMissingReferences()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "main.lua"), """
function aegis_init()
    aegis.playSound("missing.wav")
    aegis.create("sprite", { path = "sprites/player.png" })
end
""");
        File.WriteAllText(Path.Combine(dir.Path, "aegis.toml"), "entry = \"main.lua\"\n");
        Directory.CreateDirectory(Path.Combine(dir.Path, "res"));

        var report = AssetValidator.ValidateProject(dir.Path);

        AssertTrue(report.ErrorCount >= 2, "missing audio and sprite should be reported");
        AssertTrue(report.Issues.Any(i => i.Code == "asset.reference.missing"), "missing references should use asset.reference.missing code");
    }

    private static void ProjectCreatorCreatesSkeleton()
    {
        using var dir = new TempDir();
        var previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(dir.Path);
            ProjectCreator.Create("sample-game");

            var root = Path.Combine(dir.Path, "sample-game");
            AssertTrue(Directory.Exists(root), "project root should exist");
            AssertTrue(File.Exists(Path.Combine(root, "main.lua")), "main.lua should exist");
            AssertTrue(File.Exists(Path.Combine(root, "aegis.toml")), "aegis.toml should exist");
            AssertTrue(File.Exists(Path.Combine(root, "res", "sprites", "player.png")), "player sprite should exist");
            AssertContains(File.ReadAllText(Path.Combine(root, "aegis.toml")), "entry  = \"main.lua\"", "entry should point to main.lua");
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }

    private static void SceneManagerTransitionLoadsScene()
    {
        using var dir = new TempDir();
        var previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(dir.Path);
            Directory.CreateDirectory(Path.Combine(dir.Path, "scenes"));
            File.WriteAllText(Path.Combine(dir.Path, "scenes", "menu.lua"), """
function aegis_init()
end

function scene_loaded_marker()
end
""");

            var app = new App("Scene Test", 640, 480)
            {
                S2D = new Scene2D(),
                Ui2D = new Scene2D(),
            };
            using var lua = new Aegis.Scripting.LuaRuntime(app);
            lua.RegisterAll();

            SceneManager.Instance.RegisterScene("menu", "scenes/menu.lua");
            SceneManager.Instance.TransitionTo("menu", "none", 0.01f);

            AssertTrue(lua.HasFunction("scene_loaded_marker"), "transition should load the registered Lua scene immediately");
            AssertTrue(!SceneManager.Instance.IsTransitioning, "none transition should finish immediately");
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }

    private static void CliBuildWebCreatesPackage()
    {
        using var dir = new TempDir();
        var repoRoot = FindRepoRoot();
        var gameRoot = Path.Combine(dir.Path, "web-game");
        Directory.CreateDirectory(gameRoot);
        File.WriteAllText(Path.Combine(gameRoot, "main.lua"), "function aegis_init() end\n");
        File.WriteAllText(Path.Combine(gameRoot, "aegis.toml"), """
[game]
title = "test-web-build"
width = 640
height = 480
entry = "main.lua"
""");

        var exitCode = RunProcess(
            "dotnet",
            $"run --no-restore --project \"{Path.Combine(repoRoot, "src", "Aegis.CLI", "Aegis.CLI.csproj")}\" -- build \"{gameRoot}\" --target web",
            repoRoot);

        AssertEqual(0, exitCode, "web build command should succeed");

        var outDir = Path.Combine(repoRoot, "dist", "test-web-build-web");
        AssertTrue(Directory.Exists(outDir), "web build output directory should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "index.html")), "web build should create index.html");
        AssertTrue(File.Exists(Path.Combine(outDir, "game", "main.lua")), "web build should copy main.lua");
        AssertTrue(File.Exists(Path.Combine(repoRoot, "dist", "test-web-build-web.zip")), "web build should create zip");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message}. Expected '{expected}', got '{actual}'.");
    }

    private static void AssertContains(string haystack, string needle, string message)
    {
        if (!haystack.Contains(needle, StringComparison.Ordinal))
            throw new InvalidOperationException($"{message}. Missing '{needle}'.");
    }

    private static int RunProcess(string fileName, string arguments, string workingDirectory)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        });
        if (process is null) return 1;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(stdout)) Console.WriteLine(stdout.TrimEnd());
        if (!string.IsNullOrWhiteSpace(stderr)) Console.Error.WriteLine(stderr.TrimEnd());
        return process.ExitCode;
    }

    private static void WriteMinimalWav(string path)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        const short channels = 1;
        const int sampleRate = 8000;
        const short bitsPerSample = 16;
        const short blockAlign = channels * bitsPerSample / 8;
        const int byteRate = sampleRate * blockAlign;
        var sampleCount = 80;
        var dataSize = sampleCount * blockAlign;

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);
        for (var i = 0; i < sampleCount; i++)
            writer.Write((short)0);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Aegis.sln"))
                && File.Exists(Path.Combine(current.FullName, "src", "Aegis.CLI", "Aegis.CLI.csproj")))
                return current.FullName;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Aegis repository root.");
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aegis-tests-" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // Best effort cleanup; tests should not fail because temp cleanup was denied.
            }
        }
    }
}
