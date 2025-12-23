using brickapp.Data;
using brickapp.Data.Entities;
using brickapp.Data.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace brickapp.Data.Services
{
    public class MappedBrickService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IImageStorage _storage;

        public MappedBrickService(IDbContextFactory<AppDbContext> dbFactory, IImageStorage storage)
        {
            _dbFactory = dbFactory;
            _storage = storage;
        }

        public async Task UpdateMappingAsync(int brickId, string brand, string mappingName, string mappingItemId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var brick = await db.MappedBricks.FirstOrDefaultAsync(b => b.Id == brickId);
            if (brick == null) return;

            switch (brand)
            {
                case "BlueBrixx":
                    brick.BluebrixxName = mappingName;
                    brick.BluebrixxPartNum = mappingItemId;
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

            await db.SaveChangesAsync();
        }

        public async Task<List<MappedBrick>> GetAllMappedBricksAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.MappedBricks
                .Include(b => b.MappingRequests)
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

       public async Task<(List<MappedBrick> Items, int TotalCount)> GetPaginatedMappedBricksAsync(
    int pageNumber,
    int pageSize = 25,
    string? searchText = null,
    bool onlyMapped = false)
{
    await using var db = await _dbFactory.CreateDbContextAsync();

    var query = db.MappedBricks
        .Include(b => b.MappingRequests)
        .AsNoTracking();

    if (onlyMapped)
        query = query.Where(b => b.HasAtLeastOneMapping);

    if (!string.IsNullOrWhiteSpace(searchText) && searchText.Length >= 3)
    {
        var normalized = searchText.Replace(" ", "").ToLower();
        query = query.Where(b =>
            (b.Name != null && b.Name.Replace(" ", "").ToLower().Contains(normalized)) ||
            (b.LegoPartNum != null && b.LegoPartNum.Replace(" ", "").ToLower().Contains(normalized)) ||
            (b.LegoName != null && b.LegoName.Replace(" ", "").ToLower().Contains(normalized)) ||
            (b.BluebrixxName != null && b.BluebrixxName.Replace(" ", "").ToLower().Contains(normalized)) ||
            (b.CadaName != null && b.CadaName.Replace(" ", "").ToLower().Contains(normalized)) ||
            (b.PantasyName != null && b.PantasyName.Replace(" ", "").ToLower().Contains(normalized)) ||
            (b.MouldKingName != null && b.MouldKingName.Replace(" ", "").ToLower().Contains(normalized)) ||
            (b.UnknownName != null && b.UnknownName.Replace(" ", "").ToLower().Contains(normalized)) ||
            b.Id.ToString().Contains(normalized)
        );
    }

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


        public async Task<List<MappedBrick>> SearchLegoPartByNumberAsync(string legoPartNum, int maxResults = 10)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.MappedBricks
                .AsNoTracking()
                .Where(b => b.LegoPartNum != null && b.LegoPartNum.Contains(legoPartNum))
                .OrderBy(b => b.LegoPartNum)
                .Take(maxResults)
                .ToListAsync();
        }

        public async Task<MappedBrick?> GetLegoPartByNumberAsync(string legoPartNum)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.MappedBricks
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.LegoPartNum == legoPartNum);
        }

        public async Task<List<BrickColor>> GetAllColorsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.BrickColors
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<MappedBrick?> GetRandomMappedBrickAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var count = await db.MappedBricks.CountAsync();
            if (count == 0) return null;

            // Hole mehrere zufällige Bricks und prüfe, welcher ein Bild hat
            var random = new Random();
            const int maxAttempts = 20; // Versuche max 20 mal

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var skip = random.Next(0, count);
                var brick = await db.MappedBricks
                    .Include(b => b.MappingRequests)
                    .AsNoTracking()
                    .Skip(skip)
                    .FirstOrDefaultAsync();

                if (brick != null && HasImage(brick))
                    return brick;
            }

            // Fallback: Gib irgendeinen Brick zurück (falls keiner mit Bild gefunden wurde)
            var skip2 = random.Next(0, count);
            return await db.MappedBricks
                .Include(b => b.MappingRequests)
                .AsNoTracking()
                .Skip(skip2)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteMappedBrickAsync(int brickId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var brick = await db.MappedBricks
                .Include(b => b.MappingRequests)
                .FirstOrDefaultAsync(b => b.Id == brickId);

            if (brick == null)
                return false;

            // Remove all related mapping requests first
            if (brick.MappingRequests?.Any() == true)
            {
                db.MappingRequests.RemoveRange(brick.MappingRequests);
            }

            // Remove the brick itself
            db.MappedBricks.Remove(brick);

            await db.SaveChangesAsync();
            return true;
        }

        private bool HasImage(MappedBrick brick)
        {
            // Prüfe ob Bild existiert (gleiche Logik wie ImageService.GetMappedBrickImagePath)
            
            // 1) part_images/<legoPartNum>.png
            if (!string.IsNullOrWhiteSpace(brick.LegoPartNum))
            {
                var rel = $"part_images/{brick.LegoPartNum}.png";
                if (_storage.Exists(rel))
                    return true;
            }

            // 2) part_images/new/<uuid>.png
            if (!string.IsNullOrWhiteSpace(brick.Uuid))
            {
                var rel = $"part_images/new/{brick.Uuid}.png";
                if (_storage.Exists(rel))
                    return true;
            }

            return false;
        }
    }
}