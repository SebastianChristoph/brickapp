using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using brickisbrickapp.Data.Entities;

namespace brickisbrickapp.Data.Services
{
    public class NewItemRequestService
    {
        private readonly AppDbContext _db;
        private readonly UserNotificationService _notificationService;
        public NewItemRequestService(AppDbContext db, UserNotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        public async Task<NewItemRequest> CreateRequestAsync(string brand, string name, string? imagePath, string userId)
        {
            var request = new NewItemRequest
            {
                Brand = brand,
                Name = name,
                ImagePath = imagePath,
                RequestedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = NewItemRequestStatus.Pending
            };
            _db.NewItemRequests.Add(request);
            await _db.SaveChangesAsync();
            return request;
        }

        public async Task<List<NewItemRequest>> GetRequestsByUserAsync(string userId)
        {
            return await _db.NewItemRequests
                .Where(r => r.RequestedByUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<NewItemRequest>> GetOpenRequestsAsync()
        {
            return await _db.NewItemRequests
                .Where(r => r.Status == NewItemRequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task ApproveRequestAsync(int requestId, string adminUserId)
        {
            var request = await _db.NewItemRequests.FindAsync(requestId);
            if (request == null || request.Status != NewItemRequestStatus.Pending) return;
            request.Status = NewItemRequestStatus.Approved;
            request.ApprovedByUserId = adminUserId;
            request.ApprovedAt = DateTime.UtcNow;

            // MappedBrick anlegen
            var brick = new MappedBrick
            {
                Name = request.Name,
            };
            switch (request.Brand)
            {
                case "BB":
                    brick.BbName = request.Name;
                    break;
                case "Cada":
                    brick.CadaName = request.Name;
                    break;
                case "Pantasy":
                    brick.PantasyName = request.Name;
                    break;
                case "Mould King":
                    brick.MouldKingName = request.Name;
                    break;
                case "Unknown":
                    brick.UnknownName = request.Name;
                    break;
            }
            _db.MappedBricks.Add(brick);

            await _db.SaveChangesAsync();
            await _notificationService.AddNotificationAsync(
                request.RequestedByUserId,
                "Item genehmigt",
                $"Dein Item-Request '{request.Name}' ({request.Brand}) wurde genehmigt.",
                "NewItemRequest",
                request.Id
            );
        }

        public async Task RejectRequestAsync(int requestId, string adminUserId, string reason)
        {
            var request = await _db.NewItemRequests.FindAsync(requestId);
            if (request == null || request.Status != NewItemRequestStatus.Pending) return;
            request.Status = NewItemRequestStatus.Rejected;
            request.ApprovedByUserId = adminUserId;
            request.ApprovedAt = DateTime.UtcNow;
            request.ReasonRejected = reason;
            await _db.SaveChangesAsync();
            await _notificationService.AddNotificationAsync(
                request.RequestedByUserId,
                "Item abgelehnt",
                $"Dein Item-Request '{request.Name}' ({request.Brand}) wurde abgelehnt: {reason}",
                "NewItemRequest",
                request.Id
            );
        }
    }
}
