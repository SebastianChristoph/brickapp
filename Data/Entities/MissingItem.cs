namespace Data.Entities
{
    public class MissingItem
    {
        public int Id { get; set; }
        public string? ExternalPartNum { get; set; } // Die Nummer aus der Datei
        public int? ExternalColorId { get; set; }    // Die ID aus der Datei
        public int Quantity { get; set; }
        
        // Verknüpfung (optional, je nachdem ob ein Item zu beiden gehören kann oder getrennt)
        public int? MockId { get; set; }
        public int? WantedListId { get; set; }
    }
}