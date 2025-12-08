using System;
using System.Collections.Generic;

namespace brickisbrickapp.Data.Entities
{
    public class NewItemRequest
    {
        public int Id { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string RequestedByUserId { get; set; } = string.Empty; // AppUser.Uuid
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public NewItemRequestStatus Status { get; set; } = NewItemRequestStatus.Pending;
        public string? ReasonRejected { get; set; }
        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Navigation properties
        public AppUser? RequestedByUser { get; set; }
        public AppUser? ApprovedByUser { get; set; }
    }

    public enum NewItemRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
