using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace brickisbrickapp.Data.Entities
{
    public class Mock
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string MockType { get; set; } = "Bricklink"; // z.B. Bricklink, CSV, ...
        public string UserUuid { get; set; } = string.Empty;
        [ForeignKey("UserUuid")]
        public AppUser User { get; set; } = default!;
        public List<MockItem> Items { get; set; } = new();
    }
}