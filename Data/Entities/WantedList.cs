using System.Collections.Generic;

namespace brickapp.Data.Entities
{
    public class WantedList
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AppUserId { get; set; }
        public List<WantedListItem> Items { get; set; } = new();
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
    }
}