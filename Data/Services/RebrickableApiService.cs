using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace brickapp.Data.Services;

public class RebrickableApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RebrickableApiService> _logger;
    private const string ApiKey = "c1995e6803e4cddd4e4a0dbc0ec4fcb1";

    public RebrickableApiService(HttpClient httpClient, ILogger<RebrickableApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> GetLegoItemNameByPartNumber(string partNum)
    {
        if (string.IsNullOrWhiteSpace(partNum)) return null;

        // 1. Direkter Versuch (manchmal ist BL ID = Rebrickable ID)
        var directUrl = $"https://rebrickable.com/api/v3/lego/parts/{partNum}/";
        var directResponse = await ExecuteRequest(directUrl);
        if (directResponse != null)
        {
            var data = await directResponse.Content.ReadFromJsonAsync<RebrickablePartResponse>();
            if (data?.Name != null) return data.Name;
        }

        // 2. Suche und Deep-Check in den External IDs
        _logger.LogInformation("🔍 Suche nach BrickLink Mapping für {PartNum}...", partNum);
        var searchUrl = $"https://rebrickable.com/api/v3/lego/parts/?search={partNum}";
        var searchResponse = await ExecuteRequest(searchUrl);

        if (searchResponse != null)
        {
            var searchData = await searchResponse.Content.ReadFromJsonAsync<RebrickableSearchResponse>();
            if (searchData?.Results != null)
            {
                // Wir suchen in allen Ergebnissen das Teil, das unter external_ids > BrickLink unsere partNum hat
                var bestMatch = searchData.Results.FirstOrDefault(r => 
                    // Check 1: Haupt-ID matcht
                    r.PartNum?.Equals(partNum, StringComparison.OrdinalIgnoreCase) == true ||
                    // Check 2: BrickLink Mapping matcht exakt (Dein JSON Beispiel)
                    (r.ExternalIds != null && 
                     r.ExternalIds.BrickLink != null && 
                     r.ExternalIds.BrickLink.Any(bl => bl.Equals(partNum, StringComparison.OrdinalIgnoreCase)))
                );

                if (bestMatch != null)
                {
                    _logger.LogInformation("🎯 Mapping gefunden: {Input} ist bei Rebrickable {RbId} ({Name})", 
                        partNum, bestMatch.PartNum, bestMatch.Name);
                    return bestMatch.Name;
                }
            }
        }

        _logger.LogWarning("❌ Kein Mapping für {PartNum} gefunden.", partNum);
        return null;
    }

    private async Task<HttpResponseMessage?> ExecuteRequest(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"key {ApiKey}");
        try
        {
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode ? response : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🌐 API Fehler: {Url}", url);
            return null;
        }
    }

    // Die Klassenstruktur muss die external_ids abbilden:
    public class RebrickablePartResponse 
    { 
        [JsonPropertyName("name")] public string? Name { get; set; } 
        [JsonPropertyName("part_num")] public string? PartNum { get; set; } 
        [JsonPropertyName("external_ids")] public ExternalIdsContainer? ExternalIds { get; set; }
    }

    public class ExternalIdsContainer
    {
        [JsonPropertyName("BrickLink")] public List<string>? BrickLink { get; set; }
    }

    public class RebrickableSearchResponse 
    { 
        [JsonPropertyName("results")] public List<RebrickablePartResponse>? Results { get; set; } 
    }
}