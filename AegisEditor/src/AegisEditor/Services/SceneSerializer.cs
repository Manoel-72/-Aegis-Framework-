using System.Text.Json;
using AegisEditor.Shared.Messages;
using AegisEditor.Shared.Models;

namespace AegisEditor.Services;

public sealed class SceneSerializer : ISceneSerializer
{
    private static readonly JsonSerializerOptions Options = IpcSerializerOptions.Create();

    public async Task<SceneState> LoadAsync(string fullPath, CancellationToken cancellationToken = default)
    {
        await using var fs = File.OpenRead(fullPath);
        var state = await JsonSerializer.DeserializeAsync<SceneState>(fs, Options, cancellationToken)
                    .ConfigureAwait(false);
        return state ?? new SceneState();
    }

    public async Task SaveAsync(string fullPath, SceneState state, CancellationToken cancellationToken = default)
    {
        await using var fs = File.Create(fullPath);
        await JsonSerializer.SerializeAsync(fs, state, Options, cancellationToken).ConfigureAwait(false);
    }
}
