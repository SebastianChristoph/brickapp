using System;

namespace brickisbrickapp.Data.Entities
{
    public class UserNotification
    {
        public int Id { get; set; }
        public string UserUuid { get; set; } = string.Empty; // Verweis auf AppUser.Uuid
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public string? RelatedEntityType { get; set; } // z.B. "MappingRequest"
        public int? RelatedEntityId { get; set; }
        // Navigation
        public AppUser? User { get; set; }
    }
}
