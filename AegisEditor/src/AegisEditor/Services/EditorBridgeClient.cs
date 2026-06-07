using System.IO.Pipes;
using System.Text;
using AegisEditor.Shared.Messages;

namespace AegisEditor.Services;

/// <summary>
/// Cliente JSON-lines via Named Pipe para o runtime Aegis (lado servidor ainda por implementar).
/// </summary>
public sealed class EditorBridgeClient : IDisposable, IEditorBridgeClient
{
    private readonly NamedPipeBridgeOptions _options;
    private readonly IUiThreadScheduler _ui;
    private readonly IEditorLogSink _log;

    private CancellationTokenSource? _readLoopCts;
    private Task? _readLoop;
    private NamedPipeClientStream? _pipe;
    private StreamWriter? _writer;
    private StreamReader? _reader;
    private SemaphoreSlim _sendLock = new(1, 1);

    public EditorBridgeClient(
        NamedPipeBridgeOptions options,
        IUiThreadScheduler ui,
        IEditorLogSink log)
    {
        _options = options;
        _ui = ui;
        _log = log;
    }

    public bool IsConnected => _pipe?.IsConnected ?? false;

    public event EventHandler<RuntimeInboundEnvelope>? MessageReceived;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await DisconnectAsync(cancellationToken).ConfigureAwait(false);

        var pipe = new NamedPipeClientStream(
            serverName: ".",
            pipeName: _options.PipeName,
            direction: PipeDirection.InOut,
            options: PipeOptions.Asynchronous);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.ConnectTimeout);
            await pipe.ConnectAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.Post(EditorLogLevel.Warning, $"Pipe connect failed: {ex.Message}");
            await pipe.DisposeAsync().ConfigureAwait(false);
            return;
        }

        pipe.ReadMode = PipeTransmissionMode.Byte;

        var writer = new StreamWriter(pipe, Encoding.UTF8) { AutoFlush = false };
        var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: false);

        _pipe = pipe;
        _writer = writer;
        _reader = reader;

        _readLoopCts = new CancellationTokenSource();
        _readLoop = Task.Run(() => ReadLoopAsync(_readLoopCts.Token), CancellationToken.None);

        _log.Post(EditorLogLevel.Info, "Connected to runtime pipe.");
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_readLoopCts is not null)
        {
            try { _readLoopCts.Cancel(); }
            catch { /* ignore */ }

            if (_readLoop is not null)
            {
                try { await _readLoop.ConfigureAwait(false); }
                catch (OperationCanceledException) { /* expected */ }
                catch (Exception ex)
                {
                    _log.Post(EditorLogLevel.Warning, $"Read loop end: {ex.Message}");
                }
            }

            _readLoopCts.Dispose();
            _readLoopCts = null;
            _readLoop = null;
        }

        if (_writer is not null)
        {
            await _writer.DisposeAsync().ConfigureAwait(false);
            _writer = null;
        }

        _reader = null;

        if (_pipe is not null)
        {
            await _pipe.DisposeAsync().ConfigureAwait(false);
            _pipe = null;
        }
    }

    public async Task SendLineAsync(string newlineDelimitedJsonLine, CancellationToken cancellationToken = default)
    {
        if (_writer is null || _pipe is null || !_pipe.IsConnected)
        {
            _log.Post(EditorLogLevel.Warning, "Cannot send: pipe not connected.");
            return;
        }

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _writer.WriteAsync(newlineDelimitedJsonLine.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (!newlineDelimitedJsonLine.EndsWith(_options.NewLine, StringComparison.Ordinal))
                await _writer.WriteAsync(_options.NewLine.AsMemory(), cancellationToken).ConfigureAwait(false);
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        var reader = _reader;
        if (reader is null) return;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null) break;

                var env = RuntimeEvent.TryParseEnvelope(line);
                if (env is null) continue;

                _ui.Post(() => MessageReceived?.Invoke(this, env));
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            _log.Post(EditorLogLevel.Error, $"Pipe read error: {ex.Message}");
        }
        finally
        {
            var wasUnexpectedClose = !ct.IsCancellationRequested;

            try { _writer?.Dispose(); }
            catch { /* ignore */ }

            _writer = null;
            _reader = null;

            if (_pipe is not null)
            {
                try { _pipe.Dispose(); }
                catch { /* ignore */ }
                _pipe = null;
            }

            if (wasUnexpectedClose)
                _log.Post(EditorLogLevel.Warning, "Runtime desconectado; a janela do jogo foi fechada ou o processo terminou.");
        }
    }

    public void Dispose()
    {
        DisconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
        _sendLock.Dispose();
    }
}
