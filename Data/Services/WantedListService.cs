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

        // Schnelles DTO für Overview (keine Includes nötig)
        public record WantedListSummary(int Id, string Name, string? Source, int ItemCount);

        public WantedListService(IDbContextFactory<AppDbContext> factory, UserService userService)
        {
            _factory = factory;
            _userService = userService;
        }

        /// <summary>
        /// Einheitliche Normalisierung von Source/Format Strings.
        /// - trim + lower
        /// - "csv"/"xml" entfernen
        /// - bekannte Aliase ("bricklink csv", "rebrickable xml", etc.) vereinheitlichen
        /// </summary>
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
        /// Heavy: Lädt WantedLists inkl. Items + MappedBrick + BrickColor.
        /// Verwenden für Details, nicht für Overview.
        /// </summary>
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

            // Validierungs-Caches (IDs)
            var validBrickIds = await db.MappedBricks.AsNoTracking().Select(b => b.Id).ToHashSetAsync();
            var validColorIds = await db.BrickColors.AsNoTracking().Select(c => c.Id).ToHashSetAsync();

            // Items deduplizieren
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

            // Gültige Items filtern
            var valid = groupedMapped
                .Where(i => validBrickIds.Contains(i.MappedBrickId) && validColorIds.Contains(i.ColorId))
                .ToList();

            var wantedList = new WantedList
            {
                Name = model.Name,
                AppUserId = user.Id.ToString(),
                Source = NormalizeSource(model.Source),
                Items = new List<WantedListItem>(),
                MissingItems = new List<MissingItem>()
            };

            // Validierte Mapped Items hinzufügen
            foreach (var item in valid)
            {
                wantedList.Items.Add(new WantedListItem
                {
                    MappedBrickId = item.MappedBrickId,
                    BrickColorId = item.ColorId,
                    Quantity = item.Quantity
                });
            }

            // Missing Items mappen & deduplizieren
            if (model.UnmappedRows != null && model.UnmappedRows.Any())
            {
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
