namespace brickapp.Components.Shared.PartsListUpload
{
    public enum PartsUploadFormat
    {
        RebrickableCsv,
        RebrickableXml,
        BricklinkXml
    }

    public record UnmappedRow(string? PartNum, int? ColorId, int Quantity);

    public record InvalidRow(string? PartNum, int? ColorId, int Quantity, string Error);

    public class ParseResult<TItem>
    {
        public List<TItem> MappedItems { get; set; } = new();
        public List<UnmappedRow> Unmapped { get; set; } = new();
        public List<InvalidRow> InvalidRows { get; set; } = new();
        public HashSet<int> InvalidColorIds { get; set; } = new();

        public string? FatalError { get; set; }
    }
}
