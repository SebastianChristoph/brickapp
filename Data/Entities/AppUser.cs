namespace brickapp.Data.Entities;

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
    // Navigation: MappingRequests, die dieser User angelegt hat
    public ICollection<MappingRequest> MappingRequestsRequested { get; set; } = new List<MappingRequest>();
    // Navigation: MappingRequests, die dieser User genehmigt hat
    public ICollection<MappingRequest> MappingRequestsApproved { get; set; } = new List<MappingRequest>();
    // Navigation: UserNotifications
    public ICollection<UserNotification> Notifications { get; set; } = new List<UserNotification>();

    // Navigation: NewItemRequests, die dieser User angelegt hat
    public ICollection<NewItemRequest> NewItemRequestsRequested { get; set; } = new List<NewItemRequest>();
    // Navigation: NewItemRequests, die dieser User genehmigt hat
    public ICollection<NewItemRequest> NewItemRequestsApproved { get; set; } = new List<NewItemRequest>();


    // Navigation: Favorite Sets
    public ICollection<UserSetFavorite> FavoriteSets { get; set; } = new List<UserSetFavorite>();

    // Brickets: Fictive currency for user achievements
    public int Brickets { get; set; } = 0;

}
