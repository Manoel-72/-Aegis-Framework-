namespace AegisEditor.Services;

public interface IRuntimeLauncher
{
    event EventHandler? RuntimeExited;

    bool IsRuntimeRunning { get; }

    Task<int> LaunchAsync(RuntimeLaunchArguments args, CancellationToken cancellationToken = default);

    /// <summary>Inicia um processo e devolve-no imediatamente (não faz wait). O cliente continua a correr em janela própria.</summary>
    bool TryStartDetached(RuntimeLaunchArguments args, out string? errorMessage);

    bool TryStopRuntime(out string? errorMessage);
}
