namespace brickapp.Data.Services.PartsListUpload;

public record RawRow(
    string PartNum,
    int ColorIdFromFile,
    int Quantity,
    string ColorMode
);
