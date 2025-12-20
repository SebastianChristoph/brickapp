namespace brickapp.Data.Entities;

public class UserSetFavorite
{
    public int Id { get; set; }

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = default!;

    public int ItemSetId { get; set; }
    public ItemSet ItemSet { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
}
