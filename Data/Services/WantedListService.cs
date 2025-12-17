       
    using Data.DTOs;
       
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Services;

namespace Data.Services
{
    public class WantedListService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly UserService _userService;

        public WantedListService(IDbContextFactory<AppDbContext> factory, UserService userService)
        {
            _factory = factory;
            _userService = userService;
        }

         /// <summary>
        /// Gibt für jedes InventoryItem (nach BrickId und ColorId) die zugehörigen WantedLists zurück.
        /// </summary>
       public async Task<Dictionary<(int BrickId, int ColorId), List<string>>> GetWantedListNamesByBrickAndColorAsync(int userId)
{
    await using var db = await _factory.CreateDbContextAsync();
    var user = await _userService.GetCurrentUserAsync();
    if (user == null || user.Id != userId)
        return new();

    var lists = await db.WantedLists
        .Include(wl => wl.Items)
        .Where(wl => wl.AppUserId == user.Id.ToString())
        .ToListAsync();

    var dict = new Dictionary<(int, int), HashSet<string>>();

    foreach (var wl in lists)
    {
        foreach (var item in wl.Items)
        {
            var key = (item.MappedBrickId, item.BrickColorId);

            if (!dict.TryGetValue(key, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                dict[key] = set;
            }

            set.Add(wl.Name); // <- kein doppelter Name mehr
        }
    }

    // zurück in List<string>
    return dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OrderBy(x => x).ToList());
}

        public async Task<List<WantedList>> GetCurrentUserWantedListsAsync()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return new List<WantedList>();

            await using var db = await _factory.CreateDbContextAsync();

            return await db.WantedLists
                .Include(w => w.Items)
                    .ThenInclude(i => i.MappedBrick)
                .Include(w => w.Items)
                    .ThenInclude(i => i.BrickColor)
                .Where(w => w.AppUserId == user.Id.ToString())
                .ToListAsync();
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

            foreach (var item in itemsToAdd)
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
    if (user == null)
        return 0;

    await using var db = await _factory.CreateDbContextAsync();

    // Validierungs-Caches (damit nicht pro Item ein Query feuert)
    var validBrickIds = await db.MappedBricks.AsNoTracking().Select(b => b.Id).ToHashSetAsync();
    var validColorIds = await db.BrickColors.AsNoTracking().Select(c => c.Id).ToHashSetAsync();

    // Items deduplizieren (gleicher Brick+Color => Mengen addieren)
    var grouped = (model.Items ?? new List<NewWantedListItemModel>())
        .Where(i => i.MappedBrickId > 0 && i.ColorId > 0 && i.Quantity > 0)
        .GroupBy(i => (i.MappedBrickId, i.ColorId))
        .Select(g => new NewWantedListItemModel
        {
            MappedBrickId = g.Key.MappedBrickId,
            ColorId = g.Key.ColorId,
            Quantity = g.Sum(x => x.Quantity)
        })
        .ToList();

    // Ungültige FK-Werte rausfiltern
    var invalid = grouped
        .Where(i => !validBrickIds.Contains(i.MappedBrickId) || !validColorIds.Contains(i.ColorId))
        .ToList();

    var valid = grouped
        .Where(i => validBrickIds.Contains(i.MappedBrickId) && validColorIds.Contains(i.ColorId))
        .ToList();

    // Wenn du lieber "hart" fehlschlagen willst, statt skippen:
    // if (invalid.Any())
    //     throw new InvalidOperationException($"Upload contains invalid BrickColorId / MappedBrickId. Invalid items: {invalid.Count}");

    var wantedList = new WantedList
    {
        Name = model.Name,
        AppUserId = user.Id.ToString(),
        Items = new List<WantedListItem>()
    };

    foreach (var item in valid)
    {
        wantedList.Items.Add(new WantedListItem
        {
            MappedBrickId = item.MappedBrickId,
            BrickColorId = item.ColorId,
            Quantity = item.Quantity
        });
    }

    db.WantedLists.Add(wantedList);
    await db.SaveChangesAsync();

    // Optional: falls du im UI anzeigen willst "X items skipped", könntest du das loggen:
    // if (invalid.Any()) { ... }

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


        public async Task<WantedList?> GetWantedListByIdAsync(int wantedListId)
        {
            await using var db = await _factory.CreateDbContextAsync();

            return await db.WantedLists
                .Include(w => w.Items)
                    .ThenInclude(i => i.MappedBrick)
                .Include(w => w.Items)
                    .ThenInclude(i => i.BrickColor)
                .FirstOrDefaultAsync(w => w.Id == wantedListId);
        }

                    public async Task<bool> DeleteWantedListAsync(int wantedListId)
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user == null)
                    return false;

                await using var db = await _factory.CreateDbContextAsync();
                var wantedList = await db.WantedLists.Include(w => w.Items).FirstOrDefaultAsync(w => w.Id == wantedListId && w.AppUserId == user.Id.ToString());
                if (wantedList == null)
                    return false;

                db.WantedLists.Remove(wantedList);
                await db.SaveChangesAsync();
                return true;
            }
    }

    
}
