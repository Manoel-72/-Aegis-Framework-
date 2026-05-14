using System.IO;
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
    private readonly IRuntimeLauncher _runtimeLauncher;

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
        ISceneSerializer sceneSerializer,
        IRuntimeLauncher runtimeLauncher)
    {
        Hierarchy = hierarchy;
        Inspector = inspector;
        Viewport = viewport;
        LuaEditor = luaEditor;
        ConsoleLog = consoleLog;
        _bridge = bridge;
        _log = log;
        _sceneSerializer = sceneSerializer;
        _runtimeLauncher = runtimeLauncher;

        Hierarchy.SelectedEntityChanged += (_, entity) =>
            Inspector.ApplySelection(entity);

        bridge.MessageReceived += OnRuntimeInbound;

        var detectedRepo = AegisEngineLocator.FindRepoRootContainingCli();
        var demoNear = detectedRepo is not null ? AegisEngineLocator.DefaultDemoNearRepo(detectedRepo) : null;
        if (demoNear is not null)
            GamePreviewFolder = demoNear;
    }

    [ObservableProperty]
    private string _sceneProjectPath = string.Empty;

    /// <summary>Pasta do jogo (onde está main.lua); preenchido com demo-platformer se existir ao lado da engine.</summary>
    [ObservableProperty]
    private string _gamePreviewFolder = string.Empty;

    /// <summary>Raiz do repositório com <c>src/Aegis.CLI</c>; deixar vazio para detetar a partir da pasta do editor.</summary>
    [ObservableProperty]
    private string _engineRepoOverride = string.Empty;

    [RelayCommand]
    private async Task RunGameAndConnectAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(GamePreviewFolder))
        {
            _log.Post(EditorLogLevel.Warning, "Define a pasta do jogo (ex.: demo-platformer) antes de Correr.");
            return;
        }

        var repo = !string.IsNullOrWhiteSpace(EngineRepoOverride)
            ? Path.GetFullPath(EngineRepoOverride.Trim())
            : AegisEngineLocator.FindRepoRootContainingCli();

        if (repo is null || !Directory.Exists(repo))
        {
            _log.Post(EditorLogLevel.Error,
                "Não encontrei a raíz da engine (ficheiro src/Aegis.CLI/Aegis.CLI.csproj). Define «raíz motor» manualmente.");
            return;
        }

        var csproj = Path.Combine(repo, "src", "Aegis.CLI", "Aegis.CLI.csproj");
        if (!File.Exists(csproj))
        {
            _log.Post(EditorLogLevel.Error,
                $"CLI não encontrado: {csproj}. Ajuste a pasta «raíz motor».");
            return;
        }

        var gameDir = Path.GetFullPath(GamePreviewFolder.Trim());
        if (!Directory.Exists(gameDir))
        {
            _log.Post(EditorLogLevel.Error, $"Pasta do jogo não existe: {gameDir}");
            return;
        }

        if (!File.Exists(Path.Combine(gameDir, "main.lua")))
        {
            _log.Post(EditorLogLevel.Error,
                $"Sem main.lua em {gameDir}. Escolha a pasta certa do jogo.");
            return;
        }

        try
        {
            await _bridge.DisconnectAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _log.Post(EditorLogLevel.Warning, $"Disconnect antes do run: {ex.Message}");
        }

        var prebuiltCli = AegisEngineLocator.FindPrebuiltAegisCliExecutable(repo);

        string? errMsg;
        bool start;
        if (prebuiltCli is not null)
        {
            _log.Post(EditorLogLevel.Info,
                $"A arrancar aegis-cli compilado ({Path.GetFileName(prebuiltCli)}); WD = pasta do jogo.");

            start = _runtimeLauncher.TryStartDetached(
                new RuntimeLaunchArguments(
                    ExecutablePath: prebuiltCli,
                    WorkingDirectory: gameDir,
                    ArgumentList: ["run", gameDir, "--editor-pipe"]),
                out errMsg);
        }
        else
        {
            _log.Post(EditorLogLevel.Warning,
                "Sem exe em src/Aegis.CLI/bin/*/net8.0. Executa primeiro: dotnet build src/Aegis.CLI. Alternativa: dotnet run desde o utilizador.");

            start = _runtimeLauncher.TryStartDetached(
                new RuntimeLaunchArguments(
                    ExecutablePath: "dotnet",
                    WorkingDirectory: repo,
                    ArgumentList:
                    [
                        "run",
                        "--project",
                        csproj,
                        "--",
                        "run",
                        gameDir,
                        "--editor-pipe",
                    ]),
                out errMsg);
        }

        if (!start)
        {
            _log.Post(EditorLogLevel.Error, errMsg ?? "Falha ao iniciar o processo do jogo.");
            return;
        }

        _log.Post(EditorLogLevel.Info,
            "Aguardando o runtime (se fechar já, aparece código de saída no consola abaixo).");

        var warmUpMs = prebuiltCli is not null ? 2000 : 5000;
        await Task.Delay(warmUpMs, cancellationToken).ConfigureAwait(true);

        for (var attempt = 0; attempt < 24; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            try
            {
                await _bridge.ConnectAsync(cancellationToken).ConfigureAwait(true);
                if (_bridge.IsConnected)
                {
                    _log.Post(EditorLogLevel.Info, $"Ligado ao runtime ({attempt + 1} tentativa(s)).");
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Post(EditorLogLevel.Warning, $"Connect tentativa {attempt + 1}: {ex.Message}");
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(true);
        }

        _log.Post(EditorLogLevel.Warning,
            "Não ligou ao pipe. Confirma que o jogo continua aberto; se terminou, vê a linha «Processo do runtime terminou» acima.");
    }

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
