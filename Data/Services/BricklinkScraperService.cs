using System.Text.RegularExpressions;

namespace brickapp.Data.Services;

public class BricklinkScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BricklinkScraperService> _logger;

    public BricklinkScraperService(HttpClient httpClient, ILogger<BricklinkScraperService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> GetBricklinkItemNameAsync(string partNum)
    {
        try
        {
            var url = $"https://www.bricklink.com/v2/catalog/catalogitem.page?P={partNum}";
            _logger.LogInformation("Fetching Bricklink item name from: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Bricklink page. Status: {Status}", response.StatusCode);
                return null;
            }

            var html = await response.Content.ReadAsStringAsync();

            // Extract item name from the page title or h1 tag
            // Pattern: <h1 id="item-name-title">Brick 2 x 4</h1>
            var h1Match = Regex.Match(html, @"<h1[^>]*id=[""']item-name-title[""'][^>]*>([^<]+)</h1>", RegexOptions.IgnoreCase);
            if (h1Match.Success)
            {
                var itemName = h1Match.Groups[1].Value.Trim();
                _logger.LogInformation("Successfully extracted item name: {ItemName}", itemName);
                return itemName;
            }

            // Fallback: Try to get from page title "Brick: 3001 Brick 2 x 4"
            var titleMatch = Regex.Match(html, @"<title[^>]*>(?:Catalog:\s*)?(?:Parts:\s*)?(?:Brick:\s*)?(?:\d+\s+)?([^<\-]+?)(?:\s*-\s*BrickLink)?</title>", RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                var itemName = titleMatch.Groups[1].Value.Trim();
                _logger.LogInformation("Successfully extracted item name from title: {ItemName}", itemName);
                return itemName;
            }

            _logger.LogWarning("Could not extract item name from Bricklink page");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bricklink item name for part {PartNum}", partNum);
            return null;
        }
    }
}
