using brickapp.Components.Shared.PartsListUpload;

namespace brickapp.Data.DTOs
{
    public class NewWantedListModel
    {
        public string Name { get; set; } = string.Empty;
        public List<NewWantedListItemModel> Items { get; init; } = [];
        public List<UnmappedRow> UnmappedRows { get; set; } = [];
        public string Source { get; set; } = string.Empty;
      
    }
    
    public class NewWantedListItemModel
    {
        public int MappedBrickId { get; init; }
        public string BrickName { get; init; } = string.Empty;
        public int ColorId { get; set; }
        public int Quantity { get; set; }
    }
}
