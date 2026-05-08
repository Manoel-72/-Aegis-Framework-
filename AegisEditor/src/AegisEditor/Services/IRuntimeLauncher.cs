namespace AegisEditor.Services;

public interface IRuntimeLauncher
{
    Task<int> LaunchAsync(RuntimeLaunchArguments args, CancellationToken cancellationToken = default);
}
