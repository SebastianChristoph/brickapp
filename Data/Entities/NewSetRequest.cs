using System;
using System.Collections.Generic;

namespace Data.Entities
{
    public class NewSetRequest
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string SetNo { get; set; }
        public string SetName { get; set; }
        public string? ImagePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public NewSetRequestStatus Status { get; set; } = NewSetRequestStatus.Draft;
        public bool IsDraft => Status == NewSetRequestStatus.Draft;
        public string? ReasonRejected { get; set; }
        public string UserId { get; set; }
        public List<NewSetRequestItem> Items { get; set; } = new();
    }

    public class NewSetRequestItem
    {
        public int Id { get; set; }
        public int NewSetRequestId { get; set; }
        public string ItemIdOrName { get; set; }
        public int Quantity { get; set; }
        public string Color { get; set; }
    }

    public enum NewSetRequestStatus
    {
        Draft,
        Pending,
        Approved,
        Rejected
    }
}
