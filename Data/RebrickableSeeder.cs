using System.Globalization;
using CsvHelper;
using brickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using brickapp.Data.Services.Storage;

namespace brickapp.Data;

public static class RebrickableSeeder
{
    private const int BatchSize = 500;

    public static async Task SeedAsync(
        AppDbContext db,
        IDbContextFactory<AppDbContext> dbFactory,
        IExportStorage exportStorage,
        string contentRootPath)
    {
        // Timeout erhöhen für umfangreiche Importe
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        var dataDir = Path.Combine(contentRootPath, "RebrickableData");
        if (!Directory.Exists(dataDir)) return;

        // 1. Basis-Daten importieren (Teile und Farben)
        if (!await db.MappedBricks.AnyAsync())
            await ImportPartsAsync(db, Path.Combine(dataDir, "parts.csv"));

        if (!await db.BrickColors.AnyAsync())
            await ImportColorsAsync(db, Path.Combine(dataDir, "colors.csv"));

        // 2. BrickLink Color Mappings importieren
        // Bedingung: Wir haben Farben, aber noch kein einziges BrickLink-Mapping
        if (await db.BrickColors.AnyAsync() && !await db.BrickColors.AnyAsync(c => c.BricklinkColorId != null))
        {
            await ImportBricklinkColorMappingsAsync(db, Path.Combine(dataDir, "color_mappings.csv"));
        }

        // 3. Sets und Inventare importieren
        if (!await db.ItemSets.AnyAsync())
            await ImportSetsAsync(db, Path.Combine(dataDir, "sets.csv"));

        if (!await db.ItemSetBricks.AnyAsync())
            await ImportInventoryPartsAsync(db, 
                Path.Combine(dataDir, "inventories.csv"), 
                Path.Combine(dataDir, "inventory_parts.csv"));

        // 4. Externe Export Services (falls vorhanden)
        var setsExportService = new Services.ItemSetExportService(dbFactory, exportStorage);
        await setsExportService.ImportSetsAsync();

        var mappedExportService = new Services.MappedBrickExportService(dbFactory, exportStorage);
        await mappedExportService.ImportMappedBricksAsync();

        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    private static async Task ImportBricklinkColorMappingsAsync(AppDbContext db, string mappingsPath)
    {
        if (!File.Exists(mappingsPath)) return;
        Console.WriteLine("Importiere BrickLink Color Mappings...");

        // Alle Farben laden für schnellen Zugriff via Dictionary
        var colors = await db.BrickColors.ToListAsync();
        var colorDict = colors.ToDictionary(c => c.RebrickableColorId);

        using var reader = new StreamReader(mappingsPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var rows = csv.GetRecords<ColorMappingRow>();

        int count = 0;
        foreach (var row in rows)
        {
            if (colorDict.TryGetValue(row.RebrickableColorId, out var color))
            {
                if (row.BricklinkColorId.HasValue)
                {
                    color.BricklinkColorId = row.BricklinkColorId;
                    
                    // WICHTIG: Explizites Update, da AutoDetectChangesEnabled = false
                    db.BrickColors.Update(color); 
                    count++;

                    if (count % BatchSize == 0)
                    {
                        await db.SaveChangesAsync();
                    }
                }
            }
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Console.WriteLine($"{count} BrickLink Color Mappings erfolgreich gesetzt.");
    }

    private static async Task ImportColorsAsync(AppDbContext db, string colorsPath)
    {
        if (!File.Exists(colorsPath)) return;
        Console.WriteLine("Importiere Basis-Farben...");
        
        using var reader = new StreamReader(colorsPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        int count = 0;
        foreach (var c in csv.GetRecords<RebrickableColor>())
        {
            db.BrickColors.Add(new BrickColor { RebrickableColorId = c.Id, Name = c.Name, Rgb = c.Rgb });
            if (++count % BatchSize == 0) { await db.SaveChangesAsync(); db.ChangeTracker.Clear(); }
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static async Task ImportPartsAsync(AppDbContext db, string partsPath)
    {
        if (!File.Exists(partsPath)) return;
        using var reader = new StreamReader(partsPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        int count = 0;
        foreach (var p in csv.GetRecords<RebrickablePart>())
        {
            db.MappedBricks.Add(new MappedBrick { LegoPartNum = p.PartNum, LegoName = p.Name, Name = p.Name });
            if (++count % BatchSize == 0) { await db.SaveChangesAsync(); db.ChangeTracker.Clear(); }
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static async Task ImportSetsAsync(AppDbContext db, string setsPath)
    {
        if (!File.Exists(setsPath)) return;
        using var reader = new StreamReader(setsPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        int count = 0;
        foreach (var s in csv.GetRecords<RebrickableSet>())
        {
            db.ItemSets.Add(new ItemSet { SetNum = s.SetNum, Name = s.Name, Brand = "Lego", Year = s.Year, ImageUrl = s.ImgUrl });
            if (++count % BatchSize == 0) { await db.SaveChangesAsync(); db.ChangeTracker.Clear(); }
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static async Task ImportInventoryPartsAsync(AppDbContext db, string inventoriesPath, string inventoryPartsPath)
    {
        if (!File.Exists(inventoriesPath) || !File.Exists(inventoryPartsPath)) return;
        
        var setMapping = await db.ItemSets.AsNoTracking().Where(s => s.SetNum != null).ToDictionaryAsync(s => s.SetNum!, s => s.Id);
        var partMapping = await db.MappedBricks.AsNoTracking().Where(b => b.LegoPartNum != null).ToDictionaryAsync(b => b.LegoPartNum!, b => b.Id);
        var colorMapping = await db.BrickColors.AsNoTracking().ToDictionaryAsync(c => c.RebrickableColorId, c => c.Id);

        var inventories = new Dictionary<int, string>();
        using (var reader = new StreamReader(inventoriesPath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            foreach (var inv in csv.GetRecords<RebrickableInventory>()) inventories[inv.Id] = inv.SetNum;
        }

        using var invReader = new StreamReader(inventoryPartsPath);
        using var invCsv = new CsvReader(invReader, CultureInfo.InvariantCulture);
        int count = 0;
        foreach (var invPart in invCsv.GetRecords<RebrickableInventoryPart>())
        {
            if (inventories.TryGetValue(invPart.InventoryId, out var setNum) &&
                setMapping.TryGetValue(setNum, out var itemSetId) &&
                partMapping.TryGetValue(invPart.PartNum, out var mappedBrickId) &&
                colorMapping.TryGetValue(invPart.ColorId, out var brickColorId))
            {
                db.ItemSetBricks.Add(new ItemSetBrick { ItemSetId = itemSetId, MappedBrickId = mappedBrickId, BrickColorId = brickColorId, Quantity = invPart.Quantity });
                if (++count % BatchSize == 0) { await db.SaveChangesAsync(); db.ChangeTracker.Clear(); }
            }
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    // Hilfsklassen für CSV-Mapping
    private class ColorMappingRow 
    { 
        public int RebrickableColorId { get; set; } 
        public int? BricklinkColorId { get; set; } 
        public int? LegoColorId { get; set; } 
    }
    private class RebrickablePart { public string PartNum { get; set; } = ""; public string Name { get; set; } = ""; }
    private class RebrickableSet { public string SetNum { get; set; } = ""; public string Name { get; set; } = ""; public int Year { get; set; } public string ImgUrl { get; set; } = ""; }
    private class RebrickableColor { public int Id { get; set; } public string Name { get; set; } = ""; public string Rgb { get; set; } = ""; }
    private class RebrickableInventory { public int Id { get; set; } public string SetNum { get; set; } = ""; }
    private class RebrickableInventoryPart { public int InventoryId { get; set; } public string PartNum { get; set; } = ""; public int ColorId { get; set; } public int Quantity { get; set; } }
}