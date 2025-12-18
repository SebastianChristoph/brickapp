using System.Collections.Generic;

namespace Data.DTOs
{
    public class NewWantedListModel
    {
        public string Name { get; set; } = string.Empty;
        public List<NewWantedListItemModel> Items { get; set; } = new();
        public List<MissingItemDTO> MissingItems { get; set; } = new();
    }

    // Falls noch nicht vorhanden, ein einfaches DTO für MissingItems
    public class MissingItemDTO
    {
        public string? ExternalPartNum { get; set; }
        public int? ExternalColorId { get; set; }
        public int Quantity { get; set; }
    }

    public class NewWantedListItemModel
    {
        public int MappedBrickId { get; set; }
        public string BrickName { get; set; } = string.Empty;
        public int ColorId { get; set; }
        public int Quantity { get; set; }
    }
}
