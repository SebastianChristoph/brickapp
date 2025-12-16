using Microsoft.AspNetCore.Hosting;

namespace Services.Storage;

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
}
