using System.ComponentModel.DataAnnotations;

namespace brickapp.Data.Entities
{
    public class MissingItem
    {
        [Key]
        public int Id { get; set; }
        public string? ExternalPartNum { get; set; }
        public int? ExternalColorId { get; set; }
        public int Quantity { get; set; }
    }
}