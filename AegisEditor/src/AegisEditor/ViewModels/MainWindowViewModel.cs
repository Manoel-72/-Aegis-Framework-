using System.Text.Json;
using AegisEditor.Services;
using AegisEditor.Shared.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AegisEditor.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IEditorBridgeClient _bridge;
    private readonly IEditorLogSink _log;
    private readonly ISceneSerializer _sceneSerializer;

    public HierarchyViewModel Hierarchy { get; }

    public InspectorViewModel Inspector { get; }

    public ViewportViewModel Viewport { get; }

    public LuaEditorViewModel LuaEditor { get; }

    public ConsoleViewModel ConsoleLog { get; }

    public MainWindowViewModel(
        HierarchyViewModel hierarchy,
        InspectorViewModel inspector,
        ViewportViewModel viewport,
        LuaEditorViewModel luaEditor,
        ConsoleViewModel consoleLog,
        IEditorBridgeClient bridge,
        IEditorLogSink log,
        ISceneSerializer sceneSerializer)
    {
        Hierarchy = hierarchy;
        Inspector = inspector;
        Viewport = viewport;
        LuaEditor = luaEditor;
        ConsoleLog = consoleLog;
        _bridge = bridge;
        _log = log;
        _sceneSerializer = sceneSerializer;

        Hierarchy.SelectedEntityChanged += (_, entity) =>
            Inspector.ApplySelection(entity);

        bridge.MessageReceived += OnRuntimeInbound;
    }

    [ObservableProperty]
    private string _sceneProjectPath = string.Empty;

    [RelayCommand]
    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _bridge.ConnectAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _log.Post(EditorLogLevel.Error, $"Connect failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _bridge.DisconnectAsync(cancellationToken).ConfigureAwait(true);
            _log.Post(EditorLogLevel.Info, "Disconnected from runtime pipe.");
        }
        catch (Exception ex)
        {
            _log.Post(EditorLogLevel.Warning, $"Disconnect failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadSceneAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SceneProjectPath))
        {
            _log.Post(EditorLogLevel.Warning, "Set a scene JSON path before loading.");
            return;
        }

        try
        {
            var normalized = SceneProjectPath.Trim();
            var state = await _sceneSerializer.LoadAsync(normalized, cancellationToken).ConfigureAwait(true);

            Hierarchy.ApplySceneState(state);
            Viewport.ApplySceneState(state);

            if (_bridge.IsConnected)
            {
                var relPath = normalized.Replace('\\', '/');
                await _bridge.SendLineAsync(EditorCommand.SceneLoadLine(relPath), cancellationToken)
                    .ConfigureAwait(true);
                _log.Post(EditorLogLevel.Info, $"SCENE_LOAD sent: {relPath}");
            }
            else
                _log.Post(EditorLogLevel.Warning, "Scene loaded locally; runtime not connected (SCENE_LOAD not sent).");
        }
        catch (Exception ex)
        {
            _log.Post(EditorLogLevel.Error, $"Load scene failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PlayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SendWhenConnected(EditorCommand.PlayLine(), cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _log.Post(EditorLogLevel.Error, ex.Message);
        }
    }

    [RelayCommand]
    private async Task PauseAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SendWhenConnected(EditorCommand.PauseLine(), cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _log.Post(EditorLogLevel.Error, ex.Message);
        }
    }

    [RelayCommand]
    private async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SendWhenConnected(EditorCommand.StopLine(), cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _log.Post(EditorLogLevel.Error, ex.Message);
        }
    }

    [RelayCommand]
    private async Task HotReloadLuaAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SendWhenConnected(
                EditorCommand.HotReloadLine("scripts/player.lua"),
                cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _log.Post(EditorLogLevel.Error, ex.Message);
        }
    }

    private async Task SendWhenConnected(string line, CancellationToken cancellationToken)
    {
        if (!_bridge.IsConnected)
        {
            _log.Post(EditorLogLevel.Warning, "Not connected.");
            return;
        }

        await _bridge.SendLineAsync(line, cancellationToken).ConfigureAwait(true);
    }

    private void OnRuntimeInbound(object? sender, RuntimeInboundEnvelope e)
    {
        try
        {
            var opts = IpcSerializerOptions.Create();
            switch (e.Type)
            {
                case RuntimeEvent.SceneState:
                    var scene = JsonSerializer.Deserialize<SceneStatePayload>(e.Payload, opts);
                    if (scene is not null)
                    {
                        Hierarchy.ApplyScenePayload(scene);
                        Viewport.ApplyScenePayload(scene);
                    }

                    break;

                case RuntimeEvent.EntityUpdated:
                    var eu = JsonSerializer.Deserialize<EntityUpdatedPayload>(e.Payload, opts);
                    if (eu is not null)
                    {
                        foreach (var entity in Hierarchy.Entities)
                        {
                            if (entity.Id != eu.Id) continue;
                            entity.X = eu.X;
                            entity.Y = eu.Y;
                            break;
                        }

                        foreach (var entity in Viewport.Entities)
                        {
                            if (entity.Id != eu.Id) continue;
                            entity.X = eu.X;
                            entity.Y = eu.Y;
                            break;
                        }

                        Viewport.NotifyRedraw();
                    }

                    break;

                case RuntimeEvent.Log:
                    var logPayload = JsonSerializer.Deserialize<LogPayload>(e.Payload, opts);
                    if (logPayload is null) break;

                    var level = logPayload.Level.Trim().Equals("warn", StringComparison.OrdinalIgnoreCase)
                        ? EditorLogLevel.Warning
                        : logPayload.Level.Trim().Equals("error", StringComparison.OrdinalIgnoreCase)
                            ? EditorLogLevel.Error
                            : EditorLogLevel.Info;
                    _log.Post(level, logPayload.Message);
                    break;

                case RuntimeEvent.Connected:
                    var connected = JsonSerializer.Deserialize<ConnectedPayload>(e.Payload, opts);
                    if (connected is not null)
                        _log.Post(EditorLogLevel.Info, $"CONNECTED runtime {connected.RuntimeVersion}");

                    break;

                case RuntimeEvent.Error:
                    var err = JsonSerializer.Deserialize<ErrorPayload>(e.Payload, opts);
                    if (err is not null)
                        _log.Post(EditorLogLevel.Error, err.Message);
                    break;
            }
        }
        catch (Exception ex)
        {
            _log.Post(EditorLogLevel.Error, $"IPC parse failure ({e.Type}): {ex.Message}");
        }
    }
}
