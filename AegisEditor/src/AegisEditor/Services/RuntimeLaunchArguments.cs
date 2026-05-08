namespace AegisEditor.Services;

public sealed record RuntimeLaunchArguments(
    string ExecutablePath,
    string WorkingDirectory,
    string? Arguments);
