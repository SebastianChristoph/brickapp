using System;

namespace brickapp.Data.Entities
{
    public class ItemImageRequest
    {
        public int Id { get; set; }
        public int MappedBrickId { get; set; }
        public string RequestedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ItemImageRequestStatus Status { get; set; } = ItemImageRequestStatus.Pending;
        public string? ReasonRejected { get; set; }
        public string? PendingReason { get; set; }
        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string TempImagePath { get; set; } = string.Empty; // Temporärer Pfad für das hochgeladene Bild

        // Navigation properties
        public MappedBrick? MappedBrick { get; set; }
        public AppUser? RequestedByUser { get; set; }
        public AppUser? ApprovedByUser { get; set; }
    }

    public enum ItemImageRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
