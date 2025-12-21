using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace brickapp.Data.Services.Storage;

public class AzureBlobImageStorage : IImageStorage
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public AzureBlobImageStorage(string connectionString, string containerName = "brickapp")
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerName = containerName;
    }

    public string BaseUrl => $"https://{_blobServiceClient.Uri.Host}/{_containerName}/";

    public async Task<string> SaveAsync(Stream imageStream, string contentType, string relativePath)
    {
        var blobClient = _blobServiceClient.GetBlobContainerClient(_containerName).GetBlobClient(relativePath);
        await blobClient.UploadAsync(imageStream, new BlobHttpHeaders { ContentType = contentType });

        return blobClient.Uri.ToString(); // Liefert die volle URL zurück
    }

    public bool Exists(string relativePath)
    {
        var blobClient = _blobServiceClient.GetBlobContainerClient(_containerName).GetBlobClient(relativePath);
        return blobClient.Exists().Value;
    }
    public async Task<bool> DeleteAsync(string relativePath)
{
    var blobClient = _blobServiceClient.GetBlobContainerClient(_containerName).GetBlobClient(relativePath);
    return await blobClient.DeleteIfExistsAsync();
}

    public async Task<bool> CopyAsync(string sourceRelativePath, string targetRelativePath)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var sourceBlobClient = containerClient.GetBlobClient(sourceRelativePath);
        var targetBlobClient = containerClient.GetBlobClient(targetRelativePath);

        if (!await sourceBlobClient.ExistsAsync())
            return false;

        await targetBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
        return true;
    }
}
