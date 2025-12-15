using System.Text.Json;
using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Services;

public class ItemSetExportService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly string _exportPath;

    public ItemSetExportService(
        IDbContextFactory<AppDbContext> factory,
        string exportPath)
    {
        _factory = factory;
        _exportPath = exportPath;
    }

    public async Task<int> ExportSetsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        var sets = await db.ItemSets.AsNoTracking().ToListAsync();

        var exportList = sets.Select(s => new ExportSet
        {
            SetNum = s.SetNum,
            Name = s.Name,
            Brand = s.Brand,
            Year = (int)s.Year,
            ImageUrl = s.ImageUrl
        }).ToList();

        var json = JsonSerializer.Serialize(exportList, new JsonSerializerOptions
        {
            WriteIndented = true
        });

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

        await using var db = await _factory.CreateDbContextAsync();

        int importedCount = 0;
        foreach (var s in imported)
        {
            if (!await db.ItemSets.AnyAsync(x => x.SetNum == s.SetNum))
            {
                db.ItemSets.Add(new ItemSet
                {
                    SetNum = s.SetNum,
                    Name = s.Name,
                    Brand = s.Brand,
                    Year = s.Year,
                    ImageUrl = s.ImageUrl
                });
                importedCount++;
            }
        }

        await db.SaveChangesAsync();
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
