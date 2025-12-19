using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace brickapp.Data.Entities
{
    public class MockItem
    {
        [Key]
        public int Id { get; set; }
        public int MockId { get; set; }
        [ForeignKey("MockId")]
        public Mock Mock { get; set; } = default!;
        public int? MappedBrickId { get; set; }
        [ForeignKey("MappedBrickId")]
        public MappedBrick? MappedBrick { get; set; }
        public int? BrickColorId { get; set; }
        [ForeignKey("BrickColorId")]
        public BrickColor? BrickColor { get; set; }
        public string? ExternalPartNum { get; set; }
        public int Quantity { get; set; }
    }
}