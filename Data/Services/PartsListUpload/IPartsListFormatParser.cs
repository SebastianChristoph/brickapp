using System.Collections.Generic;
using brickapp.Components.Shared.PartsListUpload;

namespace brickapp.Data.Services.PartsListUpload;

public interface IPartsListFormatParser
{
    PartsUploadFormat Format { get; }

    (List<RawRow> Rows, List<InvalidRow> InvalidRows, HashSet<int> InvalidColorIds)
        Parse(string content);
}
