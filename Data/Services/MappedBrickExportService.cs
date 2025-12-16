using System.Text.Json;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Services.Storage;

namespace Data.Services;

public class MappedBrickExportService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IExportStorage _storage;

    private const string ExportRelPath = "exported_mappedbricks.json";

    public MappedBrickExportService(IDbContextFactory<AppDbContext> factory, IExportStorage storage)
    {
        _factory = factory;
        _storage = storage;
    }

    public string GetExportPath() => _storage.DescribeTarget($"mappedData/{ExportRelPath}");

    public async Task<int> ExportMappedBricksAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        var allBricks = await db.MappedBricks.AsNoTracking().ToListAsync();
        var bricks = allBricks.Where(b => CountNonNullMappings(b) >= 1).ToList();

        var exportList = bricks.Select(b => new ExportMappedBrick
        {
            LegoPartNum = b.LegoPartNum,
            LegoName = b.LegoName,
            Name = b.Name,
            BluebrixxPartNum = b.BluebrixxPartNum,
            BluebrixxName = b.BluebrixxName,
            CadaPartNum = b.CadaPartNum,
            CadaName = b.CadaName,
            PantasyPartNum = b.PantasyPartNum,
            PantasyName = b.PantasyName,
            MouldKingPartNum = b.MouldKingPartNum,
            MouldKingName = b.MouldKingName,
            UnknownPartNum = b.UnknownPartNum,
            UnknownName = b.UnknownName
        }).ToList();

        var json = JsonSerializer.Serialize(exportList, new JsonSerializerOptions { WriteIndented = true });

        await _storage.WriteTextAsync($"mappedData/{ExportRelPath}", "application/json", json);
        return exportList.Count;
    }

    public async Task<int> ImportMappedBricksAsync()
    {
        var json = await _storage.ReadTextAsync($"mappedData/{ExportRelPath}");
        if (string.IsNullOrWhiteSpace(json)) return 0;

        var imported = JsonSerializer.Deserialize<List<ExportMappedBrick>>(json);
        if (imported == null || imported.Count == 0) return 0;

        await using var db = await _factory.CreateDbContextAsync();

        int importedCount = 0;
        foreach (var b in imported)
        {
            if (string.IsNullOrWhiteSpace(b.LegoPartNum))
                continue;

            var existing = await db.MappedBricks.FirstOrDefaultAsync(x => x.LegoPartNum == b.LegoPartNum);
            if (existing != null)
            {
                existing.BluebrixxPartNum = b.BluebrixxPartNum;
                existing.BluebrixxName = b.BluebrixxName;
                existing.CadaPartNum = b.CadaPartNum;
                existing.CadaName = b.CadaName;
                existing.PantasyPartNum = b.PantasyPartNum;
                existing.PantasyName = b.PantasyName;
                existing.MouldKingPartNum = b.MouldKingPartNum;
                existing.MouldKingName = b.MouldKingName;
                existing.UnknownPartNum = b.UnknownPartNum;
                existing.UnknownName = b.UnknownName;
                importedCount++;
            }
        }

        await db.SaveChangesAsync();
        return importedCount;
    }

    private static int CountNonNullMappings(MappedBrick b)
    {
        int count = 0;
        if (!string.IsNullOrWhiteSpace(b.BluebrixxPartNum)) count++;
        if (!string.IsNullOrWhiteSpace(b.BluebrixxName)) count++;
        if (!string.IsNullOrWhiteSpace(b.CadaPartNum)) count++;
        if (!string.IsNullOrWhiteSpace(b.CadaName)) count++;
        if (!string.IsNullOrWhiteSpace(b.PantasyPartNum)) count++;
        if (!string.IsNullOrWhiteSpace(b.PantasyName)) count++;
        if (!string.IsNullOrWhiteSpace(b.MouldKingPartNum)) count++;
        if (!string.IsNullOrWhiteSpace(b.MouldKingName)) count++;
        if (!string.IsNullOrWhiteSpace(b.UnknownPartNum)) count++;
        if (!string.IsNullOrWhiteSpace(b.UnknownName)) count++;
        return count;
    }

    private class ExportMappedBrick
    {
        public string? LegoPartNum { get; set; }
        public string? LegoName { get; set; }
        public string Name { get; set; } = default!;
        public string? BluebrixxPartNum { get; set; }
        public string? BluebrixxName { get; set; }
        public string? CadaPartNum { get; set; }
        public string? CadaName { get; set; }
        public string? PantasyPartNum { get; set; }
        public string? PantasyName { get; set; }
        public string? MouldKingPartNum { get; set; }
        public string? MouldKingName { get; set; }
        public string? UnknownPartNum { get; set; }
        public string? UnknownName { get; set; }
    }
}
