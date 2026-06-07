namespace AegisEditor.ViewModels;

public sealed class AssetProblemViewModel
{
    public required string Severity { get; init; }

    public required string Message { get; init; }

    public string Path { get; init; } = string.Empty;
}
