using System.Text.Json.Serialization;

namespace brickapp.Data.Services;

public class RebrickableApiService(HttpClient httpClient, ILogger<RebrickableApiService> logger)
{
    private const string ApiKey = "c1995e6803e4cddd4e4a0dbc0ec4fcb1";

    public record RebrickablePartInfo(string Name, string? ImageUrl);

    public async Task<RebrickablePartInfo?> GetLegoItemNameByPartNumber(string partNum)
{
    if (string.IsNullOrWhiteSpace(partNum)) return null;

    // 1. Direkter Versuch
    var directUrl = $"https://rebrickable.com/api/v3/lego/parts/{partNum}/";
    var directResponse = await ExecuteRequest(directUrl);
    if (directResponse != null)
    {
        var data = await directResponse.Content.ReadFromJsonAsync<RebrickablePartResponse>();
        if (data?.Name != null) 
            return new RebrickablePartInfo(data.Name, data.PartImgUrl);
    }

    // 2. Suche
    var searchUrl = $"https://rebrickable.com/api/v3/lego/parts/?search={partNum}";
    var searchResponse = await ExecuteRequest(searchUrl);

    if (searchResponse != null)
    {
        var searchData = await searchResponse.Content.ReadFromJsonAsync<RebrickableSearchResponse>();
        if (searchData?.Results != null)
        {
            var bestMatch = searchData.Results.FirstOrDefault(r => 
                r.PartNum?.Equals(partNum, StringComparison.OrdinalIgnoreCase) == true ||
                (r.ExternalIds?.BrickLink != null && 
                 r.ExternalIds.BrickLink.Any(bl => bl.Equals(partNum, StringComparison.OrdinalIgnoreCase)))
            );

            if (bestMatch != null)
            {
                return new RebrickablePartInfo(bestMatch.Name!, bestMatch.PartImgUrl);
            }
        }
    }
    return null;
}

    private async Task<HttpResponseMessage?> ExecuteRequest(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"key {ApiKey}");
        try
        {
            var response = await httpClient.SendAsync(request);
            return response.IsSuccessStatusCode ? response : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🌐 API Fehler: {Url}", url);
            return null;
        }
    }

    // Die Klassenstruktur muss die external_ids abbilden:
  public class RebrickablePartResponse 
{ 
    [JsonPropertyName("name")] public string? Name { get; set; } 
    [JsonPropertyName("part_num")] public string? PartNum { get; set; } 
    [JsonPropertyName("part_img_url")] public string? PartImgUrl { get; set; } // Hinzugefügt
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