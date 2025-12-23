using System;
using System.Collections.Generic;

namespace brickapp.Data.Entities
{
    public class NewSetRequest
    {
        public int Id { get; set; }
        public required string Brand { get; set; }
        public required string SetNo { get; set; }
        public required string SetName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public NewSetRequestStatus Status { get; set; } = NewSetRequestStatus.Draft;
        public bool IsDraft => Status == NewSetRequestStatus.Draft;
        public string? ReasonRejected { get; set; }
        public string? PendingReason { get; set; }
        public required string UserId { get; set; }
        public List<NewSetRequestItem> Items { get; set; } = new();
    }

    public class NewSetRequestItem
    {
        public int Id { get; set; }
        public int NewSetRequestId { get; set; }
        public required string ItemIdOrName { get; set; }
        public int Quantity { get; set; }
        public required string Color { get; set; }
    }

    public enum NewSetRequestStatus
    {
        Draft,
        Pending,
        Approved,
        Rejected
    }
}
