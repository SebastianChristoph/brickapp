// Components/Shared/PartsListUpload/ParsedPart.cs
namespace brickapp.Components.Shared.PartsListUpload;

public record ParsedPart(int MappedBrickId, int BrickColorId, int Quantity, string? ExternalPartNum);
