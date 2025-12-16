using System.Globalization;
using CsvHelper;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Services.Storage;

namespace Data;

public static class RebrickableSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IDbContextFactory<AppDbContext> dbFactory,
        IExportStorage exportStorage,
        string contentRootPath)
    {
        // 1) Erst versuchen wir "Overrides" aus ExportStorage (lokal: mappedData/, Azure: Blob mappedData/)
        //    Das ist safe: wenn nix da ist, ImportSets/ImportMappedBricks geben 0 zurück.
        var setsExportService = new Data.Services.ItemSetExportService(dbFactory, exportStorage);
        await setsExportService.ImportSetsAsync();

        var mappedExportService = new Data.Services.MappedBrickExportService(dbFactory, exportStorage);
        await mappedExportService.ImportMappedBricksAsync();

        // 2) CSV Seeder nur wenn RebrickableData vorhanden ist (typisch lokal)
        var dataDir = Path.Combine(contentRootPath, "RebrickableData");
        if (!Directory.Exists(dataDir))
            return;

        // Nur importieren, wenn noch keine LEGO-Daten existieren
        if (!await db.MappedBricks.AnyAsync())
            await ImportPartsAsync(db, Path.Combine(dataDir, "parts.csv"));

        if (!await db.BrickColors.AnyAsync())
            await ImportColorsAsync(db, Path.Combine(dataDir, "colors.csv"));

        if (!await db.ItemSets.AnyAsync())
            await ImportSetsAsync(db, Path.Combine(dataDir, "sets.csv"));

        // ItemSetBricks: Verknüpfung zwischen Sets und MappedBricks
        if (!await db.ItemSetBricks.AnyAsync())
            await ImportInventoryPartsAsync(db,
                Path.Combine(dataDir, "inventories.csv"),
                Path.Combine(dataDir, "inventory_parts.csv"));
    }

    private static async Task ImportPartsAsync(AppDbContext db, string partsPath)
    {
        if (!File.Exists(partsPath)) return;

        using var reader = new StreamReader(partsPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<RebrickablePart>();

        foreach (var p in records)
        {
            // imagePath wird hier aktuell nicht persistiert – kann bleiben oder raus.
            // (Falls du später Bild-URLs speichern willst, dann hier setzen.)
            _ = p.part_num;

            var brick = new MappedBrick
            {
                LegoPartNum = p.part_num,
                LegoName = p.name,
                Name = p.name,
            };

            db.MappedBricks.Add(brick);
        }

        await db.SaveChangesAsync();
    }

    private static async Task ImportSetsAsync(AppDbContext db, string setsPath)
    {
        if (!File.Exists(setsPath)) return;

        using var reader = new StreamReader(setsPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<RebrickableSet>();

        foreach (var s in records)
        {
            var set = new ItemSet
            {
                SetNum = s.set_num,
                Name = s.name,
                Brand = "Lego",
                Year = s.year,
                ImageUrl = s.img_url
            };

            db.ItemSets.Add(set);
        }

        await db.SaveChangesAsync();
    }

    private static async Task ImportColorsAsync(AppDbContext db, string colorsPath)
    {
        if (!File.Exists(colorsPath)) return;

        using var reader = new StreamReader(colorsPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<RebrickableColor>();

        foreach (var c in records)
        {
            var color = new BrickColor
            {
                RebrickableColorId = c.id,
                Name = c.name,
                Rgb = c.rgb
            };

            db.BrickColors.Add(color);
        }

        await db.SaveChangesAsync();
    }

    private static async Task ImportInventoryPartsAsync(AppDbContext db, string inventoriesPath, string inventoryPartsPath)
    {
        if (!File.Exists(inventoriesPath) || !File.Exists(inventoryPartsPath)) return;

        // Mapping: set_num -> ItemSet Id
        var setMapping = await db.ItemSets
            .AsNoTracking()
            .Where(s => s.SetNum != null)
            .ToDictionaryAsync(s => s.SetNum!, s => s.Id);

        // Mapping: part_num -> MappedBrick Id
        var partMapping = await db.MappedBricks
            .AsNoTracking()
            .Where(b => b.LegoPartNum != null)
            .ToDictionaryAsync(b => b.LegoPartNum!, b => b.Id);

        // Mapping: color_id -> BrickColor Id
        var colorMapping = await db.BrickColors
            .AsNoTracking()
            .ToDictionaryAsync(c => c.RebrickableColorId, c => c.Id);

        // Inventories laden (inventory_id -> set_num)
        var inventories = new Dictionary<int, string>();
        using (var reader = new StreamReader(inventoriesPath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<RebrickableInventory>();
            foreach (var inv in records)
                inventories[inv.id] = inv.set_num;
        }

        // Inventory Parts laden und ItemSetBricks erstellen
        using (var reader = new StreamReader(inventoryPartsPath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<RebrickableInventoryPart>();
            foreach (var invPart in records)
            {
                if (!inventories.TryGetValue(invPart.inventory_id, out var setNum))
                    continue;

                if (!setMapping.TryGetValue(setNum, out var itemSetId))
                    continue;

                if (!partMapping.TryGetValue(invPart.part_num, out var mappedBrickId))
                    continue;

                int? colorId = null;
                if (invPart.color_id > 0 && colorMapping.TryGetValue(invPart.color_id, out var brickColorId))
                    colorId = brickColorId;
                else if (colorMapping.TryGetValue(0, out var defaultColorId))
                    colorId = defaultColorId;

                if (!colorId.HasValue)
                    continue;

                db.ItemSetBricks.Add(new ItemSetBrick
                {
                    ItemSetId = itemSetId,
                    MappedBrickId = mappedBrickId,
                    BrickColorId = colorId.Value,
                    Quantity = invPart.quantity
                });
            }
        }

        await db.SaveChangesAsync();
    }

    // Hilfs-Klassen für CSV-Mapping
    private class RebrickablePart
    {
        public string part_num { get; set; } = default!;
        public string name { get; set; } = default!;
        public int part_cat_id { get; set; }
        public string part_material { get; set; } = default!;
    }

    private class RebrickableSet
    {
        public string set_num { get; set; } = default!;
        public string name { get; set; } = default!;
        public int year { get; set; }
        public int theme_id { get; set; }
        public int num_parts { get; set; }
        public string img_url { get; set; } = default!;
    }

    private class RebrickableColor
    {
        public int id { get; set; }
        public string name { get; set; } = default!;
        public string rgb { get; set; } = default!;
        public bool is_trans { get; set; }
    }

    private class RebrickableInventory
    {
        public int id { get; set; }
        public int version { get; set; }
        public string set_num { get; set; } = default!;
    }

    private class RebrickableInventoryPart
    {
        public int inventory_id { get; set; }
        public string part_num { get; set; } = default!;
        public int color_id { get; set; }
        public int quantity { get; set; }
        public bool is_spare { get; set; }
        public string img_url { get; set; } = default!;
    }
}
