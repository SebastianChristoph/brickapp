
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class MappedBrickService
{
    private readonly AppDbContext _db;

    public MappedBrickService(AppDbContext db)
    {
        _db = db;
    }

    public async Task UpdateMappingAsync(int brickId, string brand, string mappingName, string mappingItemId)
    {
        var brick = await _db.MappedBricks.FirstOrDefaultAsync(b => b.Id == brickId);
        if (brick == null) return;

        switch (brand)
        {
            case "BB":
                brick.BbName = mappingName;
                brick.BbPartNum = mappingItemId;
                break;
            case "Cada":
                brick.CadaName = mappingName;
                brick.CadaPartNum = mappingItemId;
                break;
            case "Pantasy":
                brick.PantasyName = mappingName;
                brick.PantasyPartNum = mappingItemId;
                break;
            case "Mould King":
                brick.MouldKingName = mappingName;
                brick.MouldKingPartNum = mappingItemId;
                break;
            case "Unknown":
                brick.UnknownName = mappingName;
                brick.UnknownPartNum = mappingItemId;
                break;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<MappedBrick>> GetAllMappedBricksAsync()
    {
        return await _db.MappedBricks
            .Include(b => b.MappingRequests)
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<(List<MappedBrick> Items, int TotalCount)> GetPaginatedMappedBricksAsync(int pageNumber, int pageSize = 25, string? searchText = null)
        {

            var query = _db.MappedBricks
                .Include(b => b.MappingRequests)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText) && searchText.Length >= 3)
            {
                var normalized = searchText.Replace(" ", "").ToLower();
                query = query.Where(b =>
                    (b.Name != null && b.Name.Replace(" ", "").ToLower().Contains(normalized)) ||
                    (b.LegoPartNum != null && b.LegoPartNum.Replace(" ", "").ToLower().Contains(normalized)) ||
                    (b.LegoName != null && b.LegoName.Replace(" ", "").ToLower().Contains(normalized)) ||
                    (b.BbName != null && b.BbName.Replace(" ", "").ToLower().Contains(normalized)) ||
                    (b.CadaName != null && b.CadaName.Replace(" ", "").ToLower().Contains(normalized)) ||
                    (b.PantasyName != null && b.PantasyName.Replace(" ", "").ToLower().Contains(normalized)) ||
                    (b.MouldKingName != null && b.MouldKingName.Replace(" ", "").ToLower().Contains(normalized)) ||
                    (b.UnknownName != null && b.UnknownName.Replace(" ", "").ToLower().Contains(normalized)) ||
                    b.Id.ToString().Contains(normalized)
                );
            }

            // Sortierung: Wenn LegoPartNum vorhanden, nach Länge und Wert, sonst nach Name
            query = query
                .OrderBy(b => string.IsNullOrEmpty(b.LegoPartNum) ? 99 : b.LegoPartNum.Length)
                .ThenBy(b => b.LegoPartNum)
                .ThenBy(b => b.Name);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

    

    /// Suche nach Lego-Part-Nummer
    public async Task<List<MappedBrick>> SearchLegoPartByNumberAsync(string legoPartNum, int maxResults = 10)
    {
        return await _db.MappedBricks
            .AsNoTracking()
            .Where(b => b.LegoPartNum != null && b.LegoPartNum.Contains(legoPartNum))
            .OrderBy(b => b.LegoPartNum)
            .Take(maxResults)
            .ToListAsync();
    }

    /// Lego-Part nach exakter Nummer
    public async Task<MappedBrick?> GetLegoPartByNumberAsync(string legoPartNum)
    {
        return await _db.MappedBricks
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.LegoPartNum == legoPartNum);
    }

    /// Alle Farben abrufen
    public async Task<List<BrickColor>> GetAllColorsAsync()
    {
        return await _db.BrickColors
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
