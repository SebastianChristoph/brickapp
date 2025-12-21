namespace brickapp.Data.Entities;

public class TrackingInfo
{
    public int Id { get; set; }

    public string? UserUuid { get; set; }  // User Token/UUID, nullable for anonymous actions
    public int? AppUserId { get; set; }    // Optional: Link to AppUser
    public AppUser? AppUser { get; set; }

    public string Action { get; set; } = default!;  // e.g. "ViewSet", "AddToInventory", "SearchBricks"
    public string? Details { get; set; }            // JSON or text with additional info
    public string? PageUrl { get; set; }            // Which page/route

    public DateTime CreatedAt { get; set; }
}
