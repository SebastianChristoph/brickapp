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

    var brand = request.Brand?.Trim().ToLower();
            var partNum = request.PartNum;

       MappedBrick? existingBrick = null;

// --- DOPPELTEN-CHECK ---

if (string.IsNullOrWhiteSpace(partNum))
{
    // 1. Fall: Keine PartNum -> Gezielte Namenssuche je nach Brand
    existingBrick = brand switch
    {
        "lego" => await db.MappedBricks.FirstOrDefaultAsync(m => m.LegoName == request.Name),
        "bluebrixx" => await db.MappedBricks.FirstOrDefaultAsync(m => m.BluebrixxName == request.Name),
        "cada" => await db.MappedBricks.FirstOrDefaultAsync(m => m.CadaName == request.Name),
        "pantasy" => await db.MappedBricks.FirstOrDefaultAsync(m => m.PantasyName == request.Name),
        "mould king" or "mouldking" => await db.MappedBricks.FirstOrDefaultAsync(m => m.MouldKingName == request.Name),
        _ => await db.MappedBricks.FirstOrDefaultAsync(m => m.UnknownName == request.Name) // Falls du ein UnknownName Feld hast
    };
}
else
{
    // 2. Fall: PartNum vorhanden -> Suche über die ID
    existingBrick = brand switch
    {
        "lego" => await db.MappedBricks.FirstOrDefaultAsync(m => m.LegoPartNum == partNum),
        "bluebrixx" => await db.MappedBricks.FirstOrDefaultAsync(m => m.BluebrixxPartNum == partNum),
        "cada" => await db.MappedBricks.FirstOrDefaultAsync(m => m.CadaPartNum == partNum),
        "pantasy" => await db.MappedBricks.FirstOrDefaultAsync(m => m.PantasyPartNum == partNum),
        "mould king" or "mouldking" => await db.MappedBricks.FirstOrDefaultAsync(m => m.MouldKingPartNum == partNum),
        _ => await db.MappedBricks.FirstOrDefaultAsync(m => m.UnknownPartNum == partNum)
    };
}

    // MappedBrick? existingBrick = brand switch
    // {
    //     "lego" => await db.MappedBricks.FirstOrDefaultAsync(m => m.LegoPartNum == partNum),
    //     "bluebrixx" => await db.MappedBricks.FirstOrDefaultAsync(m => m.BluebrixxPartNum == partNum),
    //     "cada" => await db.MappedBricks.FirstOrDefaultAsync(m => m.CadaPartNum == partNum),
    //     "pantasy" => await db.MappedBricks.FirstOrDefaultAsync(m => m.PantasyPartNum == partNum),
    //     "mould king" or "mouldking" => await db.MappedBricks.FirstOrDefaultAsync(m => m.MouldKingPartNum == partNum),
    //     _ => await db.MappedBricks.FirstOrDefaultAsync(m => m.UnknownPartNum == partNum)
    // };

    if (existingBrick != null)
    {
        // Fall: Teil existiert bereits. 
        // Wir markieren den Request als approved, erstellen aber KEINEN neuen Brick.
        request.Status = NewItemRequestStatus.Approved;
        request.ApprovedByUserId = adminUserId;
        request.ApprovedAt = DateTime.UtcNow;
        request.Name = finalName; // Ggf. Namen im Request anpassen

        await db.SaveChangesAsync();

        await _notificationService.AddNotificationAsync(
            request.RequestedByUserId,
            "Item already exists",
            $"Your request for {request.Brand} ({partNum}) was approved because it already exists in our database.",
            "NewItemRequest",
            request.Id
        );
        return; // WICHTIG: Hier abbrechen, damit unten kein neuer MappedBrick erzeugt wird
    }

    // --- NORMALER FLOW (Neuer Brick) ---
    request.Name = finalName;
    request.Status = NewItemRequestStatus.Approved;
    request.ApprovedByUserId = adminUserId;
    request.ApprovedAt = DateTime.UtcNow;

    var mappedBrick = new MappedBrick
    {
        Name = finalName,
        Uuid = request.Uuid,
    };

    // Brand-Zuweisung (wie gehabt)
    switch (brand)
    {
        case "lego":
            mappedBrick.LegoName = finalName;
            mappedBrick.LegoPartNum = partNum;
            break;
        case "bluebrixx":
            mappedBrick.BluebrixxName = finalName;
            mappedBrick.BluebrixxPartNum = partNum;
            break;
        case "cada":
            mappedBrick.CadaName = finalName;
            mappedBrick.CadaPartNum = partNum;
            break;
        case "pantasy":
            mappedBrick.PantasyName = finalName;
            mappedBrick.PantasyPartNum = partNum;
            break;
        case "mould king":
        case "mouldking":
            mappedBrick.MouldKingName = finalName;
            mappedBrick.MouldKingPartNum = partNum;
            break;
        default:
            mappedBrick.UnknownName = finalName;
            mappedBrick.UnknownPartNum = partNum;
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

        public async Task<bool> DoesSetExistAsync(string brand, string setNo, string setName)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            // 1. Prüfe in veröffentlichten Sets (ItemSets)
            var existsInItemSets = await db.ItemSets.AnyAsync(s => 
                s.Brand == brand && (s.SetNum == setNo || s.Name == setName));
            
            if (existsInItemSets) return true;

            // 2. Prüfe in offenen/pending Requests (NewSetRequests)
            var existsInRequests = await db.NewSetRequests.AnyAsync(r => 
                r.Brand == brand && 
                (r.SetNo == setNo || r.SetName == setName) && 
                r.Status == NewSetRequestStatus.Pending);

            return existsInRequests;
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

        public async Task<bool> DoesItemExistAsync(string brand, string? partNum, string name)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var brandLower = brand?.Trim().ToLower();

            // Wenn PartNum vorhanden ist, prüfe nach PartNum (hat Priorität)
            if (!string.IsNullOrWhiteSpace(partNum))
            {
                var existsByPartNum = brandLower switch
                {
                    "lego" => await db.MappedBricks.AnyAsync(m => m.LegoPartNum == partNum),
                    "bluebrixx" => await db.MappedBricks.AnyAsync(m => m.BluebrixxPartNum == partNum),
                    "cada" => await db.MappedBricks.AnyAsync(m => m.CadaPartNum == partNum),
                    "pantasy" => await db.MappedBricks.AnyAsync(m => m.PantasyPartNum == partNum),
                    "mould king" or "mouldking" => await db.MappedBricks.AnyAsync(m => m.MouldKingPartNum == partNum),
                    _ => await db.MappedBricks.AnyAsync(m => m.UnknownPartNum == partNum)
                };
                if (existsByPartNum) return true;
            }

            // Falls keine PartNum oder nicht gefunden, prüfe nach Name
            return brandLower switch
            {
                "lego" => await db.MappedBricks.AnyAsync(m => m.LegoName == name),
                "bluebrixx" => await db.MappedBricks.AnyAsync(m => m.BluebrixxName == name),
                "cada" => await db.MappedBricks.AnyAsync(m => m.CadaName == name),
                "pantasy" => await db.MappedBricks.AnyAsync(m => m.PantasyName == name),
                "mould king" or "mouldking" => await db.MappedBricks.AnyAsync(m => m.MouldKingName == name),
                _ => await db.MappedBricks.AnyAsync(m => m.UnknownName == name)
            };
        }

        // --- ITEM IMAGE REQUESTS ---
        public async Task<ItemImageRequest> CreateItemImageRequestAsync(int mappedBrickId, string userId, IBrowserFile imageFile)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var brick = await db.MappedBricks.FindAsync(mappedBrickId);
            if (brick == null)
                throw new ArgumentException("MappedBrick not found");

            // Falls keine UUID vorhanden, generieren und speichern
            if (string.IsNullOrWhiteSpace(brick.Uuid))
            {
                brick.Uuid = Guid.NewGuid().ToString();
                await db.SaveChangesAsync();
                _logger.LogInformation("Generated UUID {Uuid} for MappedBrick {BrickId}", brick.Uuid, brick.Id);
            }

            // Bild in pending/ speichern
            var pendingImagePath = await _imageService.SaveResizedItemImageAsync(imageFile, "pending", null, brick.Uuid);

            if (string.IsNullOrWhiteSpace(pendingImagePath))
                throw new InvalidOperationException("Failed to save image");

            var request = new ItemImageRequest
            {
                MappedBrickId = mappedBrickId,
                RequestedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = ItemImageRequestStatus.Pending,
                TempImagePath = pendingImagePath
            };

            db.ItemImageRequests.Add(request);
            await db.SaveChangesAsync();

            _logger.LogInformation("ItemImageRequest created with Id={Id} for MappedBrick={BrickId}, image stored at {Path}", request.Id, mappedBrickId, pendingImagePath);
            return request;
        }

        public async Task<List<ItemImageRequest>> GetOpenItemImageRequestsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.ItemImageRequests
                .Include(r => r.MappedBrick)
                .Include(r => r.RequestedByUser)
                .Where(r => r.Status == ItemImageRequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ItemImageRequest>> GetItemImageRequestsByUserAsync(string userId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.ItemImageRequests
                .Include(r => r.MappedBrick)
                .Include(r => r.ApprovedByUser)
                .Where(r => r.RequestedByUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task ApproveItemImageRequestAsync(int requestId, string adminUserId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.ItemImageRequests
                .Include(r => r.MappedBrick)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.Status != ItemImageRequestStatus.Pending)
                return;

            var brick = request.MappedBrick;
            if (brick == null || string.IsNullOrWhiteSpace(brick.Uuid))
            {
                _logger.LogError("ItemImageRequest {RequestId} has no valid brick or UUID", requestId);
                return;
            }

            // Bild von pending/ nach new/ verschieben
            var finalPath = await _imageService.MoveImageAsync(request.TempImagePath, "new", null, brick.Uuid);

            if (string.IsNullOrWhiteSpace(finalPath))
            {
                _logger.LogError("Failed to move image for ItemImageRequest {RequestId}", requestId);
                return;
            }

            request.Status = ItemImageRequestStatus.Approved;
            request.ApprovedByUserId = adminUserId;
            request.ApprovedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            await _notificationService.AddNotificationAsync(
                request.RequestedByUserId,
                "Item Image Approved",
                $"Your image for '{brick.Name}' has been approved!",
                "ItemImageRequest",
                request.Id
            );

            _logger.LogInformation("ItemImageRequest {RequestId} approved and image moved from pending to new: {FinalPath}", requestId, finalPath);
        }

        public async Task RejectItemImageRequestAsync(int requestId, string adminUserId, string reason)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.ItemImageRequests
                .Include(r => r.MappedBrick)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.Status != ItemImageRequestStatus.Pending)
                return;

            // Pending-Bild löschen
            if (!string.IsNullOrWhiteSpace(request.TempImagePath))
            {
                await _imageService.DeleteImageAsync(request.TempImagePath);
                _logger.LogInformation("Deleted pending image at {Path} for rejected request {RequestId}", request.TempImagePath, requestId);
            }

            request.Status = ItemImageRequestStatus.Rejected;
            request.ReasonRejected = reason;

            await db.SaveChangesAsync();

            await _notificationService.AddNotificationAsync(
                request.RequestedByUserId,
                "Item Image Rejected",
                $"Your image request was rejected: {reason}",
                "ItemImageRequest",
                request.Id
            );

            _logger.LogInformation("ItemImageRequest {RequestId} rejected", requestId);
        }
    }
}
