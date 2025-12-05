using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;

namespace brickisbrickapp.Services;

public class RebrickablePartImageService
{
    private readonly HttpClient _httpClient;
    private const string ApiKey = "c1995e6803e4cddd4e4a0dbc0ec4fcb1";
    private const string BaseUrl = "https://rebrickable.com/api/v3/lego/parts/";

    public RebrickablePartImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> GetPartImageUrlAsync(string partNum)
    {
        if (string.IsNullOrWhiteSpace(partNum)) return PlaceholderUrl;

        // Prüfe, ob ein lokales Bild existiert (png, dann jpg)
        var pngPath = $"wwwroot/part_images/{partNum}.png";
        var jpgPath = $"wwwroot/part_images/{partNum}.jpg";
        if (System.IO.File.Exists(pngPath))
            return $"/part_images/{partNum}.png";
        if (System.IO.File.Exists(jpgPath))
            return $"/part_images/{partNum}.jpg";

        // Optional: Online-Check (auskommentiert, falls API-Limit)
        // var url = $"{BaseUrl}{partNum}/?key={ApiKey}";
        // try
        // {
        //     var response = await _httpClient.GetAsync(url);
        //     if (!response.IsSuccessStatusCode) return PlaceholderUrl;
        //     var json = await response.Content.ReadAsStringAsync();
        //     using var doc = JsonDocument.Parse(json);
        //     if (doc.RootElement.TryGetProperty("part_img_url", out var imgProp))
        //         return imgProp.GetString() ?? PlaceholderUrl;
        //     return PlaceholderUrl;
        // }
        // catch { return PlaceholderUrl; }

        // Fallback: Placeholder
        return PlaceholderUrl;
    }


    // Platzhalter-Bild (z.B. Fragezeichen-Icon von Rebrickable)
    public static string PlaceholderUrl => "/part_images/placeholder.png";

        // Holt die Bild-URLs für mehrere Teile effizient per Batch-API
        public async Task<Dictionary<string, string>> GetPartImageUrlsBatchAsync(IEnumerable<string> partNums)
        {
            var partNumList = partNums?.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
            if (partNumList == null || partNumList.Count == 0)
                return new Dictionary<string, string>();

            var dict = new Dictionary<string, string>();
            foreach (var partNum in partNumList)
            {
                // Priorität: PNG, dann JPG
                var pngPath = $"wwwroot/part_images/{partNum}.png";
                var jpgPath = $"wwwroot/part_images/{partNum}.jpg";
                if (System.IO.File.Exists(pngPath))
                {
                    dict[partNum] = $"/part_images/{partNum}.png";
                }
                else if (System.IO.File.Exists(jpgPath))
                {
                    dict[partNum] = $"/part_images/{partNum}.jpg";
                }
                else
                {
                    dict[partNum] = PlaceholderUrl;
                }
            }
            return dict;
        }
}
