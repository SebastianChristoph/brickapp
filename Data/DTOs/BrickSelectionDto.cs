using brickapp.Data.Entities;

namespace brickapp.Data.DTOs;

public class BrickSelectionDto
{
    public MappedBrick Brick { get; set; } = default!;
    public int BrickColorId { get; set; }
    public int Quantity { get; set; }
    public string Brand { get; set; } = "Lego";
}
