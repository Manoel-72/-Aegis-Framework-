using Aegis.Core;
using Aegis.Scene;

namespace Aegis.Scripting;

public sealed partial class LuaRuntime
{
    public int LoadScene(string path, bool clear = true)
    {
        var result = LoadSceneJson(path, clear);
        AegisLog.Info("Scene", $"scene.json carregada: {result.Name} ({result.EntityCount} entidade(s))");
        return result.EntityCount;
    }

    public SceneJsonLoadResult LoadSceneJson(string path, bool clear = true)
    {
        var full = ResolveGameFile(path, ".scene.json", ".json");

        if (clear)
            ClearAll();

        var loader = new SceneJsonLoader();
        return loader.Load(full, _app.S2D);
    }

    private string ResolveGameFile(string relativePath, params string[] allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("[Aegis|Scene] Caminho de cena vazio.", nameof(relativePath));

        var root = Path.GetFullPath(_gameRoot);
        var safePath = relativePath.Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar, '/');
        var full = Path.GetFullPath(Path.Combine(root, safePath));

        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"[Aegis|Scene] Arquivo fora da pasta do jogo: '{relativePath}'");

        if (allowedExtensions.Length > 0)
        {
            var ext = Path.GetExtension(full);
            var ok = allowedExtensions.Any(e => ext.Equals(e, StringComparison.OrdinalIgnoreCase));
            if (!ok)
                throw new InvalidOperationException($"[Aegis|Scene] Extensao nao suportada para '{relativePath}'.");
        }

        if (!File.Exists(full))
            throw new FileNotFoundException($"[Aegis|Scene] Arquivo nao encontrado: '{full}'");

        return full;
    }
}
