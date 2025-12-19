using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using brickapp.Components.Shared.PartsListUpload;
using brickapp.Data;

namespace brickapp.Data.Services.PartsListUpload;

public sealed class PartsListUploadService : IPartsListUploadService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly Dictionary<PartsUploadFormat, IPartsListFormatParser> _parsers;

    public PartsListUploadService(
        IDbContextFactory<AppDbContext> factory,
        IEnumerable<IPartsListFormatParser> parsers)
    {
        _factory = factory;

        // genau EIN Parser pro Format erzwingen
        _parsers = parsers.ToDictionary(p => p.Format);
    }

    public async Task<ParseResult<ParsedPart>> ParseAsync(string content, PartsUploadFormat format)
    {
        var result = new ParseResult<ParsedPart>();

        if (!_parsers.TryGetValue(format, out var parser))
        {
            result.FatalError = $"No parser registered for format '{format}'.";
            return result;
        }

        // ============================
        // 1) Datei -> RawRows
        // ============================
        var (rawRows, invalidRows, invalidColorIds) = parser.Parse(content);

        result.InvalidRows.AddRange(invalidRows);
        result.InvalidColorIds.UnionWith(invalidColorIds);

        if (rawRows.Count == 0)
            return result;

        await using var db = await _factory.CreateDbContextAsync();

        // ============================
        // 2) DB-Daten laden
        // ============================

        // Bricks: Upload-PartNum -> MappedBrick über LegoPartNum
        var bricks = await db.MappedBricks
            .AsNoTracking()
            .Where(b => !string.IsNullOrWhiteSpace(b.LegoPartNum))
            .ToListAsync();

        var brickByPartNum = bricks
            .GroupBy(b => b.LegoPartNum!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Colors laden
        var colors = await db.BrickColors
            .AsNoTracking()
            .ToListAsync();

        // RebrickableColorId -> BrickColor.Id
        var rebrickableColorIdToDbId = colors
            .GroupBy(c => c.RebrickableColorId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        // BricklinkColorId -> BrickColor.Id (nur wo gesetzt)
            var bricklinkColorIdToDbId = colors
                .Where(c => c.BricklinkColorId.HasValue)
                .GroupBy(c => c.BricklinkColorId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Id);

        // ============================
        // 3) RawRows -> ParsedPart
        // ============================

        foreach (var row in rawRows)
        {
            // --- Part Mapping ---
            if (!brickByPartNum.TryGetValue(row.PartNum, out var brick))
            {
                result.Unmapped.Add(new UnmappedRow(
                    row.PartNum,
                    row.ColorIdFromFile,
                    row.Quantity));
                continue;
            }

            int dbColorId;

            // --- Color Mapping ---
            if (string.Equals(row.ColorMode, "Rebrickable", StringComparison.OrdinalIgnoreCase))
            {
                if (!rebrickableColorIdToDbId.TryGetValue(row.ColorIdFromFile, out dbColorId))
                {
                    result.InvalidColorIds.Add(row.ColorIdFromFile);
                    continue;
                }
            }
            else // Bricklink
            {
                // BrickLink: direkt über BricklinkColorId in DB
                if (!bricklinkColorIdToDbId.TryGetValue(row.ColorIdFromFile, out dbColorId))
                {
                    result.InvalidColorIds.Add(row.ColorIdFromFile);
                    continue;
                }
            }

            result.MappedItems.Add(new ParsedPart(
                MappedBrickId: brick.Id,
                BrickColorId: dbColorId,
                Quantity: row.Quantity,
                ExternalPartNum: row.PartNum
            ));
        }

        // ============================
        // 4) Deduplizieren & Summieren
        // ============================
        result.MappedItems = result.MappedItems
            .GroupBy(p => new { p.MappedBrickId, p.BrickColorId })
            .Select(g => new ParsedPart(
                g.Key.MappedBrickId,
                g.Key.BrickColorId,
                g.Sum(x => x.Quantity),
                g.First().ExternalPartNum
            ))
            .ToList();

        return result;
    }
}
