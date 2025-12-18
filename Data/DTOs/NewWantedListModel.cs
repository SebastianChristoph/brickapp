using System.Collections.Generic;

namespace brickapp.Data.DTOs
{
    public class NewWantedListModel
    {
        public string Name { get; set; } = string.Empty;
        public List<NewWantedListItemModel> Items { get; set; } = new();
      
    }


    public class NewWantedListItemModel
    {
        public int MappedBrickId { get; set; }
        public string BrickName { get; set; } = string.Empty;
        public int ColorId { get; set; }
        public int Quantity { get; set; }
    }
}
