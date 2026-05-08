namespace AegisEditor.Services;

public sealed class AssetBrowserService(IEditorLogSink log) : IAssetBrowserService
{
    public Task<IReadOnlyList<AssetEntry>> ListAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var di = new DirectoryInfo(directoryPath);
            if (!di.Exists)
            {
                log.Post(EditorLogLevel.Warning, $"Directory not found: {directoryPath}");
                return Task.FromResult<IReadOnlyList<AssetEntry>>(Array.Empty<AssetEntry>());
            }

            var list = new List<AssetEntry>();
            foreach (var sub in di.GetDirectories())
                list.Add(new AssetEntry(sub.Name, sub.FullName, IsDirectory: true));

            foreach (var file in di.GetFiles())
                list.Add(new AssetEntry(file.Name, file.FullName, IsDirectory: false));

            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyList<AssetEntry>>(list);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.Post(EditorLogLevel.Error, $"Asset list failed: {ex.Message}");
            return Task.FromResult<IReadOnlyList<AssetEntry>>(Array.Empty<AssetEntry>());
        }
    }
}
