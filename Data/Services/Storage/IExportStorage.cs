namespace brickapp.Data.Services.Storage;
public interface IExportStorage
{
    Task WriteTextAsync(string relativePath, string contentType, string content);
    Task<string?> ReadTextAsync(string relativePath);
    Task<bool> ExistsAsync(string relativePath);
    string DescribeTarget(string relativePath);
}
