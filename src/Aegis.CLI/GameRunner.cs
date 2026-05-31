using Aegis.Core;

namespace Aegis.CLI;

public static class GameRunner
{
    /// <summary>
    /// Se <paramref name="firstArg"/> for uma pasta existente (ex.: <c>examples/physics-lab</c>),
    /// muda o cwd para ela antes de ler <c>aegis.toml</c>. Retorna <c>true</c> se entrou
    /// na pasta (o argumento não é um override de script Lua).
    /// </summary>
    private static bool TryEnterGameDirectory(string? firstArg)
    {
        if (string.IsNullOrWhiteSpace(firstArg)) return false;
        // Não tratar como pasta se parecer ficheiro Lua
        if (firstArg.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)) return false;

        var path = Path.GetFullPath(firstArg.Trim());
        if (!Directory.Exists(path)) return false;

        Directory.SetCurrentDirectory(path);
        Console.WriteLine($"[Aegis] cwd → {path}");
        return true;
    }

    public static void Run(string? scriptOverride)
    {
        if (TryEnterGameDirectory(scriptOverride))
            scriptOverride = null;

        string title  = "Aegis Game";
        int    width  = 1280;
        int    height = 720;
        string entry  = "main.lua";

        if (File.Exists("aegis.toml"))
        {
            foreach (var line in File.ReadAllLines("aegis.toml"))
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;
                var k = parts[0].Trim();
                var v = parts[1].Trim().Trim('"');
                switch (k)
                {
                    case "title":  title  = v; break;
                    case "width":  int.TryParse(v, out width);  break;
                    case "height": int.TryParse(v, out height); break;
                    case "entry":  entry  = v; break;
                }
            }
        }

        var script = scriptOverride ?? entry;

        if (!File.Exists(script))
        {
            Console.WriteLine($"[Aegis] Script não encontrado: '{script}'");
            Console.WriteLine("[Aegis] Rode 'aegis new <nome>' numa pasta de jogo, ou:");
            Console.WriteLine("  aegis run examples/physics-lab   |   aegis run examples/hyper-casual");
            if (File.Exists(Path.Combine("src", "Aegis.CLI", "Aegis.CLI.csproj")))
                Console.WriteLine("  Na raiz do engine: .\\aegis.cmd run examples/physics-lab  (garante cwd + build certos)");
            return;
        }

        Console.WriteLine($"[Aegis] Iniciando '{title}'  {width}x{height}  →  {script}");
        new App(title, width, height).Run(script);
    }
}
