namespace brickisbrickapp.Data.Entities;

public class BrickColor
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    public int RebrickableColorId { get; set; }   // colors.csv id
    public string? Rgb { get; set; }

    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
