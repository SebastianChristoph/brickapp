using brickisbrickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace brickisbrickapp.Data.Services
{
    public class MappingRequestService
    {
        private readonly AppDbContext _db;
        private readonly UserNotificationService _notificationService;
        public MappingRequestService(AppDbContext db, UserNotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }
        public MappingRequestService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<MappingRequest>> GetRequestsByUserAsync(string userId)
        {
            return await _db.MappingRequests
                .Include(mr => mr.Brick)
                .Include(mr => mr.ApprovedByUser)
                .Where(mr => mr.RequestedByUserId == userId)
                .OrderByDescending(mr => mr.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<MappingRequest>> GetOpenRequestsAsync()
        {
            return await _db.MappingRequests
                .Include(mr => mr.Brick)
                .Include(mr => mr.RequestedByUser)
                .Where(mr => mr.Status == MappingRequestStatus.Pending)
                .OrderBy(mr => mr.CreatedAt)
                .ToListAsync();
        }

        public async Task<MappingRequest?> GetByIdAsync(int id)
        {
            return await _db.MappingRequests
                .Include(mr => mr.Brick)
                .Include(mr => mr.RequestedByUser)
                .Include(mr => mr.ApprovedByUser)
                .FirstOrDefaultAsync(mr => mr.Id == id);
        }

        public async Task<MappingRequest> CreateRequestAsync(int brickId, string brand, string mappingName, string mappingItemId, string userId)
        {
            var request = new MappingRequest
            {
                BrickId = brickId,
                Brand = brand,
                MappingName = mappingName,
                MappingItemId = mappingItemId,
                RequestedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = MappingRequestStatus.Pending
            };
            _db.MappingRequests.Add(request);
            await _db.SaveChangesAsync();
            return request;
        }

        public async Task ApproveRequestAsync(int requestId, string adminUserId)
        {
            var request = await _db.MappingRequests.FindAsync(requestId);
            if (request == null || request.Status != MappingRequestStatus.Pending) return;
            request.Status = MappingRequestStatus.Approved;
            request.ApprovedByUserId = adminUserId;
            request.ApprovedAt = DateTime.UtcNow;
            // Mapping in MappedBrick durchführen
            var brick = await _db.MappedBricks.FindAsync(request.BrickId);
            if (brick != null)
            {
                switch (request.Brand)
                {
                    case "BB": brick.BbName = request.MappingName; brick.BbPartNum = request.MappingItemId; break;
                    case "Cada": brick.CadaName = request.MappingName; brick.CadaPartNum = request.MappingItemId; break;
                    case "Pantasy": brick.PantasyName = request.MappingName; brick.PantasyPartNum = request.MappingItemId; break;
                    case "Mould King": brick.MouldKingName = request.MappingName; brick.MouldKingPartNum = request.MappingItemId; break;
                    case "Unknown": brick.UnknownName = request.MappingName; brick.UnknownPartNum = request.MappingItemId; break;
                }
            }
            await _db.SaveChangesAsync();
            // Notification für User
            await _notificationService.AddNotificationAsync(
                request.RequestedByUserId,
                "Mapping genehmigt",
                $"Dein Mapping-Request für {request.Brand} ({request.MappingName}) wurde genehmigt.",
                "MappingRequest",
                request.Id
            );
        }

        public async Task RejectRequestAsync(int requestId, string adminUserId, string reason)
        {
            var request = await _db.MappingRequests.FindAsync(requestId);
            if (request == null || request.Status != MappingRequestStatus.Pending) return;
            request.Status = MappingRequestStatus.Rejected;
            request.ApprovedByUserId = adminUserId;
            request.ApprovedAt = DateTime.UtcNow;
            request.ReasonRejected = reason;
            await _db.SaveChangesAsync();
            // Notification für User
            await _notificationService.AddNotificationAsync(
                request.RequestedByUserId,
                "Mapping abgelehnt",
                $"Dein Mapping-Request für {request.Brand} ({request.MappingName}) wurde abgelehnt: {reason}",
                "MappingRequest",
                request.Id
            );
        }

        public async Task<bool> IsMappingBlockedAsync(int brickId, string brand)
        {
            return await _db.MappingRequests.AnyAsync(mr => mr.BrickId == brickId && mr.Brand == brand && mr.Status == MappingRequestStatus.Pending);
        }
    }
}
