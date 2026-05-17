using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aegis.Audio;
using Aegis.Core;
using Aegis.Display;
using Aegis.Scene;
using Aegis.Scripting;
using Aegis.World;
using AegisEditor.Shared.Messages;
using AegisEditor.Shared.Models;
using Microsoft.Xna.Framework;

namespace Aegis.Editor;

/// <summary>
/// Servidor JSON-lines via named pipe compatível com o cliente do Aegis Editor.
/// Ativar: variável <c>AEGIS_EDITOR_PIPE=1</c> ou nome do pipe explicitamente.
/// Pipe padrão (igual ao editor): <see cref="DefaultPipe"/>.
/// </summary>
public sealed class EditorPipeHost : IDisposable
{
    public const string DefaultPipe = "AegisEditorRuntime";

    public static EditorPipeHost Instance { get; } = new();

    private static readonly JsonSerializerOptions JsonOpts = IpcSerializerOptions.Create();

    private readonly ConcurrentQueue<string> _inboundLines = new();

    private readonly ConcurrentQueue<string> _outboundLines = new();

    private readonly object _writeGate = new();

    private readonly Dictionary<Object2D, string> _objectToEditorId = new();

    private readonly Dictionary<string, Object2D> _editorIdToObject = new(StringComparer.Ordinal);

    private string? _pipeName;

    private CancellationTokenSource? _cts;

    private Task? _ioTask;

    private StreamWriter? _writer;

    private int _started;

    private int _needsGreeting;

    private volatile bool _shutdown;

    private float _scenePushTimer;

    private AegisGame? _game;

    private App? _app;

    private LuaRuntime? _lua;

    internal static volatile bool SimulationPausedByEditor;

    private EditorPipeHost()
    {
    }

    internal void Attach(AegisGame game, App app)
    {
        _game = game;
        _app = app;
        _lua = app.Lua;
    }

    internal void TryStartFromEnvironment()
    {
        var name = ResolvePipeFromEnvironment();
        if (name is null || _shutdown) return;

        if (Interlocked.Exchange(ref _started, 1) != 0) return;

        _pipeName = name;
        AegisLog.EchoToEditorIpc += MirrorLogToOutboundQueue;

        EmitLogBuffered("info",
            $"EDITOR_PIPE '{_pipeName}' wd={NormalizePath(Environment.CurrentDirectory)} audioRoot={AudioManager.AudioRoot}");

        _cts = new CancellationTokenSource();
        _ioTask = Task.Run(() => IoLoopAsync(_cts.Token));

        EmitLogBuffered("info", "[EditorPipe] esperando cliente (editor)…");
    }

    internal static string? ResolvePipeFromEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable("AEGIS_EDITOR_PIPE");
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim();
        if (raw.Equals("1", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("true", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("yes", StringComparison.OrdinalIgnoreCase))
            return DefaultPipe;
        return raw;
    }

    internal void PumpAndFlush(TimeSpan elapsed)
    {
        if (_started == 0 || _shutdown) return;

        while (_inboundLines.TryDequeue(out var line))
            ProcessInboundLine(line);

        if (Interlocked.CompareExchange(ref _needsGreeting, 0, 1) == 1)
            EmitConnectedAndSceneBuffered();

        _scenePushTimer += (float)elapsed.TotalSeconds;
        if (_scenePushTimer >= 0.48f && IsWriterReadyUnsafe())
        {
            _scenePushTimer = 0f;
            EnqueueSceneState();
        }

        FlushOutboundUnsafe();
    }

    private bool IsWriterReadyUnsafe()
    {
        lock (_writeGate)
            return _writer is not null;
    }

    private void MirrorLogToOutboundQueue(string level, string message)
    {
        try
        {
            if (_started == 0 || _shutdown || !IsWriterReadyUnsafe())
                return;

            var lp = new LogPayload { Level = level, Message = message };
            _outboundLines.Enqueue(SerializeEnvelope(RuntimeEvent.Log, lp));
        }
        catch
        {
            /* ignore IPC mirror failures */
        }
    }

    private async Task IoLoopAsync(CancellationToken ct)
    {
        if (_pipeName is null) return;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await using (server.ConfigureAwait(false))
                {
                    await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
                    StreamWriter writer;
                    lock (_writeGate)
                    {
                        writer = new StreamWriter(server, Encoding.UTF8, leaveOpen: true) { AutoFlush = false };
                        _writer = writer;
                    }

                    Interlocked.Exchange(ref _needsGreeting, 1);

                    using var reader = new StreamReader(server, Encoding.UTF8, leaveOpen: true);

                    while (!ct.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                        if (line is null) break;
                        _inboundLines.Enqueue(line);
                    }

                    lock (_writeGate)
                    {
                        if (ReferenceEquals(_writer, writer))
                            _writer = null;

                        writer.Dispose();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            /* shutdown */
        }
        catch (Exception ex)
        {
            AegisLog.Exception("EditorPipe", ex);
        }

        lock (_writeGate)
        {
            try { _writer?.Dispose(); }
            catch { /* ignore */ }
            finally { _writer = null; }
        }
    }

    private void FlushOutboundUnsafe()
    {
        lock (_writeGate)
        {
            if (_writer is null) return;
            try
            {
                while (_outboundLines.TryDequeue(out var line))
                {
                    _writer.Write(line.AsSpan());
                    if (!line.EndsWith('\n'))
                        _writer.Write('\n');

                    _writer.Flush();
                }
            }
            catch (Exception ex)
            {
                try { AegisLog.Warn("EditorPipe", $"Escrita pipe: {ex.Message}"); }
                catch { /* ignore */ }

                try { _writer.Dispose(); }
                catch { /* ignore */ }
                finally { _writer = null; }
            }
        }
    }

    private void ProcessInboundLine(string line)
    {
        if (_game is null || _app is null || _lua is null || _shutdown) return;

        Inbound? cmd;
        try
        {
            cmd = JsonSerializer.Deserialize<Inbound>(line, JsonOpts);
            if (cmd is null || string.IsNullOrWhiteSpace(cmd.Type)) return;
        }
        catch (Exception ex)
        {
            EmitErrorBuffered(ex.Message);
            return;
        }

        try
        {
            switch (cmd.Type)
            {
                case EditorCommand.SceneLoad:
                {
                    var path = PayloadString(cmd.Payload, "path");
                    if (!string.IsNullOrEmpty(path))
                    {
                        _lua.LoadSceneFile(path.Replace('\\', '/').Trim());
                        EmitLogBuffered("info", $"SCENE_LOAD ok: {path}");
                        EnqueueSceneState();
                    }

                    break;
                }
                case EditorCommand.Play:
                    SimulationPausedByEditor = false;
                    EmitLogBuffered("info", "PLAY");
                    break;
                case EditorCommand.Pause:
                    SimulationPausedByEditor = true;
                    EmitLogBuffered("info", "PAUSE");
                    break;
                case EditorCommand.Stop:
                    EmitLogBuffered("info", "STOP → exit");
                    _game.Exit();
                    break;

                case EditorCommand.HotReload:
                {
                    var file = PayloadString(cmd.Payload, "file");
                    if (!string.IsNullOrEmpty(file))
                        _lua.EditorHotReloadFile(file.Replace('\\', '/').Trim());

                    EmitLogBuffered("info", $"HOT_RELOAD {file}");
                    EnqueueSceneState();
                    break;
                }

                case EditorCommand.EntityMove:
                {
                    var id = PayloadString(cmd.Payload, "id");
                    var wx = PayloadDouble(cmd.Payload, "x");
                    var wy = PayloadDouble(cmd.Payload, "y");
                    if (!string.IsNullOrEmpty(id)
                        && _editorIdToObject.TryGetValue(id, out var objForMove)
                        && wx is not double.NaN
                        && wy is not double.NaN
                        && float.IsFinite((float)wx)
                        && float.IsFinite((float)wy))
                        MoveToWorldXY(objForMove, (float)wx, (float)wy);

                    if (!string.IsNullOrEmpty(id) && _editorIdToObject.TryGetValue(id, out var tracked))
                        EnqueueEntityUpdated(id, tracked);

                    break;
                }
                case EditorCommand.EntityScale:
                {
                    var id = PayloadString(cmd.Payload, "id");
                    var sx = PayloadDouble(cmd.Payload, "sx");
                    var sy = PayloadDouble(cmd.Payload, "sy");
                    if (!string.IsNullOrEmpty(id) && _editorIdToObject.TryGetValue(id, out var obj))
                    {
                        if (sx is not double.NaN && float.IsFinite((float)sx))
                            obj.ScaleX = (float)sx;

                        if (sy is not double.NaN && float.IsFinite((float)sy))
                            obj.ScaleY = (float)sy;

                        EnqueueEntityUpdated(id, obj);
                    }

                    break;
                }
                case EditorCommand.EntitySelect:
                case EditorCommand.EntitySpawn:
                case EditorCommand.EntityDelete:
                case EditorCommand.PropSet:
                    EmitLogBuffered("info", $"{cmd.Type}: stub");
                    break;
                default:
                    EmitLogBuffered("info", $"{cmd.Type}: ignorado");
                    break;
            }
        }
        catch (Exception ex)
        {
            AegisLog.Exception("EditorPipe", ex);
            EmitErrorBuffered(ex.Message);
        }
    }

    private static void MoveToWorldXY(Object2D obj, float worldX, float worldY)
    {
        var cur = obj.WorldPosition;
        var dWx = worldX - cur.X;
        var dWy = worldY - cur.Y;
        if (MathF.Abs(dWx) + MathF.Abs(dWy) < 1e-5f) return;
        ApplyWorldTranslationDelta(obj, dWx, dWy);
    }

    private static void ApplyWorldTranslationDelta(Object2D obj, float dWx, float dWy)
    {
        var parent = obj.Parent;
        if (parent is null || parent is Scene2D)
        {
            obj.X += dWx;
            obj.Y += dWy;
            return;
        }

        var pwm = parent.GetWorldMatrix();
        Matrix.Invert(ref pwm, out var invPw);
        var dl = Vector2.TransformNormal(new Vector2(dWx, dWy), invPw);
        obj.X += dl.X;
        obj.Y += dl.Y;
    }

    private void EnqueueEntityUpdated(string id, Object2D obj)
    {
        var wp = obj.WorldPosition;
        _outboundLines.Enqueue(SerializeEnvelope(RuntimeEvent.EntityUpdated,
            new EntityUpdatedPayload { Id = id, X = wp.X, Y = wp.Y }));
    }

    private void EnqueueSceneState()
    {
        if (_app?.S2D is null || !IsWriterReadyUnsafe()) return;

        var payload = CollectScenePayload();
        _outboundLines.Enqueue(SerializeEnvelope(RuntimeEvent.SceneState, payload));
    }

    /// <summary>Reúne entidades sob <see cref="App.S2D"/> para o editor.</summary>
    private SceneStatePayload CollectScenePayload()
    {
        var entities = new List<SceneEntityDto>();
        var tilemaps = new List<TilemapDto>();
        _objectToEditorId.Clear();
        _editorIdToObject.Clear();

        var counter = 0;
        var preorder = new List<Object2D>();

        void VisitSubtree(Object2D obj)
        {
            var id = "e" + counter.ToString(CultureInfo.InvariantCulture);
            counter++;
            _objectToEditorId[obj] = id;
            _editorIdToObject[id] = obj;
            preorder.Add(obj);

            foreach (var c in obj.Children.OrderBy(child => child.Z))
                VisitSubtree(c);
        }

        foreach (var root in _app!.S2D.Children.OrderBy(c => c.Z))
            VisitSubtree(root);

        foreach (var obj in preorder)
        {
            string? parentId = null;
            if (obj.Parent is not null
                && !ReferenceEquals(obj.Parent, _app.S2D)
                && _objectToEditorId.TryGetValue(obj.Parent, out var pid))
                parentId = pid;

            var wp = obj.WorldPosition;
            var childrenIds = obj.Children
                .OrderBy(c => c.Z)
                .Select(c => _objectToEditorId.GetValueOrDefault(c, string.Empty))
                .Where(s => s.Length > 0)
                .ToList();

            entities.Add(new SceneEntityDto
            {
                Id = _objectToEditorId[obj],
                Name = obj.GetType().Name,
                Type = obj.GetType().Name,
                X = wp.X,
                Y = wp.Y,
                ScaleX = obj.ScaleX,
                ScaleY = obj.ScaleY,
                Rotation = obj.Rotation,
                TexturePath = (obj as Bitmap)?.Texture?.Name?.Replace('\\', '/'),
                Components = [],
                Children = childrenIds,
                ParentId = parentId,
            });

            if (obj is TilemapNode tm && !string.IsNullOrWhiteSpace(tm.TiledSourcePath))
            {
                tilemaps.Add(new TilemapDto
                {
                    Id = _objectToEditorId[obj],
                    Name = nameof(TilemapNode),
                    TiledJsonPath = tm.TiledSourcePath,
                    X = wp.X,
                    Y = wp.Y,
                });
            }
        }

        return new SceneStatePayload { Entities = entities, Tilemaps = tilemaps.Count > 0 ? tilemaps : null };
    }

    private sealed class Inbound
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("payload")]
        public JsonElement Payload { get; set; }
    }

    private sealed class OutEnvelope
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("payload")]
        public required object Payload { get; init; }
    }

    private static string SerializeEnvelope(string type, object payload)
        => JsonSerializer.Serialize(new OutEnvelope { Type = type, Payload = payload }, JsonOpts) + "\n";

    private static string PayloadString(JsonElement payload, string name)
        => payload.TryGetProperty(name, out var p)
            ? p.GetString()?.Trim() ?? string.Empty
            : string.Empty;

    private static double PayloadDouble(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var p)) return double.NaN;
        return p.ValueKind switch
        {
            JsonValueKind.Number => p.TryGetDouble(out var x) ? x : double.NaN,
            JsonValueKind.String =>
                double.TryParse(p.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s)
                    ? s
                    : double.NaN,
            _ => double.NaN,
        };
    }

    private void EmitConnectedAndSceneBuffered()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly().GetName();
            var v = asm.Version?.ToString() ?? "0.98.0";
            var conn = new ConnectedPayload { RuntimeVersion = "Aegis " + v };

            _outboundLines.Enqueue(SerializeEnvelope(RuntimeEvent.Connected, conn));
            EnqueueSceneState();
        }
        catch
        {
            /* ignore */
        }
    }

    private void EmitLogBuffered(string level, string message)
    {
        _outboundLines.Enqueue(SerializeEnvelope(RuntimeEvent.Log, new LogPayload { Level = level, Message = message }));
    }

    private void EmitErrorBuffered(string msg)
        => _outboundLines.Enqueue(SerializeEnvelope(RuntimeEvent.Error, new ErrorPayload { Message = msg }));

    private static string NormalizePath(string path)
        => path.Replace('\\', '/');

    internal void Shutdown()
    {
        _shutdown = true;
        try { AegisLog.EchoToEditorIpc -= MirrorLogToOutboundQueue; }
        catch { /* ignore */ }

        try { _cts?.Cancel(); }
        catch { /* ignore */ }

        try { _ioTask?.Wait(TimeSpan.FromSeconds(2)); }
        catch { /* ignore */ }

        lock (_writeGate)
        {
            try { _writer?.Dispose(); }
            catch { /* ignore */ }
            finally { _writer = null; }
        }

        try { _cts?.Dispose(); }
        catch { /* ignore */ }
        finally { _cts = null; }
    }

    public void Dispose() => Shutdown();
}
