using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;

namespace Services;

public class RebrickablePartImageService
{
    private readonly HttpClient _httpClient;
    private const string ApiKey = "c1995e6803e4cddd4e4a0dbc0ec4fcb1";
    private const string BaseUrl = "https://rebrickable.com/api/v3/lego/parts/";

    public RebrickablePartImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


        public async Task<string?> GetPartImageUrlAsync(string partNum, string? brand = null, string? name = null)
        {
            // 1. Wenn partNum vorhanden, wie bisher
            if (!string.IsNullOrWhiteSpace(partNum))
            {
                if (!string.IsNullOrWhiteSpace(brand))
                {
                    var safeBrand = brand.ToLower().Replace(" ", "_");
                    var pngBrandPath = $"wwwroot/part_images_{safeBrand}/{partNum}.png";
                    var jpgBrandPath = $"wwwroot/part_images_{safeBrand}/{partNum}.jpg";
                    if (System.IO.File.Exists(pngBrandPath))
                        return $"/part_images_{safeBrand}/{partNum}.png";
                    if (System.IO.File.Exists(jpgBrandPath))
                        return $"/part_images_{safeBrand}/{partNum}.jpg";
                }
                var pngPath = $"wwwroot/part_images/{partNum}.png";
                var jpgPath = $"wwwroot/part_images/{partNum}.jpg";
                if (System.IO.File.Exists(pngPath))
                    return $"/part_images/{partNum}.png";
                if (System.IO.File.Exists(jpgPath))
                    return $"/part_images/{partNum}.jpg";
            }

            // 2. Wenn keine partNum, aber Name und Brand vorhanden, prüfe nach Bild
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(brand))
            {
                var safeBrand = brand.ToLower().Replace(" ", "_");
                var safeName = name.ToLower().Replace(" ", "_");
                var pngBrandPath = $"wwwroot/part_images_{safeBrand}/{safeName}.png";
                var jpgBrandPath = $"wwwroot/part_images_{safeBrand}/{safeName}.jpg";
                if (System.IO.File.Exists(pngBrandPath))
                    return $"/part_images_{safeBrand}/{safeName}.png";
                if (System.IO.File.Exists(jpgBrandPath))
                    return $"/part_images_{safeBrand}/{safeName}.jpg";
            }

            // Fallback: Placeholder
            return PlaceholderUrl;
        }

        public async Task<string?> GetSetImageUrlAsync(string setNo, string? brand = null)
        {
            if (string.IsNullOrWhiteSpace(setNo)) return PlaceholderUrl;
            if (!string.IsNullOrWhiteSpace(brand))
            {
                var safeBrand = brand.ToLower().Replace(" ", "_");
                var pngBrandPath = $"wwwroot/set_images/{safeBrand}/{setNo}.png";
                var jpgBrandPath = $"wwwroot/set_images/{safeBrand}/{setNo}.jpg";
                if (System.IO.File.Exists(pngBrandPath))
                    return $"/set_images/{safeBrand}/{setNo}.png";
                if (System.IO.File.Exists(jpgBrandPath))
                    return $"/set_images/{safeBrand}/{setNo}.jpg";
            }
            return PlaceholderUrl;
        }


    // Platzhalter-Bild (z.B. Fragezeichen-Icon von Rebrickable)
    public static string PlaceholderUrl => "/part_images/placeholder.png";

        // Holt die Bild-URLs für mehrere Teile effizient per Batch-API
    public async Task<Dictionary<string, string>> GetPartImageUrlsBatchAsync(IEnumerable<string> partNums, string? brand = null)
    {
        var partNumList = partNums?.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        if (partNumList == null || partNumList.Count == 0)
            return new Dictionary<string, string>();

        var dict = new Dictionary<string, string>();
        foreach (var partNum in partNumList)
        {
            // Priorität: Brand-Ordner PNG, dann JPG, dann Standard-Ordner PNG, dann JPG
            if (!string.IsNullOrWhiteSpace(brand))
            {
                var safeBrand = brand.ToLower().Replace(" ", "_");
                var pngBrandPath = $"wwwroot/part_images_{safeBrand}/{partNum}.png";
                var jpgBrandPath = $"wwwroot/part_images_{safeBrand}/{partNum}.jpg";
                if (System.IO.File.Exists(pngBrandPath))
                {
                    dict[partNum] = $"/part_images_{safeBrand}/{partNum}.png";
                    continue;
                }
                if (System.IO.File.Exists(jpgBrandPath))
                {
                    dict[partNum] = $"/part_images_{safeBrand}/{partNum}.jpg";
                    continue;
                }
            }
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
