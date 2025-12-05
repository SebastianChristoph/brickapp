using brickisbrickapp.Data;
using brickisbrickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace brickisbrickapp.Services;

public class ItemSetService
{
    private readonly AppDbContext _db;
    private readonly UserService _userService;

    public ItemSetService(AppDbContext db, UserService userService)
    {
        _db = db;
        _userService = userService;
    }

    public async Task<List<UserItemSet>> GetCurrentUserItemSetsAsync()
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
        {
            return new List<UserItemSet>();
        }

        return await _db.UserItemSets
            .AsNoTracking()
            .Include(us => us.ItemSet)
                .ThenInclude(s => s.Bricks)
            .Where(us => us.AppUserId == user.Id)
            .OrderBy(us => us.ItemSet.Name)
            .ToListAsync();
    }

    /// Details für ein bestimmtes Set des aktuellen Users
    public async Task<UserItemSet?> GetCurrentUserItemSetAsync(int itemSetId)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return null;

        return await _db.UserItemSets
            .AsNoTracking()
            .Include(us => us.ItemSet)
                .ThenInclude(s => s.Bricks)
                    .ThenInclude(sb => sb.MappedBrick)
            .Include(us => us.ItemSet)
                .ThenInclude(s => s.Bricks)
                    .ThenInclude(sb => sb.BrickColor)   // 🔥 Farbe mitziehen
            .Where(us => us.AppUserId == user.Id && us.ItemSetId == itemSetId)
            .FirstOrDefaultAsync();
    }

    /// Alle Sets paginiert (OHNE Bricks für schnelle Übersicht)
    public async Task<(List<ItemSet> Items, int TotalCount)> GetPaginatedItemSetsAsync(int pageNumber, int pageSize = 25)
    {
        var query = _db.ItemSets.AsNoTracking().OrderBy(s => s.Name);
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// Ein spezifisches Set mit allen Details laden
    public async Task<ItemSet?> GetItemSetByIdAsync(int itemSetId)
    {
        return await _db.ItemSets
            .AsNoTracking()
            .Include(s => s.Bricks)
                .ThenInclude(sb => sb.MappedBrick)
            .Include(s => s.Bricks)
                .ThenInclude(sb => sb.BrickColor)
            .FirstOrDefaultAsync(s => s.Id == itemSetId);
    }
}
