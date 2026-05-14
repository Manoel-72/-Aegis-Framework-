namespace AegisEditor.Services;

public sealed record RuntimeLaunchArguments(
    string ExecutablePath,
    string WorkingDirectory,
    string? Arguments = null,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null,
    IReadOnlyList<string>? ArgumentList = null);
