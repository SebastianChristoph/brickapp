using brickapp.Data.Entities;

namespace brickapp.Data.DTOs;

public class BrickSelectionDto
{
    public MappedBrick Brick { get; init; } = null!;
    public int BrickColorId { get; init; }
    public int Quantity { get; init; }
    public string Brand { get; init; } = "Lego";
}
