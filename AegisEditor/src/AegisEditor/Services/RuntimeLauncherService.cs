using System.Diagnostics;

namespace AegisEditor.Services;

public sealed class RuntimeLauncherService(IEditorLogSink log) : IRuntimeLauncher
{
    public Task<int> LaunchAsync(RuntimeLaunchArguments args, CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = args.ExecutablePath,
                WorkingDirectory = args.WorkingDirectory,
                Arguments = args.Arguments ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = false,
            };

            using var proc = Process.Start(psi);
            if (proc is null)
            {
                log.Post(EditorLogLevel.Error, "Process.Start returned null.");
                return Task.FromResult(-1);
            }

            return proc.WaitForExitAsync(cancellationToken).ContinueWith(_ => proc.ExitCode, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            log.Post(EditorLogLevel.Error, $"Launch failed: {ex.Message}");
            return Task.FromResult(-1);
        }
    }
}
