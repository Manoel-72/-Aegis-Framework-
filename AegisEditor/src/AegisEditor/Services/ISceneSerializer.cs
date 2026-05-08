using AegisEditor.Shared.Models;

namespace AegisEditor.Services;

public interface ISceneSerializer
{
    Task<SceneState> LoadAsync(string fullPath, CancellationToken cancellationToken = default);

    Task SaveAsync(string fullPath, SceneState state, CancellationToken cancellationToken = default);
}
