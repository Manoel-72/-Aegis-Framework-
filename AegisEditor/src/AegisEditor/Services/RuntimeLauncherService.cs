using System.Diagnostics;
using System.IO;

namespace AegisEditor.Services;

public sealed class RuntimeLauncherService(IEditorLogSink log) : IRuntimeLauncher
{
    private readonly object _gate = new();
    private Process? _runtimeProcess;

    public event EventHandler? RuntimeExited;

    public bool IsRuntimeRunning
    {
        get
        {
            lock (_gate)
                return _runtimeProcess is { HasExited: false };
        }
    }

    public Task<int> LaunchAsync(RuntimeLaunchArguments args, CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = CreateProcessStartInfo(args);

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

    public bool TryStartDetached(RuntimeLaunchArguments args, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            lock (_gate)
            {
                if (_runtimeProcess is { HasExited: false })
                {
                    log.Post(EditorLogLevel.Info, $"Runtime ja esta rodando (PID {_runtimeProcess.Id}); nao vou abrir outra janela.");
                    return true;
                }

                if (_runtimeProcess is not null)
                {
                    try { _runtimeProcess.Dispose(); }
                    catch { /* ignore */ }
                    _runtimeProcess = null;
                }
            }

            var psi = CreateProcessStartInfo(args);
            // dotnet no PATH: convém UseShellExecute para o Windows resolver o host.
            // Exe local (aegis-cli): UseShellExecute=false + ArgumentList costuma ser mais estável.
            var file = psi.FileName;
            var localExe = Path.IsPathRooted(file) && File.Exists(file);
            psi.UseShellExecute = !localExe;

            var proc = Process.Start(psi);
            if (proc is null)
            {
                errorMessage = "Process.Start returned null.";
                log.Post(EditorLogLevel.Error, errorMessage);
                return false;
            }

            proc.EnableRaisingEvents = true;
            proc.Exited += (_, _) =>
            {
                try
                {
                    log.Post(
                        proc.ExitCode == 0 ? EditorLogLevel.Info : EditorLogLevel.Warning,
                        $"Processo do runtime terminou (código {proc.ExitCode}).");
                }
                catch
                {
                    /* ignore */
                }
                finally
                {
                    lock (_gate)
                    {
                        if (ReferenceEquals(_runtimeProcess, proc))
                            _runtimeProcess = null;
                    }

                    try { proc.Dispose(); }
                    catch { /* ignore */ }
                    RuntimeExited?.Invoke(this, EventArgs.Empty);
                }
            };

            lock (_gate)
                _runtimeProcess = proc;

            _ = proc.Id;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            log.Post(EditorLogLevel.Error, $"Start detached failed: {ex.Message}");
            return false;
        }
    }

    public bool TryStopRuntime(out string? errorMessage)
    {
        errorMessage = null;
        Process? proc;
        lock (_gate)
        {
            proc = _runtimeProcess;
            if (proc is null || proc.HasExited)
            {
                _runtimeProcess = null;
                return true;
            }
        }

        try
        {
            if (proc.CloseMainWindow())
            {
                if (!proc.WaitForExit(2500))
                    proc.Kill(entireProcessTree: true);
            }
            else
                proc.Kill(entireProcessTree: true);

            lock (_gate)
            {
                if (ReferenceEquals(_runtimeProcess, proc))
                    _runtimeProcess = null;
            }

            log.Post(EditorLogLevel.Info, "Runtime parado pelo editor.");
            RuntimeExited?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            log.Post(EditorLogLevel.Warning, $"Falha ao parar runtime: {ex.Message}");
            return false;
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo(RuntimeLaunchArguments args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = args.ExecutablePath,
            WorkingDirectory = args.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        if (args.ArgumentList is { Count: > 0 })
        {
            foreach (var a in args.ArgumentList)
                psi.ArgumentList.Add(a);
        }
        else
        {
            psi.Arguments = args.Arguments ?? string.Empty;
        }

        if (args.EnvironmentVariables is { Count: > 0 })
        {
            foreach (var kv in args.EnvironmentVariables)
                psi.Environment[kv.Key] = kv.Value ?? string.Empty;
        }

        return psi;
    }
}
