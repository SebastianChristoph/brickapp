using brickapp.Data;
using brickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace brickapp.Data.Services
{
public class InventoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly UserService _userService;

    public InventoryService(IDbContextFactory<AppDbContext> dbFactory, UserService userService)
    {
        _dbFactory = dbFactory;
        _userService = userService;
    }

    public async Task<bool> DeleteAllInventoryItemsAsync()
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return false;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var items = await db.InventoryItems.Where(i => i.AppUserId == user.Id).ToListAsync();
        if (!items.Any())
            return false;

        db.InventoryItems.RemoveRange(items);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddMockItemsToInventoryAsync(int mockId)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return false;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var mock = await db.Mocks.Include(m => m.Items).FirstOrDefaultAsync(m => m.Id == mockId);
        if (mock == null || mock.Items == null || !mock.Items.Any())
            return false;

        var mockType = mock.MockType?.ToLower();
        var brand = (mockType == "bricklink" || mockType == "rebrickable") ? "Lego" : "Mock";

        foreach (var item in mock.Items)
        {
            if (item.MappedBrickId == null || item.BrickColorId == null || item.Quantity <= 0)
                continue;

            var success = await AddInventoryItemAsync(item.MappedBrickId.Value, item.BrickColorId.Value, brand, item.Quantity);
            if (!success)
                return false;
        }

        return true;
    }

    public async Task<List<InventoryItem>> GetCurrentUserInventoryAsync()
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return new List<InventoryItem>();

        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.InventoryItems
            .AsNoTracking()
            .Include(i => i.MappedBrick)
            .Include(i => i.BrickColor)
            .Where(i => i.AppUserId == user.Id)
            .OrderBy(i => i.MappedBrick.Name)
            .ThenBy(i => i.BrickColor.Name)
            .ToListAsync();
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(int itemId)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return null;

        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.InventoryItems
            .Include(i => i.MappedBrick)
            .Include(i => i.BrickColor)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.AppUserId == user.Id);
    }

    public async Task<bool> UpdateInventoryItemAsync(int itemId, int newQuantity)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null || newQuantity <= 0)
            return false;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var item = await db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.AppUserId == user.Id);

        if (item == null)
            return false;

        item.Quantity = newQuantity;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteInventoryItemAsync(int itemId)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return false;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var item = await db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.AppUserId == user.Id);

        if (item == null)
            return false;

        db.InventoryItems.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddInventoryItemAsync(int mappedBrickId, int brickColorId, string brand, int quantity)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null || quantity <= 0)
            return false;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var brick = await db.MappedBricks.FirstOrDefaultAsync(b => b.Id == mappedBrickId);
        var color = await db.BrickColors.FirstOrDefaultAsync(c => c.Id == brickColorId);

        if (brick == null || color == null)
            return false;

        var existing = await db.InventoryItems
            .FirstOrDefaultAsync(i =>
                i.AppUserId == user.Id &&
                i.MappedBrickId == mappedBrickId &&
                i.BrickColorId == brickColorId &&
                i.Brand == brand);

        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            db.InventoryItems.Add(new InventoryItem
            {
                AppUserId = user.Id,
                MappedBrickId = mappedBrickId,
                BrickColorId = brickColorId,
                Brand = brand,
                Quantity = quantity
            });
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddSetBricksToInventoryAsync(int itemSetId)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return false;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var setBricks = await db.ItemSetBricks
            .Where(sb => sb.ItemSetId == itemSetId)
            .ToListAsync();

        if (!setBricks.Any())
            return false;

        foreach (var setBrick in setBricks)
        {
            var success = await AddInventoryItemAsync(setBrick.MappedBrickId, setBrick.BrickColorId, "Lego", setBrick.Quantity);
            if (!success)
                return false;
        }

        return true;
    }
}
}