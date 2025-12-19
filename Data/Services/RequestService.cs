using brickapp.Data;
using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace brickapp.Data.Services

{
    public class RequestService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly UserNotificationService _notificationService;
        private readonly ImageService _imageService;
        private readonly ILogger<RequestService> _logger;

        public RequestService(
            IDbContextFactory<AppDbContext> dbFactory,
            UserNotificationService notificationService,
            ImageService imageService,
            ILogger<RequestService> logger)
        {
            _dbFactory = dbFactory;
            _notificationService = notificationService;
            _imageService = imageService;
            _logger = logger;
        }

        // NEW ITEM REQUESTS
        public async Task<NewItemRequest> CreateNewItemRequestAsync(string brand, string name, string userId, string? partNum, IBrowserFile? imageFile = null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            _logger.LogInformation("🟡 [RequestService] CreateNewItemRequestAsync called: Brand={Brand}, Name={Name}, UserId={UserId}, PartNum={PartNum}, ImageFileNull={ImageFileNull}",
                brand, name, userId, partNum, imageFile == null);

            var request = new NewItemRequest
            {
                Uuid = Guid.NewGuid().ToString(),
                Brand = brand,
                Name = name,
                PartNum = partNum ?? string.Empty,
                RequestedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = NewItemRequestStatus.Pending
            };

            db.NewItemRequests.Add(request);

            if (imageFile != null)
            {
                _logger.LogInformation("🟡 [RequestService] Image file provided. Brand={Brand}, PartNum={PartNum}, Uuid={Uuid}",
                    brand, partNum, request.Uuid);

                if (brand.ToLower().Trim() == "lego" && !string.IsNullOrEmpty(partNum))
                    await _imageService.SaveResizedItemImageAsync(imageFile, brand, partNum, null);
                else
                    await _imageService.SaveResizedItemImageAsync(imageFile, brand, null, request.Uuid);
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("🟡 [RequestService] NewItemRequest created with Id={Id} and Uuid={Uuid}", request.Id, request.Uuid);
            return request;
        }

        public async Task<List<NewItemRequest>> GetAllNewItemRequestsByUserAsync(string userId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.NewItemRequests
                .Include(r => r.RequestedByUser)
                .Include(r => r.ApprovedByUser)
                .Where(r => r.RequestedByUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<NewItemRequest>> GetOpenNewItemRequestsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.NewItemRequests
                .Include(r => r.RequestedByUser)
                .Where(r => r.Status == NewItemRequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteNewSetRequestAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var request = await db.NewSetRequests.FindAsync(id);
            if (request == null) return false;
            db.NewSetRequests.Remove(request);
            await db.SaveChangesAsync();
            return true;
        }

      public async Task ApproveNewItemRequestAsync(
    int requestId,
    string adminUserId,
    string? overrideName)
{
    await using var db = await _dbFactory.CreateDbContextAsync();

    var request = await db.NewItemRequests.FindAsync(requestId);
    if (request == null || request.Status != NewItemRequestStatus.Pending)
        return;

    var finalName = string.IsNullOrWhiteSpace(overrideName)
        ? request.Name
        : overrideName.Trim();

    // Namen ggf. überschreiben
    request.Name = finalName;

    request.Status = NewItemRequestStatus.Approved;
    request.ApprovedByUserId = adminUserId;
    request.ApprovedAt = DateTime.UtcNow;

    var mappedBrick = new MappedBrick
    {
        Name = finalName,
        Uuid = request.Uuid,
    };

    switch (request.Brand?.Trim().ToLower())
        {
            case "lego":
            mappedBrick.LegoName = finalName;
            mappedBrick.LegoPartNum = request.PartNum;
            break;
        case "bluebrixx":
            mappedBrick.BluebrixxName = finalName;
            mappedBrick.BluebrixxPartNum = request.PartNum;
            break;
        case "cada":
            mappedBrick.CadaName = finalName;
            mappedBrick.CadaPartNum = request.PartNum;
            break;
        case "pantasy":
            mappedBrick.PantasyName = finalName;
            mappedBrick.PantasyPartNum = request.PartNum;
            break;
        case "mould king":
        case "mouldking":
            mappedBrick.MouldKingName = finalName;
            mappedBrick.MouldKingPartNum = request.PartNum;
            break;
        default:
            mappedBrick.UnknownName = finalName;
            mappedBrick.UnknownPartNum = request.PartNum;
            break;
    }

    db.MappedBricks.Add(mappedBrick);
    await db.SaveChangesAsync();

    await _notificationService.AddNotificationAsync(
        request.RequestedByUserId,
        "New Item Approved",
        $"Your request for {request.Brand} ({finalName}) has been approved.",
        "NewItemRequest",
        request.Id
    );
}

public Task ApproveNewItemRequestAsync(int requestId, string adminUserId)
{
    return ApproveNewItemRequestAsync(requestId, adminUserId, null);
}
        public async Task RejectNewItemRequestAsync(int requestId, string adminUserId, string reason)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.NewItemRequests.FindAsync(requestId);
            if (request == null || request.Status != NewItemRequestStatus.Pending) return;

            request.Status = NewItemRequestStatus.Rejected;
            request.ApprovedByUserId = adminUserId;
            request.ApprovedAt = DateTime.UtcNow;
            request.ReasonRejected = reason;

            await db.SaveChangesAsync();

            await _notificationService.AddNotificationAsync(
                request.RequestedByUserId,
                "New Item Rejected",
                $"Your request for {request.Brand} ({request.Name}) has been rejected: {reason}",
                "NewItemRequest",
                request.Id
            );
        }

        public async Task<bool> IsNewItemRequestBlockedAsync(string name, string brand)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.NewItemRequests.AnyAsync(r => r.Name == name && r.Brand == brand && r.Status == NewItemRequestStatus.Pending);
        }

        // --- NewSetRequest Methoden ---
        public async Task<bool> UpdateNewSetRequestAsync(int id, string brand, string setNo, string setName, string? imagePath, List<NewSetRequestItem> items, NewSetRequestStatus status)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.NewSetRequests.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return false;

            request.Brand = brand;
            request.SetNo = setNo;
            request.SetName = setName;
            request.Status = status;

            request.Items.Clear();
            foreach (var item in items)
            {
                request.Items.Add(new NewSetRequestItem
                {
                    ItemIdOrName = item.ItemIdOrName,
                    Quantity = item.Quantity,
                    Color = item.Color
                });
            }

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<List<NewSetRequest>> GetNewSetRequestsByUserAsync(string userId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.NewSetRequests
                .Include(r => r.Items)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<NewSetRequest>> GetOpenNewSetRequestsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.NewSetRequests
                .Include(r => r.Items)
                .Where(r => r.Status == NewSetRequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<NewSetRequest?> GetNewSetRequestByIdAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.NewSetRequests
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<NewSetRequest> CreateNewSetRequestAsync(string brand, string setNo, string setName, string? imagePath, string userId, List<NewSetRequestItem> items, NewSetRequestStatus status)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = new NewSetRequest
            {
                Brand = brand,
                SetNo = setNo,
                SetName = setName,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = status,
                Items = items
            };

            db.NewSetRequests.Add(request);
            await db.SaveChangesAsync();
            return request;
        }

        public async Task ApproveNewSetRequestAsync(int requestId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.NewSetRequests
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.Status != NewSetRequestStatus.Pending) return;

            var itemSet = new ItemSet
            {
                Name = request.SetName,
                Brand = request.Brand,
                SetNum = request.SetNo,
                Year = null
            };

            db.ItemSets.Add(itemSet);
            await db.SaveChangesAsync();

            foreach (var reqItem in request.Items)
            {
                int mappedBrickId = 0;
                if (int.TryParse(reqItem.ItemIdOrName, out var id))
                    mappedBrickId = id;

                var color = await db.BrickColors.FirstOrDefaultAsync(c => c.Name == reqItem.Color);
                int colorId = color?.Id ?? 1;

                db.ItemSetBricks.Add(new ItemSetBrick
                {
                    ItemSetId = itemSet.Id,
                    MappedBrickId = mappedBrickId,
                    BrickColorId = colorId,
                    Quantity = reqItem.Quantity
                });
            }

            request.Status = NewSetRequestStatus.Approved;
            request.ReasonRejected = null;
            request.CreatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            await _notificationService.AddNotificationAsync(
                request.UserId,
                "New Set Approved",
                $"Your request for Set {request.SetNo} ({request.SetName}) has been approved.",
                "NewSetRequest",
                request.Id
            );
        }

        public async Task RejectNewSetRequestAsync(int requestId, string reason)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.NewSetRequests.FindAsync(requestId);
            if (request == null || request.Status != NewSetRequestStatus.Pending) return;

            request.Status = NewSetRequestStatus.Rejected;
            request.ReasonRejected = reason;
            await db.SaveChangesAsync();

            await _notificationService.AddNotificationAsync(
                request.UserId,
                "New Set Rejected",
                $"Your request for Set {request.SetNo} ({request.SetName}) has been rejected: {reason}",
                "NewSetRequest",
                request.Id
            );
        }

        public async Task<bool> IsNewSetRequestBlockedAsync(string setNo, string brand)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.NewSetRequests.AnyAsync(r => r.SetNo == setNo && r.Brand == brand && r.Status == NewSetRequestStatus.Pending);
        }

        // --- Mapping Requests Methoden ---
        public async Task<List<MappingRequest>> GetMappingRequestsByUserAsync(string userId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.MappingRequests
                .Include(mr => mr.Brick)
                .Include(mr => mr.ApprovedByUser)
                .Where(mr => mr.RequestedByUserId == userId)
                .OrderByDescending(mr => mr.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<MappingRequest>> GetOpenMappingRequestsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.MappingRequests
                .Include(mr => mr.Brick)
                .Include(mr => mr.RequestedByUser)
                .Where(mr => mr.Status == MappingRequestStatus.Pending)
                .OrderBy(mr => mr.CreatedAt)
                .ToListAsync();
        }

        public async Task<MappingRequest> CreateMappingRequestAsync(int brickId, string brand, string mappingName, string mappingItemId, string userId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

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

            db.MappingRequests.Add(request);
            await db.SaveChangesAsync();
            return request;
        }

        public async Task ApproveMappingRequestAsync(int requestId, string adminUserId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.MappingRequests.FindAsync(requestId);
            if (request == null || request.Status != MappingRequestStatus.Pending) return;

            request.Status = MappingRequestStatus.Approved;
            request.ApprovedByUserId = adminUserId;
            request.ApprovedAt = DateTime.UtcNow;

            var brick = await db.MappedBricks.FindAsync(request.BrickId);
            if (brick != null)
            {
                switch (request.Brand)
                {
                    case "BlueBrixx": brick.BluebrixxName = request.MappingName; brick.BluebrixxPartNum = request.MappingItemId; break;
                    case "Cada": brick.CadaName = request.MappingName; brick.CadaPartNum = request.MappingItemId; break;
                    case "Pantasy": brick.PantasyName = request.MappingName; brick.PantasyPartNum = request.MappingItemId; break;
                    case "Mould King": brick.MouldKingName = request.MappingName; brick.MouldKingPartNum = request.MappingItemId; break;
                    case "Unknown": brick.UnknownName = request.MappingName; brick.UnknownPartNum = request.MappingItemId; break;
                }
                brick.HasAtLeastOneMapping = true;
                _logger.LogInformation($"🟡 [RequestService] HasAtLeastOneMapping = true für {brick.Name} gesetzt");
            }

            await db.SaveChangesAsync();

            await _notificationService.AddNotificationAsync(
                request.RequestedByUserId,
                "Mapping Approved",
                $"Your mapping request for {request.Brand} ({request.MappingName}) has been approved.",
                "MappingRequest",
                request.Id
            );
        }

        public async Task RejectMappingRequestAsync(int requestId, string adminUserId, string reason)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.MappingRequests.FindAsync(requestId);
            if (request == null || request.Status != MappingRequestStatus.Pending) return;

            request.Status = MappingRequestStatus.Rejected;
            request.ApprovedByUserId = adminUserId;
            request.ApprovedAt = DateTime.UtcNow;
            request.ReasonRejected = reason;

            await db.SaveChangesAsync();

            await _notificationService.AddNotificationAsync(
                request.RequestedByUserId,
                "Mapping Rejected",
                $"Your mapping request for {request.Brand} ({request.MappingName}) has been rejected: {reason}",
                "MappingRequest",
                request.Id
            );
        }

        public async Task<bool> IsMappingBlockedAsync(int brickId, string brand)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.MappingRequests.AnyAsync(mr => mr.BrickId == brickId && mr.Brand == brand && mr.Status == MappingRequestStatus.Pending);
        }
    }
}
