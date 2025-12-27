using System.Globalization;
using System.Text;
using brickapp.Components.Shared.PartsListUpload;

namespace brickapp.Data.Services.PartsListUpload;

public sealed class RebrickableCsvParser : IPartsListFormatParser
{
    public PartsUploadFormat Format => PartsUploadFormat.RebrickableCsv;

    public (List<RawRow> Rows, List<InvalidRow> InvalidRows, HashSet<int> InvalidColorIds) Parse(string content)
    {
        var rows = new List<RawRow>();
        var invalidRows = new List<InvalidRow>();
        var invalidColorIds = new HashSet<int>();

        if (string.IsNullOrWhiteSpace(content))
        {
            invalidRows.Add(new InvalidRow(null, null, 0, "Empty file content"));
            return (rows, invalidRows, invalidColorIds);
        }

        // Split lines robustly
        var lines = SplitLines(content).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (lines.Count == 0)
        {
            invalidRows.Add(new InvalidRow(null, null, 0, "No data rows found"));
            return (rows, invalidRows, invalidColorIds);
        }

        // Header
        var header = ParseCsvLine(lines[0]);
        var headerMap = BuildHeaderMap(header);

        // Required columns (flexible names)
        var partIdx = FindColumn(headerMap, new[]
        {
            "part", "part num", "partnum", "part id", "partid", "itemid", "item id"
        });

        // Rebrickable exports commonly have "Color ID"
        var colorIdIdx = FindColumn(headerMap, new[]
        {
            "color id", "colorid", "rebrickable color id", "rb color id", "color"
        });

        var qtyIdx = FindColumn(headerMap, new[]
        {
            "quantity", "qty", "count", "num"
        });

        // Optional: "Is Spare" / "Spare" (we ignore it for now)
        // var spareIdx = FindColumn(headerMap, new[] { "is spare", "spare" });

        if (partIdx < 0 || colorIdIdx < 0 || qtyIdx < 0)
        {
            invalidRows.Add(new InvalidRow(null, null, 0,
                $"Missing required columns. Found header: [{string.Join(", ", header)}]. " +
                "Need Part + Color ID + Quantity (names may vary)."));
            return (rows, invalidRows, invalidColorIds);
        }

        for (int i = 1; i < lines.Count; i++)
        {
            var fields = ParseCsvLine(lines[i]);

            // Safely get values even if line is shorter
            string? part = GetField(fields, partIdx)?.Trim();
            string? colorStr = GetField(fields, colorIdIdx)?.Trim();
            string? qtyStr = GetField(fields, qtyIdx)?.Trim();

            if (string.IsNullOrWhiteSpace(part))
            {
                invalidRows.Add(new InvalidRow(null, null, 0, $"Line {i + 1}: Missing PartNum"));
                continue;
            }

            if (!TryParseInt(colorStr, out var colorId))
            {
                invalidRows.Add(new InvalidRow(part, null, 0, $"Line {i + 1}: Invalid Color ID '{colorStr}'"));
                continue;
            }

            if (!TryParseInt(qtyStr, out var qty) || qty <= 0)
            {
                invalidRows.Add(new InvalidRow(part, colorId, 0, $"Line {i + 1}: Invalid Qty '{qtyStr}'"));
                continue;
            }

            rows.Add(new RawRow(part, colorId, qty, ColorMode: "Rebrickable"));
        }

        return (rows, invalidRows, invalidColorIds);
    }

    private static IEnumerable<string> SplitLines(string s)
    {
        using var reader = new StringReader(s);
        while (reader.ReadLine() is { } line)
            yield return line;
    }

    private static Dictionary<string, int> BuildHeaderMap(List<string> headerFields)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headerFields.Count; i++)
        {
            var key = (headerFields[i]).Trim();
            if (!string.IsNullOrWhiteSpace(key))
                map.TryAdd(key, i);
        }
        return map;
    }

    private static int FindColumn(Dictionary<string, int> headerMap, IEnumerable<string> candidates)
    {
        foreach (var c in candidates)
        {
            // exact match first
            if (headerMap.TryGetValue(c, out var idx))
                return idx;

            // also try normalized variants
            var normalized = NormalizeHeader(c);
            foreach (var kv in headerMap)
            {
                if (NormalizeHeader(kv.Key) == normalized)
                    return kv.Value;
            }
        }
        return -1;
    }

    private static string NormalizeHeader(string s)
        => new string((s).Trim().ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch) || ch == ' ').ToArray())
            .Replace("  ", " ");

    private static string? GetField(List<string> fields, int idx)
        => idx >= 0 && idx < fields.Count ? fields[idx] : null;

    private static bool TryParseInt(string? s, out int value)
    {
        return int.TryParse((s ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Minimal CSV parser with quote support: handles commas inside quotes and escaped quotes ("").
    /// </summary>
    private static List<string> ParseCsvLine(string? line)
    {
        var result = new List<string>();
        if (line == null)
            return result;

        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    // escaped quote
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            else
            {
                if (ch == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else if (ch == '"')
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(ch);
                }
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}
