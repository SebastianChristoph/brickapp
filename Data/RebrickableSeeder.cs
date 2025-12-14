using System.Globalization;
using CsvHelper;
using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data;

public static class RebrickableSeeder
{
    public static async Task SeedAsync(AppDbContext db, string contentRootPath)
    {
        // Importiere Sets aus JSON-Exportdatei im mappedData-Ordner
        var setsExportPath = Path.Combine(contentRootPath, "mappedData", "exported_sets.json");
        var setsExportService = new Services.ItemSetExportService(db, setsExportPath);
        await setsExportService.ImportSetsAsync();
        {
            var dataDir = Path.Combine(contentRootPath, "RebrickableData");
            if (!Directory.Exists(dataDir))
            {
                // Falls Ordner nicht existiert -> nichts tun
                return;
            }

            // Nur importieren, wenn noch keine LEGO-Daten existieren
            if (!await db.MappedBricks.AnyAsync())
            {
                await ImportPartsAsync(db, Path.Combine(dataDir, "parts.csv"));
            }

            // Importiere gemappte Bricks aus JSON-Exportdatei im mappedData-Ordner
            var exportPath = Path.Combine(contentRootPath, "mappedData", "exported_mappedbricks.json");
            var exportService = new Services.MappedBrickExportService(db, exportPath);
            await exportService.ImportMappedBricksAsync();

            if (!await db.BrickColors.AnyAsync())
            {
                await ImportColorsAsync(db, Path.Combine(dataDir, "colors.csv"));
            }

            if (!await db.ItemSets.AnyAsync())
            {
                await ImportSetsAsync(db, Path.Combine(dataDir, "sets.csv"));
            }

            // ItemSetBricks: Verknüpfung zwischen Sets und MappedBricks
            if (!await db.ItemSetBricks.AnyAsync())
            {
                await ImportInventoryPartsAsync(db,
                    Path.Combine(dataDir, "inventories.csv"),
                    Path.Combine(dataDir, "inventory_parts.csv"));
            }
        }
    }
    private static async Task ImportPartsAsync(AppDbContext db, string partsPath)
    {
        if (!File.Exists(partsPath)) return;

        using var reader = new StreamReader(partsPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<RebrickablePart>();


        foreach (var p in records)
        {
            string? imagePath = null;
            var pngPath = Path.Combine("wwwroot", "part_images", p.part_num + ".png");
            var jpgPath = Path.Combine("wwwroot", "part_images", p.part_num + ".jpg");
            if (File.Exists(pngPath))
                imagePath = $"/part_images/{p.part_num}.png";
            else if (File.Exists(jpgPath))
                imagePath = $"/part_images/{p.part_num}.jpg";

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
                LegoSetNum = s.set_num,
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
            .Where(s => s.LegoSetNum != null)
            .ToDictionaryAsync(s => s.LegoSetNum!, s => s.Id);

        // Mapping: part_num -> MappedBrick Id
        var partMapping = await db.MappedBricks
            .AsNoTracking()
            .Where(b => b.LegoPartNum != null)
            .ToDictionaryAsync(b => b.LegoPartNum!, b => b.Id);

        // Mapping: color_id -> BrickColor Id
        var colorMapping = await db.BrickColors
            .AsNoTracking()
            .ToDictionaryAsync(c => c.RebrickableColorId, c => c.Id);

        // Inventories laden
        var inventories = new Dictionary<int, string>(); // inventory_id -> set_num
        using (var reader = new StreamReader(inventoriesPath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<RebrickableInventory>();
            foreach (var inv in records)
            {
                inventories[inv.id] = inv.set_num;
            }
        }

        // Inventory Parts laden und ItemSetBricks erstellen
        using (var reader = new StreamReader(inventoryPartsPath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<RebrickableInventoryPart>();
            foreach (var invPart in records)
            {
                // inventory_id -> set_num
                if (!inventories.TryGetValue(invPart.inventory_id, out var setNum))
                    continue;

                // set_num -> ItemSet Id
                if (!setMapping.TryGetValue(setNum, out var itemSetId))
                    continue;

                // part_num -> MappedBrick Id
                if (!partMapping.TryGetValue(invPart.part_num, out var mappedBrickId))
                    continue;

                // color_id -> BrickColor Id (0 = standardfarbe, aber default auf 1 wenn vorhanden)
                int? colorId = null;
                if (invPart.color_id > 0 && colorMapping.TryGetValue(invPart.color_id, out var brickColorId))
                {
                    colorId = brickColorId;
                }
                else if (colorMapping.TryGetValue(0, out var defaultColorId))
                {
                    // Fallback auf color_id 0 (default)
                    colorId = defaultColorId;
                }

                // Wenn keine Farbe gefunden -> skip
                if (!colorId.HasValue)
                    continue;

                var itemSetBrick = new ItemSetBrick
                {
                    ItemSetId = itemSetId,
                    MappedBrickId = mappedBrickId,
                    BrickColorId = colorId.Value,
                    Quantity = invPart.quantity
                };

                db.ItemSetBricks.Add(itemSetBrick);
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
