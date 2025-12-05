using brickisbrickapp.Data.Entities;

public class ItemSet
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
    public string Brand { get; set; } = "Lego";

    public string? LegoSetNum { get; set; }    // sets.set_num
    public int? Year { get; set; }
    public string? ImageUrl { get; set; }      // sets.img_url

    public ICollection<ItemSetBrick> Bricks { get; set; } = new List<ItemSetBrick>();
}
