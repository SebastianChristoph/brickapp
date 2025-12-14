

using System.Runtime.CompilerServices;
using Data.Entities;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace Data.Services
{
    public class RequestService
    {
        private readonly AppDbContext _db;
        private readonly UserNotificationService _notificationService;
        private readonly ImageService _imageService;
        private readonly ILogger<RequestService> _logger;

        public RequestService(AppDbContext db, UserNotificationService notificationService, ImageService imageService, ILogger<RequestService> logger)
        {
            _db = db;
            _notificationService = notificationService;
            _imageService = imageService;
            _logger = logger;
        }
        
        
        // NEW ITEM REQUESTS

          public async Task<NewItemRequest> CreateNewItemRequestAsync(string brand, string name, string userId, string? partNum, IBrowserFile? imageFile = null)
        {
            _logger.LogInformation("🟡 [RequestService] CreateNewItemRequestAsync called: Brand={Brand}, Name={Name}, UserId={UserId}, PartNum={PartNum}, ImageFileNull={ImageFileNull}", brand, name, userId, partNum, imageFile == null);
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
            _db.NewItemRequests.Add(request);

            if (imageFile != null)
            {
                _logger.LogInformation("🟡 [RequestService] Image file provided. Brand={Brand}, PartNum={PartNum}, Uuid={Uuid}", brand, partNum, request.Uuid);
                if (brand.ToLower().Trim() == "lego" && !string.IsNullOrEmpty(partNum))
                {
                    _logger.LogInformation("🟡 Saving LEGO image for PartNum={PartNum}", partNum);
                    await _imageService.SaveResizedItemImageAsync(imageFile, brand, partNum, null);
                }
                else
                {
                    _logger.LogInformation("🟡 [RequestService] Saving non-LEGO image for Uuid={Uuid}", request.Uuid);
                    await _imageService.SaveResizedItemImageAsync(imageFile, brand, null, request.Uuid);
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("🟡 [RequestService] NewItemRequest created with Id={Id} and Uuid={Uuid}", request.Id, request.Uuid);
            return request;
        }

        public async Task<List<NewItemRequest>> GetAllNewItemRequestsByUserAsync(string userId)
        {
            return await _db.NewItemRequests
                .Include(r => r.RequestedByUser)
                .Include(r => r.ApprovedByUser)
                .Where(r => r.RequestedByUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

       
         public async Task<List<NewItemRequest>> GetOpenNewItemRequestsAsync()
        {
            return await _db.NewItemRequests
                .Include(r => r.RequestedByUser)
                .Where(r => r.Status == NewItemRequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
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
       
        public async Task ApproveNewItemRequestAsync(int requestId, string adminUserId)
        {
            var request = await _db.NewItemRequests.FindAsync(requestId);
            if (request == null || request.Status != NewItemRequestStatus.Pending) return;
            request.Status = NewItemRequestStatus.Approved;
            request.ApprovedByUserId = adminUserId;
            request.ApprovedAt = DateTime.UtcNow;

            // Create new MappedBrick for the approved item
            var mappedBrick = new MappedBrick
            {
                Name = request.Name,
                Uuid = request.Uuid
            };
            // Set the brand-specific fields
            switch (request.Brand?.Trim().ToLower())
            {
                case "bb":
                case "bluebrixx":
                    mappedBrick.BbName = request.Name;
                    mappedBrick.BbPartNum = request.PartNum;
                    break;
                case "cada":
                    mappedBrick.CadaName = request.Name;
                    mappedBrick.CadaPartNum = request.PartNum;
                    break;
                case "pantasy":
                    mappedBrick.PantasyName = request.Name;
                    mappedBrick.PantasyPartNum = request.PartNum;
                    break;
                case "mould king":
                case "mouldking":
                    mappedBrick.MouldKingName = request.Name;
                    mappedBrick.MouldKingPartNum = request.PartNum;
                    break;
                case "unknown":
                    mappedBrick.UnknownName = request.Name;
                    mappedBrick.UnknownPartNum = request.PartNum;
                    break;
                default:
                    // fallback: set as unknown
                    mappedBrick.UnknownName = request.Name;
                    mappedBrick.UnknownPartNum = request.PartNum;
                    break;
            }

            _db.MappedBricks.Add(mappedBrick);
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

         public async Task<bool> UpdateNewSetRequestAsync(int id, string brand, string setNo, string setName, string? imagePath, List<NewSetRequestItem> items, NewSetRequestStatus status)
        {
            var request = await _db.NewSetRequests.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id);
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
            await _db.SaveChangesAsync();
            return true;
        }
        
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
            var request = await _db.NewSetRequests
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null || request.Status != NewSetRequestStatus.Pending) return;

            // ItemSet anlegen
            var itemSet = new ItemSet
            {
                Name = request.SetName,
                Brand = request.Brand,
                LegoSetNum = request.SetNo,
                Year = null // Optional: aus Request übernehmen, falls vorhanden
            };
            _db.ItemSets.Add(itemSet);
            await _db.SaveChangesAsync();

            // Bricks anlegen
            foreach (var reqItem in request.Items)
            {
                int mappedBrickId = 0;
                if (int.TryParse(reqItem.ItemIdOrName, out var id))
                    mappedBrickId = id;

                // BrickColorId auflösen (hier nach Name suchen)
                var color = await _db.BrickColors.FirstOrDefaultAsync(c => c.Name == reqItem.Color);
                int colorId = color?.Id ?? 1; // Fallback: 1

                var setBrick = new ItemSetBrick
                {
                    ItemSetId = itemSet.Id,
                    MappedBrickId = mappedBrickId,
                    BrickColorId = colorId,
                    Quantity = reqItem.Quantity
                };
                _db.ItemSetBricks.Add(setBrick);
            }
            // Request-Status setzen
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
   
    
        // --- Mapping Requests Methoden ---
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
