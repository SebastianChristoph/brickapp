using System.Text.Json;
using Data;                  // <- WICHTIG für AppDbContext
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Services.Storage;

namespace Data.Services;

public class ItemSetExportService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IExportStorage _storage;

    // liegt unter mappedData/
    private const string ExportRelPath = "exported_sets.json";

    public ItemSetExportService(IDbContextFactory<AppDbContext> factory, IExportStorage storage)
    {
        _factory = factory;
        _storage = storage;
    }

    public string GetExportPath() => _storage.DescribeTarget(ExportRelPath);

    public async Task<int> ExportSetsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        var sets = await db.ItemSets.AsNoTracking().ToListAsync();

        var exportList = sets.Select(s => new ExportSet
        {
            SetNum = s.SetNum,
            Name = s.Name,
            Brand = s.Brand,
            Year = (int)(s.Year ?? 0), // falls Year nullable ist
            ImageUrl = s.ImageUrl
        }).ToList();

        var json = JsonSerializer.Serialize(exportList, new JsonSerializerOptions { WriteIndented = true });

        await _storage.WriteTextAsync(ExportRelPath, "application/json", json);
        return exportList.Count;
    }

    public async Task<int> ImportSetsAsync()
    {
        // Wichtig: nicht crashen wenn Datei nicht existiert
        if (!await _storage.ExistsAsync($"mappedData/{ExportRelPath}"))
            return 0;

        var json = await _storage.ReadTextAsync($"mappedData/{ExportRelPath}");
        if (string.IsNullOrWhiteSpace(json))
            return 0;

        var imported = JsonSerializer.Deserialize<List<ExportSet>>(json);
        if (imported == null || imported.Count == 0)
            return 0;

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
