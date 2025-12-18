namespace brickapp.Data.Entities;

public class ItemSetBrick
{
    public int Id { get; set; }

    public int ItemSetId { get; set; }
    public ItemSet ItemSet { get; set; } = default!;

    public int MappedBrickId { get; set; }
    public MappedBrick MappedBrick { get; set; } = default!;

     // 🔥 NEU: Farbe des Steins im Set
    public int BrickColorId { get; set; }
    public BrickColor BrickColor { get; set; } = default!;

    public int Quantity { get; set; }
}
