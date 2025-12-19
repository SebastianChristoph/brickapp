using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using brickapp.Components.Shared.PartsListUpload;
using brickapp.Data.Services.PartsListUpload;

namespace brickapp.Data.Services.PartsListUpload;

public sealed class BricklinkXmlParser : IPartsListFormatParser
{
    public PartsUploadFormat Format => PartsUploadFormat.BricklinkXml;

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

        XDocument doc;
        try
        {
            doc = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        }
        catch (Exception ex)
        {
            invalidRows.Add(new InvalidRow(null, null, 0, $"Invalid XML: {ex.Message}"));
            return (rows, invalidRows, invalidColorIds);
        }

        // BrickLink XML export is commonly:
        // <INVENTORY><ITEM>...</ITEM></INVENTORY>
        var itemElements = doc.Descendants()
            .Where(e => e.Name.LocalName.Equals("ITEM", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (itemElements.Count == 0)
        {
            invalidRows.Add(new InvalidRow(null, null, 0, "No <ITEM> elements found"));
            return (rows, invalidRows, invalidColorIds);
        }

        int itemNo = 0;
        foreach (var item in itemElements)
        {
            itemNo++;

            var itemType = GetValue(item, "ITEMTYPE");
            if (!string.IsNullOrWhiteSpace(itemType) && !itemType.Equals("P", StringComparison.OrdinalIgnoreCase))
            {
                // Only parts
                continue;
            }

            var partNum = GetValue(item, "ITEMID") ?? GetValue(item, "PARTNUM") ?? GetValue(item, "PART");
            var colorStr = GetValue(item, "COLOR");

            // BrickLink often uses MINQTY for wanted lists, QTY for other exports.
            var qtyStr = GetValue(item, "MINQTY") ?? GetValue(item, "QTY") ?? GetValue(item, "QUANTITY");

            if (string.IsNullOrWhiteSpace(partNum))
            {
                invalidRows.Add(new InvalidRow(null, null, 0, $"Item {itemNo}: Missing ITEMID/PartNum"));
                continue;
            }

            if (!TryParseInt(colorStr, out var bricklinkColorId))
            {
                invalidRows.Add(new InvalidRow(partNum, null, 0, $"Item {itemNo}: Invalid COLOR '{colorStr}'"));
                continue;
            }

            if (!TryParseInt(qtyStr, out var qty) || qty <= 0)
            {
                invalidRows.Add(new InvalidRow(partNum, bricklinkColorId, 0, $"Item {itemNo}: Invalid Qty '{qtyStr}'"));
                continue;
            }

            rows.Add(new RawRow(partNum.Trim(), bricklinkColorId, qty, ColorMode: "Bricklink"));
        }

        return (rows, invalidRows, invalidColorIds);
    }

    private static string? GetValue(XElement parent, string localName)
    {
        return parent.Elements()
            .FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    private static bool TryParseInt(string? s, out int value)
    {
        return int.TryParse((s ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
