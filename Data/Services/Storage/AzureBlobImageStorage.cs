using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
namespace Services.Storage;

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
}
