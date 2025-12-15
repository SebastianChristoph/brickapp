namespace Data.Entities;

public class MappedBrick
{
    public int Id { get; set; }
    public bool HasAtLeastOneMapping { get; set; } = false;
    public string Uuid { get; set; } = string.Empty;
    public string Name { get; set; } = default!;
    public string? LegoPartNum { get; set; }
    public string? LegoName { get; set; } 
    public string? BluebrixxPartNum { get; set; }
    public string? BluebrixxName { get; set; }
    public string? CadaPartNum { get; set; }
    public string? CadaName { get; set; }
    public string? PantasyPartNum { get; set; }
    public string? PantasyName { get; set; }
    public string? MouldKingPartNum { get; set; }
    public string? MouldKingName { get; set; }
    public string? UnknownPartNum { get; set; }
    public string? UnknownName { get; set; }

    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    // Navigation: MappingRequests zu diesem Brick
    public ICollection<MappingRequest> MappingRequests { get; set; } = new List<MappingRequest>();
}
