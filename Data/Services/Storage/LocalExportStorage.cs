namespace Services.Storage;

public class LocalExportStorage : IExportStorage
{
    private readonly string _baseDir;

    public LocalExportStorage(string baseDir)
    {
        _baseDir = baseDir;
    }

    private string FullPath(string relativePath)
        => Path.Combine(_baseDir, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

    public async Task WriteTextAsync(string relativePath, string contentType, string content)
    {
        var full = FullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllTextAsync(full, content);
    }

    public async Task<string?> ReadTextAsync(string relativePath)
    {
        var full = FullPath(relativePath);
        if (!File.Exists(full)) return null;
        return await File.ReadAllTextAsync(full);
    }

    public Task<bool> ExistsAsync(string relativePath)
        => Task.FromResult(File.Exists(FullPath(relativePath)));

    public string DescribeTarget(string relativePath) => FullPath(relativePath);
}
