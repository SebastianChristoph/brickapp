using Microsoft.AspNetCore.Hosting;

namespace brickapp.Data.Services.Storage;

public class LocalImageStorage : IImageStorage
{
    private readonly string _basePath;

    public LocalImageStorage(string basePath)
    {
        _basePath = basePath;
    }

    public string BaseUrl => "/"; // Lokale Basis-URL ist einfach "/"

    public async Task<string> SaveAsync(Stream imageStream, string contentType, string relativePath)
    {
        var fullPath = Path.Combine(_basePath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var fileStream = new FileStream(fullPath, FileMode.Create);
        await imageStream.CopyToAsync(fileStream);

        return $"{BaseUrl}{relativePath}";
    }

    public bool Exists(string relativePath)
    {
        var fullPath = Path.Combine(_basePath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        return File.Exists(fullPath);
    }
    public Task<bool> DeleteAsync(string relativePath)
{
    var fullPath = Path.Combine(_basePath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
    if (File.Exists(fullPath))
    {
        File.Delete(fullPath);
        return Task.FromResult(true);
    }
    return Task.FromResult(false);
}

    public Task<bool> CopyAsync(string sourceRelativePath, string targetRelativePath)
    {
        var sourcePath = Path.Combine(_basePath, sourceRelativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        var targetPath = Path.Combine(_basePath, targetRelativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        
        if (!File.Exists(sourcePath))
            return Task.FromResult(false);

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Copy(sourcePath, targetPath, overwrite: true);
        return Task.FromResult(true);
    }
}
