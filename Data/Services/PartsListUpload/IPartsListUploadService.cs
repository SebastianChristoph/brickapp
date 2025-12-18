using System.Threading.Tasks;
using brickapp.Components.Shared.PartsListUpload;

namespace brickapp.Data.Services.PartsListUpload;

public interface IPartsListUploadService
{
    Task<ParseResult<ParsedPart>> ParseAsync(string content, PartsUploadFormat format);
}
