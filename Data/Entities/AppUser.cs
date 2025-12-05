namespace brickisbrickapp.Data.Entities;

public class AppUser
{
    public int Id { get; set; }              // interne DB-ID
    public string Uuid { get; set; } = default!;  // öffentliche UUID (z.B. Guid als String)

    public string? Name { get; set; }
    public bool IsAdmin { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation: alle Inventory-Items dieses Users
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public ICollection<UserItemSet> UserItemSets { get; set; } = new List<UserItemSet>();

}
