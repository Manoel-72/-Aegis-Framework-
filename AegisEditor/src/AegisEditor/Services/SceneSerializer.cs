using System.Text.Json;
using AegisEditor.Shared.Messages;
using AegisEditor.Shared.Models;

namespace AegisEditor.Services;

public sealed class SceneSerializer : ISceneSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public async Task<SceneState> LoadAsync(string fullPath, CancellationToken cancellationToken = default)
    {
        await using var fs = File.OpenRead(fullPath);
        var state = await JsonSerializer.DeserializeAsync<SceneState>(fs, Options, cancellationToken)
                    .ConfigureAwait(false);
        return Normalize(state ?? new SceneState(), Path.GetFileNameWithoutExtension(fullPath));
    }

    public async Task SaveAsync(string fullPath, SceneState state, CancellationToken cancellationToken = default)
    {
        var normalizedPath = Path.GetFullPath(fullPath);
        Directory.CreateDirectory(Path.GetDirectoryName(normalizedPath)!);

        var tempPath = normalizedPath + ".tmp";
        await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(fs, Normalize(state, Path.GetFileNameWithoutExtension(fullPath)), Options, cancellationToken)
                .ConfigureAwait(false);
            await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        if (File.Exists(normalizedPath))
            File.Replace(tempPath, normalizedPath, null);
        else
            File.Move(tempPath, normalizedPath);
    }

    private static SceneState Normalize(SceneState state, string? fallbackName)
    {
        state.Format = string.IsNullOrWhiteSpace(state.Format) ? "aegis.scene" : state.Format.Trim();
        state.Version = SceneState.CurrentVersion;
        state.Name = string.IsNullOrWhiteSpace(state.Name) ? fallbackName ?? "Scene" : state.Name.Trim();
        state.Kind = string.IsNullOrWhiteSpace(state.Kind) ? "2d" : state.Kind.Trim().ToLowerInvariant();
        state.Width = state.Width <= 0 ? 1280 : state.Width;
        state.Height = state.Height <= 0 ? 720 : state.Height;
        state.Entities ??= [];
        state.Tilemaps ??= [];
        return state;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = IpcSerializerOptions.Create();
        options.WriteIndented = true;
        return options;
    }
}
