using brickapp.Data;
using brickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace brickapp.Data.Services
{

public class ItemSetService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly UserService _userService;

    public ItemSetService(IDbContextFactory<AppDbContext> dbFactory, UserService userService)
    {
        _dbFactory = dbFactory;
        _userService = userService;
    }

    public async Task<List<UserItemSet>> GetCurrentUserItemSetsAsync()
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return new List<UserItemSet>();

        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.UserItemSets
            .AsNoTracking()
            .Include(us => us.ItemSet)
                .ThenInclude(s => s.Bricks)
            .Where(us => us.AppUserId == user.Id)
            .OrderBy(us => us.ItemSet.Name)
            .ToListAsync();
    }

    public async Task<UserItemSet?> GetCurrentUserItemSetAsync(int itemSetId)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return null;

        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.UserItemSets
            .AsNoTracking()
            .Include(us => us.ItemSet)
                .ThenInclude(s => s.Bricks)
                    .ThenInclude(sb => sb.MappedBrick)
                        .ThenInclude(mb => mb.MappingRequests)
            .Include(us => us.ItemSet)
                .ThenInclude(s => s.Bricks)
                    .ThenInclude(sb => sb.BrickColor)
            .Where(us => us.AppUserId == user.Id && us.ItemSetId == itemSetId)
            .FirstOrDefaultAsync();
    }

    public async Task<(List<ItemSet> Items, int TotalCount)> GetPaginatedItemSetsAsync(int pageNumber, int pageSize = 25)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = db.ItemSets.AsNoTracking().OrderBy(s => s.Name);
        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<ItemSet?> GetItemSetByIdAsync(int itemSetId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.ItemSets
            .AsNoTracking()
            .Include(s => s.Bricks)
                .ThenInclude(sb => sb.MappedBrick)
                    .ThenInclude(mb => mb.MappingRequests)
            .Include(s => s.Bricks)
                .ThenInclude(sb => sb.BrickColor)
            .FirstOrDefaultAsync(s => s.Id == itemSetId);
    }
}
}