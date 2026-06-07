using System.Text.Json;
using Aegis.Core;
using AegisEditor.Shared.Models;

namespace Aegis.Scene;

/// <summary>
/// Instancia cenas salvas pelo Aegis Editor no formato .scene.json.
/// A primeira versão é propositalmente pequena: entidades 2D, sprites e grupos.
/// </summary>
public sealed class SceneJsonLoader
{
    private readonly SceneEntityFactory _factory;

    public SceneJsonLoader()
        : this(new SceneEntityFactory())
    {
    }

    public SceneJsonLoader(SceneEntityFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public SceneJsonLoadResult Load(string fullPath, Scene2D worldRoot)
    {
        ArgumentNullException.ThrowIfNull(worldRoot);

        if (string.IsNullOrWhiteSpace(fullPath))
            throw new ArgumentException("[Aegis|Scene] Caminho de cena vazio.", nameof(fullPath));

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"[Aegis|Scene] Cena nao encontrada: '{fullPath}'");

        using var stream = File.OpenRead(fullPath);
        var state = JsonSerializer.Deserialize<SceneState>(stream, Options)
            ?? throw new InvalidOperationException($"[Aegis|Scene] Cena invalida: '{fullPath}'");

        state = ValidateAndMigrateScene(state, fullPath);

        var created = new Dictionary<string, Object2D>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in state.Entities)
        {
            var parent = ResolveParent(worldRoot, created, entity.ParentId);
            var obj = _factory.Create(entity, parent);

            if (!string.IsNullOrWhiteSpace(entity.Id))
                created[entity.Id] = obj;
        }

        return new SceneJsonLoadResult(state.Name, state.Entities.Count, created.Values.ToArray());
    }

    private static SceneState ValidateAndMigrateScene(SceneState state, string fullPath)
    {
        if (!state.Format.Equals("aegis.scene", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"[Aegis|Scene] Formato de cena nao suportado em '{fullPath}': {state.Format}");

        if (state.Version is < 1 or > SceneState.CurrentVersion)
            throw new InvalidOperationException($"[Aegis|Scene] Versao de cena nao suportada em '{fullPath}': {state.Version}");

        state.Format = "aegis.scene";
        state.Version = SceneState.CurrentVersion;
        state.Kind = string.IsNullOrWhiteSpace(state.Kind) ? "2d" : state.Kind.Trim().ToLowerInvariant();
        state.Name = string.IsNullOrWhiteSpace(state.Name) ? Path.GetFileNameWithoutExtension(fullPath) : state.Name.Trim();
        state.Width = state.Width <= 0 ? 1280 : state.Width;
        state.Height = state.Height <= 0 ? 720 : state.Height;
        state.Entities ??= [];
        state.Tilemaps ??= [];
        foreach (var entity in state.Entities)
            entity.Components ??= [];

        return state;
    }

    private static Object2D ResolveParent(Scene2D root, IReadOnlyDictionary<string, Object2D> created, string? parentId)
    {
        if (string.IsNullOrWhiteSpace(parentId))
            return root;

        return created.TryGetValue(parentId, out var parent) ? parent : root;
    }

}

public sealed record SceneJsonLoadResult(string Name, int EntityCount, IReadOnlyList<Object2D> Objects);
