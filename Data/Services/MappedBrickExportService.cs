using System.Text.Json;
using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Services;

public class MappedBrickExportService
  
{
    private readonly AppDbContext _db;
    private readonly string _exportPath;
    public string GetExportPath() => _exportPath;

    public MappedBrickExportService(AppDbContext db, string exportPath)
    {
        _db = db;
        _exportPath = exportPath;
    }

    public async Task<int> ExportMappedBricksAsync()
    {
        var allBricks = await _db.MappedBricks.ToListAsync();
        var bricks = allBricks.Where(b => CountNonNullMappings(b) >= 1).ToList();

        var exportList = bricks.Select(b => new ExportMappedBrick
        {
            LegoPartNum = b.LegoPartNum,
            LegoName = b.LegoName,
            Name = b.Name,
            BbPartNum = b.BbPartNum,
            BbName = b.BbName,
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
        await File.WriteAllTextAsync(_exportPath, json);
        return exportList.Count;
    }

    public async Task<int> ImportMappedBricksAsync()
    {
        if (!File.Exists(_exportPath)) return 0;
        var json = await File.ReadAllTextAsync(_exportPath);
        var imported = JsonSerializer.Deserialize<List<ExportMappedBrick>>(json);
        if (imported == null) return 0;

        int importedCount = 0;
        foreach (var b in imported)
        {
            var existing = await _db.MappedBricks.FirstOrDefaultAsync(x => x.LegoPartNum == b.LegoPartNum);
            if (existing != null)
            {
                // Update mappings
                existing.BbPartNum = b.BbPartNum;
                existing.BbName = b.BbName;
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
        await _db.SaveChangesAsync();
        return importedCount;
    }

    private int CountNonNullMappings(MappedBrick b)
    {
        int count = 0;
        if (!string.IsNullOrWhiteSpace(b.BbPartNum)) count++;
        if (!string.IsNullOrWhiteSpace(b.BbName)) count++;
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
        public string? BbPartNum { get; set; }
        public string? BbName { get; set; }
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
