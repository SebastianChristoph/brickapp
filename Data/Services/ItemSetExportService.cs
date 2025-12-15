using System.Text.Json;
using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Services;

public class ItemSetExportService
{
    private readonly AppDbContext _db;
    private readonly string _exportPath;

    public ItemSetExportService(AppDbContext db, string exportPath)
    {
        _db = db;
        _exportPath = exportPath;
    }

    public async Task<int> ExportSetsAsync()
    {
        var sets = await _db.ItemSets.ToListAsync();
        var exportList = sets.Select(s => new ExportSet
        {
            SetNum = s.SetNum,
            Name = s.Name,
            Brand = s.Brand,
            Year = (int)s.Year,
            ImageUrl = s.ImageUrl
        }).ToList();
        var json = JsonSerializer.Serialize(exportList, new JsonSerializerOptions { WriteIndented = true });
        var dir = Path.GetDirectoryName(_exportPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(_exportPath, json);
        return exportList.Count;
    }

    public async Task<int> ImportSetsAsync()
    {
        if (!File.Exists(_exportPath)) return 0;
        var json = await File.ReadAllTextAsync(_exportPath);
        var imported = JsonSerializer.Deserialize<List<ExportSet>>(json);
        if (imported == null) return 0;
        int importedCount = 0;
        foreach (var s in imported)
        {
            if (!await _db.ItemSets.AnyAsync(x => x.SetNum == s.SetNum))
            {
                var set = new ItemSet
                {
                    SetNum = s.SetNum,
                    Name = s.Name,
                    Brand = s.Brand,
                    Year = s.Year,
                    ImageUrl = s.ImageUrl
                };
                _db.ItemSets.Add(set);
                importedCount++;
            }
        }
        await _db.SaveChangesAsync();
        return importedCount;
    }

    private class ExportSet
    {
        public string SetNum { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int Year { get; set; }
        public string? ImageUrl { get; set; }
    }
}
