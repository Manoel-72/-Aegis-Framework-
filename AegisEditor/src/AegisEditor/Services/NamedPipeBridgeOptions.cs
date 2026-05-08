namespace AegisEditor.Services;

public sealed record NamedPipeBridgeOptions(
    string PipeName,
    TimeSpan ConnectTimeout,
    string NewLine = "\n")
{
    public static NamedPipeBridgeOptions Default { get; }
        = new("AegisEditorRuntime", TimeSpan.FromSeconds(5));
}
