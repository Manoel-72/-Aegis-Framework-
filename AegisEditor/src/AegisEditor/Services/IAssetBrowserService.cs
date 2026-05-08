namespace AegisEditor.Services;

public interface IAssetBrowserService
{
    Task<IReadOnlyList<AssetEntry>> ListAsync(string directoryPath, CancellationToken cancellationToken = default);
}
