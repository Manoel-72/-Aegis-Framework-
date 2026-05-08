using AegisEditor.Shared.Messages;

namespace AegisEditor.Services;

public interface IEditorBridgeClient
{
    bool IsConnected { get; }

    event EventHandler<RuntimeInboundEnvelope>? MessageReceived;

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task SendLineAsync(string newlineDelimitedJsonLine, CancellationToken cancellationToken = default);
}
