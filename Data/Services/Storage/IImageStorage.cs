namespace Services.Storage;

public interface IImageStorage
{
    string BaseUrl { get; } 
    
    Task<string> SaveAsync(
        Stream stream,
        string contentType,
        string relativePath
    );

    bool Exists(string relativePath);
}
