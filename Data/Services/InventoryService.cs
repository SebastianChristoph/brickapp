   
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class InventoryService
{
    public InventoryService(AppDbContext db, UserService userService)
    {
        _db = db;
        _userService = userService;
    }
    

        private readonly AppDbContext _db;
    private readonly UserService _userService;



    /// Löscht das gesamte Inventar des aktuell eingeloggten Users
    public async Task<bool> DeleteAllInventoryItemsAsync()
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return false;

        var items = await _db.InventoryItems.Where(i => i.AppUserId == user.Id).ToListAsync();
        if (!items.Any())
            return false;

        _db.InventoryItems.RemoveRange(items);
        await _db.SaveChangesAsync();
        return true;
    }

     public async Task<bool> AddMockItemsToInventoryAsync(int mockId)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return false;

        var mock = await _db.Mocks.Include(m => m.Items).FirstOrDefaultAsync(m => m.Id == mockId);
        if (mock == null || mock.Items == null || !mock.Items.Any())
            return false;

        // Wenn bricklink-Mock, Brand auf "Lego" setzen
        var brand = (mock.MockType?.ToLower() == "bricklink") ? "Lego" : "Mock";

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

  

    /// Holt alle InventoryItems des aktuell eingeloggten Users.
    public async Task<List<InventoryItem>> GetCurrentUserInventoryAsync()
    {
        // UserService kümmert sich darum, das Token aus dem localStorage zu lesen
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
        {
            return new List<InventoryItem>();
        }

        return await _db.InventoryItems
            .AsNoTracking()
            .Include(i => i.MappedBrick)
            .Include(i => i.BrickColor)
            .Where(i => i.AppUserId == user.Id)
            .OrderBy(i => i.MappedBrick.Name)
            .ThenBy(i => i.BrickColor.Name)
            .ToListAsync();
    }

    /// InventoryItem nach ID abrufen
    public async Task<InventoryItem?> GetInventoryItemAsync(int itemId)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return null;

        return await _db.InventoryItems
            .Include(i => i.MappedBrick)
            .Include(i => i.BrickColor)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.AppUserId == user.Id);
    }

    /// InventoryItem aktualisieren
    public async Task<bool> UpdateInventoryItemAsync(int itemId, int newQuantity)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null || newQuantity <= 0)
            return false;

        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.AppUserId == user.Id);

        if (item == null)
            return false;

        item.Quantity = newQuantity;
        await _db.SaveChangesAsync();
        return true;
    }

    /// InventoryItem löschen
    public async Task<bool> DeleteInventoryItemAsync(int itemId)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return false;

        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.AppUserId == user.Id);

        if (item == null)
            return false;

        _db.InventoryItems.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    /// Neues InventoryItem hinzufügen
    public async Task<bool> AddInventoryItemAsync(int mappedBrickId, int brickColorId, string brand, int quantity)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null || quantity <= 0)
            return false;

        // Prüfe ob Brick und Farbe existieren
        var brick = await _db.MappedBricks.FirstOrDefaultAsync(b => b.Id == mappedBrickId);
        var color = await _db.BrickColors.FirstOrDefaultAsync(c => c.Id == brickColorId);

        if (brick == null || color == null)
            return false;

        // Prüfe ob bereits vorhanden -> Update
        var existing = await _db.InventoryItems
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
            var newItem = new InventoryItem
            {
                AppUserId = user.Id,
                MappedBrickId = mappedBrickId,
                BrickColorId = brickColorId,
                Brand = brand,
                Quantity = quantity
            };
            _db.InventoryItems.Add(newItem);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    /// Alle ItemSetBricks eines Sets zum Inventory hinzufügen
    public async Task<bool> AddSetBricksToInventoryAsync(int itemSetId)
    {
        var user = await _userService.GetCurrentUserAsync();
        if (user == null)
            return false;

        // Lade alle ItemSetBricks des Sets
        var setBricks = await _db.ItemSetBricks
            .Where(sb => sb.ItemSetId == itemSetId)
            .ToListAsync();

        if (!setBricks.Any())
            return false;

        // Für jedes SetBrick: Lego als Brand verwenden und zum Inventory hinzufügen
        foreach (var setBrick in setBricks)
        {
            var success = await AddInventoryItemAsync(setBrick.MappedBrickId, setBrick.BrickColorId, "Lego", setBrick.Quantity);
            if (!success)
                return false;
        }

        return true;
    }
}
