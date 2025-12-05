namespace brickisbrickapp.Data.Entities;

public class MappedBrick
{
    public int Id { get; set; }

    // dein „neutraler“ Name
    public string Name { get; set; } = default!;

    // LEGO / Rebrickable
    public string? LegoPartNum { get; set; }   // parts.part_num
    public string? LegoName { get; set; }      // parts.name

    // Andere Hersteller (kannst du später befüllen)
    public string? BbPartNum { get; set; }
    public string? BbName { get; set; }
    public string? CadaPartNum { get; set; }
    public string? CadaName { get; set; }
    public string? PantasyPartNum { get; set; }
    public string? PantasyName { get; set; }
    public string? MouldKingPartNum { get; set; }
    public string? MouldKingName { get; set; }

    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
