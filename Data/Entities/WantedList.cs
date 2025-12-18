using System.Collections.Generic;

namespace Data.Entities
{
 public class WantedList
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AppUserId { get; set; }
    public List<WantedListItem> Items { get; set; } = new();
    
    // NEU: Damit die Daten in der DB landen
    public List<WantedListMissingItem> MissingItems { get; set; } = new();
}
    public class WantedListItem
    {
        public int Id { get; set; }
        public int WantedListId { get; set; }
        public int MappedBrickId { get; set; }
        public int BrickColorId { get; set; }
        public int Quantity { get; set; }

        public MappedBrick? MappedBrick { get; set; }
        public BrickColor? BrickColor { get; set; }
        public WantedList? WantedList { get; set; }
        public List<MissingItem> MissingItems { get; set; } = new();
    }

    // NEU: Die Klasse für ungemappte Teile
public class WantedListMissingItem
{
    public int Id { get; set; }
    public int WantedListId { get; set; }
    public string? ExternalPartNum { get; set; }
    public int? ExternalColorId { get; set; }
    public int Quantity { get; set; }
}
}
