using brickapp.Components.Shared.PartsListUpload;


namespace brickapp.Data.DTOs
{
    public class NewWantedListModel
    {
        public string Name { get; set; } = string.Empty;
        public List<NewWantedListItemModel> Items { get; set; } = new();
        public List<UnmappedRow> UnmappedRows { get; set; } = new();
        public string Source { get; set; } = string.Empty; // z.B. manual, import, ...
      
    }


    public class NewWantedListItemModel
    {
        public int MappedBrickId { get; set; }
        public string BrickName { get; set; } = string.Empty;
        public int ColorId { get; set; }
        public int Quantity { get; set; }
    }
}
