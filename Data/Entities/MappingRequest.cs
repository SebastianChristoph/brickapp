using System;

namespace Data.Entities
{
    public class MappingRequest
    {
        public int Id { get; set; }
        public int BrickId { get; set; } // Verweis auf MappedBrick
        public string Brand { get; set; } = string.Empty;
        public string MappingName { get; set; } = string.Empty;
        public string MappingItemId { get; set; } = string.Empty;
        public string RequestedByUserId { get; set; } = string.Empty; // AppUser.Id
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public MappingRequestStatus Status { get; set; } = MappingRequestStatus.Pending;
        public string? ReasonRejected { get; set; }
        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Navigation properties
        public AppUser? RequestedByUser { get; set; }
        public AppUser? ApprovedByUser { get; set; }
        public MappedBrick? Brick { get; set; }
    }

    public enum MappingRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
