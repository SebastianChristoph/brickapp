using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using brickapp.Data;
using brickapp.Data.DTOs;
using brickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace brickapp.Data.Services
{
    public class WantedListService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly UserService _userService;
        private readonly ILogger<WantedListService> _logger;
        private readonly ImageService _imageService;

        // Schnelles DTO für Overview (keine Includes nötig)
        public record WantedListSummary(int Id, string Name, string? Source, int ItemCount);
        private readonly RebrickableApiService _rebrickableApi;

        public WantedListService(IDbContextFactory<AppDbContext> factory, UserService userService, RebrickableApiService rebrickableApi, ILogger<WantedListService> logger, ImageService imageService)
        {
            _factory = factory;
            _userService = userService;
            _rebrickableApi = rebrickableApi;
            _logger = logger;
            _imageService = imageService;
        }

        public static string NormalizeSource(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return "manual";

            var s = source.Trim().ToLower();

            // Dateiendungen/Formatwörter entfernen
            s = s.Replace("csv", "").Replace("xml", "").Trim();

            // Aliases/Enthält-Checks
            if (s.Contains("bricklink")) return "bricklink";
            if (s.Contains("rebrickable")) return "rebrickable";

            return string.IsNullOrWhiteSpace(s) ? "manual" : s;
        }

        public async Task<Dictionary<(int BrickId, int ColorId), List<string>>> GetWantedListNamesByBrickAndColorAsync(int userId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var user = await _userService.GetCurrentUserAsync();
            if (user == null || user.Id != userId)
                return new();

            var lists = await db.WantedLists
                .AsNoTracking()
                .Include(wl => wl.Items)
                .Where(wl => wl.AppUserId == user.Id.ToString())
                .ToListAsync();

            var dict = new Dictionary<(int, int), HashSet<string>>();

            foreach (var wl in lists)
            {
                if (wl.Items == null) continue;

                foreach (var item in wl.Items)
                {
                    var key = (item.MappedBrickId, item.BrickColorId);

                    if (!dict.TryGetValue(key, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        dict[key] = set;
                    }

                    set.Add(wl.Name); // keine doppelten Namen
                }
            }

            return dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OrderBy(x => x).ToList());
        }

         /// <summary>
        /// Fast: Overview nur mit Name/Source/Count (ohne Includes).
        /// </summary>
        public async Task<List<WantedListSummary>> GetCurrentUserWantedListSummariesAsync()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return new();

            await using var db = await _factory.CreateDbContextAsync();

            return await db.WantedLists
                .AsNoTracking()
                .Where(w => w.AppUserId == user.Id.ToString())
                .OrderByDescending(w => w.Id)
                .Select(w => new WantedListSummary(
                    w.Id,
                    w.Name,
                    w.Source,
                    w.Items.Count()
                ))
                .ToListAsync();
        }

      
        public async Task<List<WantedList>> GetCurrentUserWantedListsAsync()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return new();

            await using var db = await _factory.CreateDbContextAsync();

            return await db.WantedLists
                .AsNoTracking()
                .Include(w => w.Items)
                    .ThenInclude(i => i.MappedBrick)
                .Include(w => w.Items)
                    .ThenInclude(i => i.BrickColor)
                .Where(w => w.AppUserId == user.Id.ToString())
                .ToListAsync();
        }

     public async Task<List<MissingItem>> GetResolvableMissingItemsAsync(int wantedListId)
{
    await using var db = await _factory.CreateDbContextAsync();
    
    // Wir laden die Liste inkl. MissingItems
    var wantedList = await db.WantedLists
        .Include(w => w.MissingItems)
        .FirstOrDefaultAsync(w => w.Id == wantedListId);

    if (wantedList == null || !wantedList.MissingItems.Any()) 
        return new List<MissingItem>();

    // Alle externen PartNums aus der Liste sammeln
    var partNums = wantedList.MissingItems
        .Select(m => m.ExternalPartNum)
        .Distinct()
        .ToList();
    
    // Prüfen, welche davon nun in MappedBricks existieren
    var existingLegoPartNums = await db.MappedBricks
        .Where(mb => mb.LegoPartNum != null && partNums.Contains(mb.LegoPartNum))
        .Select(mb => mb.LegoPartNum)
        .ToListAsync();

    // Nur die MissingItems zurückgeben, die jetzt gemappt werden können
    return wantedList.MissingItems
        .Where(m => existingLegoPartNums.Contains(m.ExternalPartNum))
        .ToList();
}
      public async Task ResolveMissingItemsAsync(int wantedListId, List<int> missingItemIds)
{
    await using var db = await _factory.CreateDbContextAsync();
    
    // Hier laden wir die Liste mit BEIDEN Collections (wichtig für die Migration)
    var wantedList = await db.WantedLists
        .Include(w => w.Items)
        .Include(w => w.MissingItems)
        .FirstOrDefaultAsync(w => w.Id == wantedListId);

    if (wantedList == null) return;

    // Nur die MissingItems filtern, die der User auflösen will
    var toResolve = wantedList.MissingItems
        .Where(m => missingItemIds.Contains(m.Id))
        .ToList();

    foreach (var missing in toResolve)
    {
        var brick = await db.MappedBricks
            .FirstOrDefaultAsync(mb => mb.LegoPartNum == missing.ExternalPartNum);

        if (brick != null)
        {
            // Zu echten Items hinzufügen
            wantedList.Items.Add(new WantedListItem
            {
                MappedBrickId = brick.Id,
                BrickColorId = missing.ExternalColorId ?? 0,
                Quantity = missing.Quantity
            });

            // Aus MissingItems entfernen
            wantedList.MissingItems.Remove(missing);
        }
    }

    await db.SaveChangesAsync();
}

        public async Task<bool> CreateWantedListAsync(NewWantedListModel model)
        {
            var id = await CreateWantedListAndReturnIdAsync(model);
            return id > 0;
        }

        public async Task<bool> AddItemsToWantedListAsync(int wantedListId, IEnumerable<NewWantedListItemModel> itemsToAdd)
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return false;

            await using var db = await _factory.CreateDbContextAsync();

            var list = await db.WantedLists
                .Include(w => w.Items)
                .FirstOrDefaultAsync(w => w.Id == wantedListId && w.AppUserId == user.Id.ToString());

            if (list == null)
                return false;

            foreach (var item in itemsToAdd ?? Enumerable.Empty<NewWantedListItemModel>())
            {
                if (item.MappedBrickId <= 0 || item.ColorId <= 0 || item.Quantity <= 0)
                    continue;

                var existing = list.Items.FirstOrDefault(x =>
                    x.MappedBrickId == item.MappedBrickId &&
                    x.BrickColorId == item.ColorId);

                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                }
                else
                {
                    list.Items.Add(new WantedListItem
                    {
                        MappedBrickId = item.MappedBrickId,
                        BrickColorId = item.ColorId,
                        Quantity = item.Quantity
                    });
                }
            }

            await db.SaveChangesAsync();
            return true;
        }

    public async Task<int> CreateWantedListAndReturnIdAsync(NewWantedListModel model)
{
    var user = await _userService.GetCurrentUserAsync();
    if (user == null) return 0;

    await using var db = await _factory.CreateDbContextAsync();

    // 1. Validierungs-Caches (IDs) laden für Performance
    var validBrickIds = await db.MappedBricks.AsNoTracking().Select(b => b.Id).ToHashSetAsync();
    var validColorIds = await db.BrickColors.AsNoTracking().Select(c => c.Id).ToHashSetAsync();

    // 2. Mapped Items deduplizieren und validieren
    var groupedMapped = (model.Items ?? new List<NewWantedListItemModel>())
        .Where(i => i.MappedBrickId > 0 && i.ColorId > 0 && i.Quantity > 0)
        .GroupBy(i => (i.MappedBrickId, i.ColorId))
        .Select(g => new NewWantedListItemModel
        {
            MappedBrickId = g.Key.MappedBrickId,
            ColorId = g.Key.ColorId,
            Quantity = g.Sum(x => x.Quantity)
        })
        .ToList();

    var validItems = groupedMapped
        .Where(i => validBrickIds.Contains(i.MappedBrickId) && validColorIds.Contains(i.ColorId))
        .ToList();

    // 3. WantedList Objekt initialisieren
    var wantedList = new WantedList
    {
        Name = model.Name,
        AppUserId = user.Id.ToString(),
        Source = NormalizeSource(model.Source),
        Items = new List<WantedListItem>(),
        MissingItems = new List<MissingItem>()
    };

    // 4. Validierte Items zur Liste hinzufügen
    foreach (var item in validItems)
    {
        wantedList.Items.Add(new WantedListItem
        {
            MappedBrickId = item.MappedBrickId,
            BrickColorId = item.ColorId,
            Quantity = item.Quantity
        });
    }

    // 5. Unmapped Rows verarbeiten: MissingItems & NewItemRequests
    if (model.UnmappedRows != null && model.UnmappedRows.Any())
    {
        // A) NewItemRequests erstellen (Dedupliziert nach PartNum)
        var distinctPartNums = model.UnmappedRows
            .Where(r => !string.IsNullOrEmpty(r.PartNum))
            .Select(r => r.PartNum!)
            .Distinct()
            .ToList();

        // WICHTIG: Alle existierenden Requests EINMAL laden (Performance + Race Condition Vermeidung)
        var existingRequestPartNums = await db.NewItemRequests
            .Where(r => r.Brand == "Lego" 
                     && distinctPartNums.Contains(r.PartNum)
                     && (r.Status == NewItemRequestStatus.Pending || r.Status == NewItemRequestStatus.Approved))
            .Select(r => r.PartNum)
            .ToListAsync();

        foreach (var partNum in distinctPartNums)
        {
            // Prüfen ob Request bereits existiert (Pending oder Approved)
            if (existingRequestPartNums.Contains(partNum))
            {
                _logger.LogInformation("ℹ️ NewItemRequest für {PartNum} existiert bereits (Pending/Approved) - wird übersprungen.", partNum);
                continue;
            }

            // API Call um Name und Bild zu holen
            var partInfo = await _rebrickableApi.GetLegoItemNameByPartNumber(partNum);

            if (partInfo != null && !string.IsNullOrWhiteSpace(partInfo.Name))
            {
                var newUuid = Guid.NewGuid().ToString();
                var newRequest = new NewItemRequest
                {
                    Uuid = newUuid,
                    Brand = "Lego",
                    PartNum = partNum,
                    Name = partInfo.Name,
                    RequestedByUserId = user.Uuid,
                    CreatedAt = DateTime.UtcNow,
                    Status = NewItemRequestStatus.Pending
                };

                db.NewItemRequests.Add(newRequest);

                // BILD SPEICHERN
                if (!string.IsNullOrWhiteSpace(partInfo.ImageUrl))
                {
                    await _imageService.DownloadAndSaveItemImageAsync(
                        partInfo.ImageUrl, 
                        "Lego", 
                        partNum, 
                        newUuid);
                }

                _logger.LogInformation("✅ NewItemRequest und Bild für {PartNum} erstellt.", partNum);
            }
        }

        // B) MissingItems für die WantedList erstellen (Dedupliziert nach PartNum & Farbe)
        // Das passiert immer, damit der User in seiner Liste sieht, was fehlt.
        wantedList.MissingItems = model.UnmappedRows
            .GroupBy(u => new { u.PartNum, u.ColorId })
            .Select(g => new MissingItem
            {
                ExternalPartNum = g.Key.PartNum,
                ExternalColorId = g.Key.ColorId,
                Quantity = g.Sum(x => x.Quantity)
            })
            .ToList();
    }

    // 6. Alles in einem Rutsch speichern (Atomarität)
    db.WantedLists.Add(wantedList);
    await db.SaveChangesAsync();

    return wantedList.Id;
}
        public async Task<bool> DeleteWantedListItemAsync(int wantedListItemId)
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return false;

            await using var db = await _factory.CreateDbContextAsync();

            // nur löschen, wenn Item zu einer Liste des aktuellen Users gehört
            var item = await db.WantedListItems
                .Include(i => i.WantedList)
                .FirstOrDefaultAsync(i =>
                    i.Id == wantedListItemId &&
                    i.WantedList != null &&
                    i.WantedList.AppUserId == user.Id.ToString());

            if (item == null)
                return false;

            db.WantedListItems.Remove(item);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateWantedListItemAsync(int wantedListItemId, int newQuantity, int? newColorId = null)
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null || newQuantity <= 0)
                return false;

            await using var db = await _factory.CreateDbContextAsync();

            var item = await db.WantedListItems
                .Include(i => i.WantedList)
                .FirstOrDefaultAsync(i =>
                    i.Id == wantedListItemId &&
                    i.WantedList != null &&
                    i.WantedList.AppUserId == user.Id.ToString());

            if (item == null)
                return false;

            item.Quantity = newQuantity;

            if (newColorId.HasValue && newColorId.Value > 0)
            {
                item.BrickColorId = newColorId.Value;
            }

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<WantedList?> GetWantedListByIdAsync(int wantedListId)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.WantedLists
                .AsNoTracking()
                .Include(w => w.Items)
                    .ThenInclude(i => i.MappedBrick)
                .Include(w => w.Items)
                    .ThenInclude(i => i.BrickColor)
                .Include(w => w.MissingItems)
                .FirstOrDefaultAsync(w => w.Id == wantedListId);
        }

        public async Task<bool> DeleteWantedListAsync(int wantedListId)
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return false;

            await using var db = await _factory.CreateDbContextAsync();

            var wantedList = await db.WantedLists
                .Include(w => w.Items)
                .FirstOrDefaultAsync(w => w.Id == wantedListId && w.AppUserId == user.Id.ToString());

            if (wantedList == null)
                return false;

            db.WantedLists.Remove(wantedList);
            await db.SaveChangesAsync();
            return true;
        }
    }
}
