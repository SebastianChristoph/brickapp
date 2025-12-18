namespace brickapp.Data.Entities;

public class InventoryItem
{
    public int Id { get; set; }

    // FK auf den Stein
    public int MappedBrickId { get; set; }
    public MappedBrick MappedBrick { get; set; } = default!;

    // FK auf die Farbe
    public int BrickColorId { get; set; }
    public BrickColor BrickColor { get; set; } = default!;

    // FK auf den User
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = default!;

    // Von welcher Marke ist dieses konkrete Teil (Lego, Cada, BlueBrixx, ...)
    public string Brand { get; set; } = default!;

    public int Quantity { get; set; }
}
