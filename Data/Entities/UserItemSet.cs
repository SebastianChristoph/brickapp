namespace brickapp.Data.Entities;

public class UserItemSet
{
    public int Id { get; set; }

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = default!;

    public int ItemSetId { get; set; }
    public ItemSet ItemSet { get; set; } = default!;

    public int Quantity { get; set; }   // wie oft der User dieses Set hat
    public ICollection<UserItemSet> UserItemSets { get; set; } = new List<UserItemSet>();

}