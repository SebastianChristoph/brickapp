
using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Services
{
    public class RequestService
    {
        // --- NewItemRequest Methoden ---
        public async Task<List<NewItemRequest>> GetNewItemRequestsByUserAsync(string userId)
        {
            return await _db.NewItemRequests
                .Include(r => r.RequestedByUser)
                .Include(r => r.ApprovedByUser)
                .Where(r => r.RequestedByUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
        public async Task<bool> DeleteNewSetRequestAsync(int id)
        {
            var request = await _db.NewSetRequests.FindAsync(id);
            if (request == null) return false;
            _db.NewSetRequests.Remove(request);
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<List<NewItemRequest>> GetOpenNewItemRequestsAsync()
        {
            return await _db.NewItemRequests
                .Include(r => r.RequestedByUser)
                .Where(r => r.Status == NewItemRequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<NewItemRequest?> GetNewItemRequestByIdAsync(int id)
        {
            return await _db.NewItemRequests
                .Include(r => r.RequestedByUser)
                .Include(r => r.ApprovedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<NewItemRequest> CreateNewItemRequestAsync(string brand, string name, string? imagePath, string userId)
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

        public async Task ApproveNewItemRequestAsync(int requestId, string adminUserId)
        {
            var request = await _db.NewItemRequests.FindAsync(requestId);
            if (request == null || request.Status != NewItemRequestStatus.Pending) return;
            request.Status = NewItemRequestStatus.Approved;
            request.ApprovedByUserId = adminUserId;
            request.ApprovedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            if (_notificationService != null)
            {
                await _notificationService.AddNotificationAsync(
                    request.RequestedByUserId,
                    "Neues Item genehmigt",
                    $"Dein Request für {request.Brand} ({request.Name}) wurde genehmigt.",
                    "NewItemRequest",
                    request.Id
                );
            }
        }

        public async Task RejectNewItemRequestAsync(int requestId, string adminUserId, string reason)
        {
            var request = await _db.NewItemRequests.FindAsync(requestId);
            if (request == null || request.Status != NewItemRequestStatus.Pending) return;
            request.Status = NewItemRequestStatus.Rejected;
            request.ApprovedByUserId = adminUserId;
            request.ApprovedAt = DateTime.UtcNow;
            request.ReasonRejected = reason;
            await _db.SaveChangesAsync();
            if (_notificationService != null)
            {
                await _notificationService.AddNotificationAsync(
                    request.RequestedByUserId,
                    "Neues Item abgelehnt",
                    $"Dein Request für {request.Brand} ({request.Name}) wurde abgelehnt: {reason}",
                    "NewItemRequest",
                    request.Id
                );
            }
        }

        public async Task<bool> IsNewItemRequestBlockedAsync(string name, string brand)
        {
            return await _db.NewItemRequests.AnyAsync(r => r.Name == name && r.Brand == brand && r.Status == NewItemRequestStatus.Pending);
        }

        // --- NewSetRequest Methoden ---
        public async Task<List<NewSetRequest>> GetNewSetRequestsByUserAsync(string userId)
        {
            return await _db.NewSetRequests
                .Include(r => r.Items)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<NewSetRequest>> GetOpenNewSetRequestsAsync()
        {
            return await _db.NewSetRequests
                .Include(r => r.Items)
                .Where(r => r.Status == NewSetRequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<NewSetRequest?> GetNewSetRequestByIdAsync(int id)
        {
            return await _db.NewSetRequests
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<NewSetRequest> CreateNewSetRequestAsync(string brand, string setNo, string setName, string? imagePath, string userId, List<NewSetRequestItem> items, NewSetRequestStatus status)
        {
            var request = new NewSetRequest
            {
                Brand = brand,
                SetNo = setNo,
                SetName = setName,
                ImagePath = imagePath,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = status,
                Items = items
            };
            _db.NewSetRequests.Add(request);
            await _db.SaveChangesAsync();
            return request;
        }

        public async Task ApproveNewSetRequestAsync(int requestId)
        {
            var request = await _db.NewSetRequests.FindAsync(requestId);
            if (request == null || request.Status != NewSetRequestStatus.Pending) return;
            request.Status = NewSetRequestStatus.Approved;
            request.ReasonRejected = null;
            request.CreatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            if (_notificationService != null)
            {
                await _notificationService.AddNotificationAsync(
                    request.UserId,
                    "Neues Set genehmigt",
                    $"Dein Request für Set {request.SetNo} ({request.SetName}) wurde genehmigt.",
                    "NewSetRequest",
                    request.Id
                );
            }
        }

        public async Task RejectNewSetRequestAsync(int requestId, string reason)
        {
            var request = await _db.NewSetRequests.FindAsync(requestId);
            if (request == null || request.Status != NewSetRequestStatus.Pending) return;
            request.Status = NewSetRequestStatus.Rejected;
            request.ReasonRejected = reason;
            await _db.SaveChangesAsync();
            if (_notificationService != null)
            {
                await _notificationService.AddNotificationAsync(
                    request.UserId,
                    "Neues Set abgelehnt",
                    $"Dein Request für Set {request.SetNo} ({request.SetName}) wurde abgelehnt: {reason}",
                    "NewSetRequest",
                    request.Id
                );
            }
        }

        public async Task<bool> IsNewSetRequestBlockedAsync(string setNo, string brand)
        {
            return await _db.NewSetRequests.AnyAsync(r => r.SetNo == setNo && r.Brand == brand && r.Status == NewSetRequestStatus.Pending);
        }
        private readonly AppDbContext _db;
        private readonly UserNotificationService _notificationService;
        public RequestService(AppDbContext db, UserNotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }
    
        public async Task<List<MappingRequest>> GetMappingRequestsByUserAsync(string userId)
        {
            return await _db.MappingRequests
                .Include(mr => mr.Brick)
                .Include(mr => mr.ApprovedByUser)
                .Where(mr => mr.RequestedByUserId == userId)
                .OrderByDescending(mr => mr.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<MappingRequest>> GetOpenMappingRequestsAsync()
        {
            return await _db.MappingRequests
                .Include(mr => mr.Brick)
                .Include(mr => mr.RequestedByUser)
                .Where(mr => mr.Status == MappingRequestStatus.Pending)
                .OrderBy(mr => mr.CreatedAt)
                .ToListAsync();
        }

        public async Task<MappingRequest?> GetMappingRequestByIdAsync(int id)
        {
            return await _db.MappingRequests
                .Include(mr => mr.Brick)
                .Include(mr => mr.RequestedByUser)
                .Include(mr => mr.ApprovedByUser)
                .FirstOrDefaultAsync(mr => mr.Id == id);
        }

        public async Task<MappingRequest> CreateMappingRequestAsync(int brickId, string brand, string mappingName, string mappingItemId, string userId)
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

        public async Task ApproveMappingRequestAsync(int requestId, string adminUserId)
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

        public async Task RejectMappingRequestAsync(int requestId, string adminUserId, string reason)
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
