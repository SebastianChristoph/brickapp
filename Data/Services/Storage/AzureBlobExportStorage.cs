using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Services.Storage;

public class AzureBlobExportStorage : IExportStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobExportStorage(string connectionString, string containerName)
    {
        _container = new BlobContainerClient(connectionString, containerName);
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task WriteTextAsync(string relativePath, string contentType, string content)
    {
        var blob = _container.GetBlobClient(relativePath.Replace("\\", "/"));
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        await blob.UploadAsync(ms, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });
    }

    public async Task<string?> ReadTextAsync(string relativePath)
    {
        var blob = _container.GetBlobClient(relativePath.Replace("\\", "/"));
        if (!await blob.ExistsAsync()) return null;
        var download = await blob.DownloadContentAsync();
        return download.Value.Content.ToString();
    }

    public async Task<bool> ExistsAsync(string relativePath)
    {
        var blob = _container.GetBlobClient(relativePath.Replace("\\", "/"));
        return await blob.ExistsAsync();
    }

    public string DescribeTarget(string relativePath)
        => $"{_container.Uri}/{relativePath.Replace("\\", "/")}";
}
