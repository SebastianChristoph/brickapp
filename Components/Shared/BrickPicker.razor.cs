using brickapp.Data.DTOs;
using brickapp.Data.Entities;
using brickapp.Helpers;
using Microsoft.AspNetCore.Components;

namespace brickapp.Components.Shared;

public partial class BrickPicker
{
    [Parameter] public string Label { get; set; } = "Search for an item";
    [Parameter] public string Placeholder { get; set; } = "Enter part number or name...";
    [Parameter] public string AddButtonText { get; set; } = "Add to List";
    [Parameter] public bool ShowQuickColorPicker { get; set; } = true;
    [Parameter] public bool ShowBrandSelector { get; set; }
    [Parameter] public MappedBrick? PreselectedBrick { get; set; }
    [Parameter] public EventCallback<BrickSelectionDto> OnBrickAdded { get; set; }
    private List<MappedBrick> _allBricks = new();
    private bool _searchingBricks;
    private bool _allBricksLoaded;
    private List<BrickColor> _allColors = new();
    private List<BrickColor> _availableColors = new();
    private List<BrickColor> _quickColors = new();
    private Dictionary<string, string> _imageCache = new();
    private MappedBrick? SelectedBrick { get; set; }
    private MappedBrick? _previousSelectedBrick;
    private int? SelectedColorId { get; set; }
    private int Quantity { get; set; } = 1;
    private string SelectedBrand { get; set; } = "Lego";
    private List<MappedBrick> _searchResults = new();
    private List<string> _availableBrands = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _allColors = await MappedBrickService.GetAllColorsAsync();
            Logger.LogInformation("🟢 [BrickPicker] Loaded {Count} colors", _allColors.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "🔴 [BrickPicker] Error loading colors");
        }
    }

    protected override void OnParametersSet()
    {
        // Wenn ein PreselectedBrick übergeben wurde, setze ihn
        if (PreselectedBrick != null && SelectedBrick == null)
        {
            SelectedBrick = PreselectedBrick;
            OnBrickSelected(PreselectedBrick);
        }
        else if (SelectedBrick != _previousSelectedBrick)
        {
            _previousSelectedBrick = SelectedBrick;
            OnBrickSelected(SelectedBrick);
        }
    }

    private void OnOpenChanged(bool isOpen)
    {
        // Verhindere das Schließen der Liste, wenn noch kein Brick ausgewählt wurde
        if (!isOpen && SelectedBrick == null && _searchResults.Any())
        {
            StateHasChanged();
        }
    }

    private async Task EnsureAllBricksLoadedAsync()
    {
        if (_allBricksLoaded) return;
        _allBricksLoaded = true;

        try
        {
            Logger.LogInformation("🟡 [BrickPicker] Loading all bricks...");
            _allBricks = await MappedBrickService.GetAllMappedBricksAsync();
            Logger.LogInformation("🟢 [BrickPicker] Loaded {Count} bricks", _allBricks.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "🔴 [BrickPicker] Error loading bricks");
            _allBricks = new();
        }
    }

    private async Task<IEnumerable<MappedBrick>> SearchBricksWrapper(string searchText,
        CancellationToken cancellationToken)
    {
        _searchingBricks = true;
        StateHasChanged();
        var results = await SearchBricks(searchText);
        _searchingBricks = false;
        StateHasChanged();
        return results;
    }

    private async Task<IEnumerable<MappedBrick>> SearchBricks(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
            return Array.Empty<MappedBrick>();

        await EnsureAllBricksLoadedAsync();

        var normalized = searchText.Replace(" ", "").ToLower();

        var results = _allBricks
            .Where(b =>
            {
                var matchNum = (b.LegoPartNum?.Replace(" ", "").ToLower().Contains(normalized) ?? false) ||
                               (b.BluebrixxPartNum?.Replace(" ", "").ToLower().Contains(normalized) ?? false) ||
                               (b.CadaPartNum?.Replace(" ", "").ToLower().Contains(normalized) ?? false) ||
                               (b.PantasyPartNum?.Replace(" ", "").ToLower().Contains(normalized) ?? false) ||
                               (b.MouldKingPartNum?.Replace(" ", "").ToLower().Contains(normalized) ?? false) ||
                               (b.UnknownPartNum?.Replace(" ", "").ToLower().Contains(normalized) ?? false);

                var matchName = b.Name.Replace(" ", "").ToLower().Contains(normalized) ||
                                (b.LegoName?.Replace(" ", "").ToLower().Contains(normalized) ?? false) ||
                                (b.BluebrixxName?.Replace(" ", "").ToLower().Contains(normalized) ?? false) ||
                                (b.CadaName?.Replace(" ", "").ToLower().Contains(normalized) ?? false) ||
                                (b.PantasyName?.Replace(" ", "").ToLower().Contains(normalized) ?? false) ||
                                (b.MouldKingName?.Replace(" ", "").ToLower().Contains(normalized) ?? false) ||
                                (b.UnknownName?.Replace(" ", "").ToLower().Contains(normalized) ?? false);

                return matchNum || matchName;
            })
            .OrderByDescending(b =>
            {
                // Priorität 1: Exakte Übereinstimmung mit Namen (wichtigste!)
                var exactNameMatch = b.Name.Replace(" ", "").Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                                     (b.LegoName?.Replace(" ", "")
                                         .Equals(normalized, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                     (b.BluebrixxName?.Replace(" ", "")
                                         .Equals(normalized, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                     (b.CadaName?.Replace(" ", "")
                                         .Equals(normalized, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                     (b.PantasyName?.Replace(" ", "")
                                         .Equals(normalized, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                     (b.MouldKingName?.Replace(" ", "")
                                         .Equals(normalized, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                     (b.UnknownName?.Replace(" ", "")
                                         .Equals(normalized, StringComparison.OrdinalIgnoreCase) ?? false);

                return exactNameMatch ? 2 : 0;
            })
            .ThenByDescending(b =>
            {
                // Priorität 2: Exakte Übereinstimmung mit Part Number
                var exactPartMatch =
                    (b.LegoPartNum?.Replace(" ", "").Equals(normalized, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (b.BluebrixxPartNum?.Replace(" ", "").Equals(normalized, StringComparison.OrdinalIgnoreCase) ??
                     false) ||
                    (b.CadaPartNum?.Replace(" ", "").Equals(normalized, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (b.PantasyPartNum?.Replace(" ", "").Equals(normalized, StringComparison.OrdinalIgnoreCase) ??
                     false) ||
                    (b.MouldKingPartNum?.Replace(" ", "").Equals(normalized, StringComparison.OrdinalIgnoreCase) ??
                     false) ||
                    (b.UnknownPartNum?.Replace(" ", "").Equals(normalized, StringComparison.OrdinalIgnoreCase) ??
                     false);

                return exactPartMatch ? 1 : 0;
            })
            .ThenBy(b => b.LegoPartNum?.Length ?? 999)
            .Take(10)
            .ToList();

        // Speichere die Ergebnisse für die Open-Logik
        _searchResults = results;

        // Preload images for results
        foreach (var brick in results)
        {
            PreloadBrickImage(brick);
        }

        return results;
    }

    private void OnBrickSelected(MappedBrick? brick)
    {
        SelectedBrick = brick;
        SelectedColorId = null;
        Quantity = 1;

        if (brick != null)
        {
            UpdateAvailableColors();
            UpdateAvailableBrands();
        }

        StateHasChanged();
    }

    private void UpdateAvailableColors()
    {
        // Alle Farben verfügbar machen
        _availableColors = _allColors.OrderBy(c => c.Name).ToList();

        // Quick Colors: Die 10 meistgenutzten Farben laut Statistik
        var topColorNames = new[]
        {
            "Black",
            "White",
            "Light Bluish Gray",
            "Dark Bluish Gray",
            "Red",
            "Yellow",
            "Reddish Brown",
            "Blue",
            "Tan",
            "Light Gray"
        };
        _quickColors = _allColors
            .Where(c => topColorNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(c => Array.IndexOf(topColorNames, c.Name))
            .ToList();
    }

    private void SelectQuickColor(int colorId)
    {
        SelectedColorId = colorId;
        StateHasChanged();
    }

    private async Task HandleAdd()
    {
        if (SelectedBrick == null || SelectedColorId == null || Quantity < 1)
            return;

        var selection = new BrickSelectionDto
        {
            Brick = SelectedBrick,
            BrickColorId = SelectedColorId.Value,
            Quantity = Quantity,
            Brand = SelectedBrand
        };

        await OnBrickAdded.InvokeAsync(selection);

        // Reset
        SelectedBrick = null;
        SelectedColorId = null;
        Quantity = 1;
        StateHasChanged();
    }

    private string GetBrickDisplayName(MappedBrick? brick)
    {
        if (brick == null) return "";
        var partNum = GetBrickPartNumber(brick);
        var name = GetBrickName(brick);
        return $"{partNum} - {name}";
    }

    private string GetBrickPartNumber(MappedBrick brick)
    {
        return brick.LegoPartNum ?? brick.BluebrixxPartNum ?? brick.CadaPartNum ??
            brick.PantasyPartNum ?? brick.MouldKingPartNum ?? brick.UnknownPartNum ?? "No ID";
    }

    private string GetBrickName(MappedBrick brick)
    {
        return brick.LegoName ?? brick.BluebrixxName ?? brick.CadaName ??
            brick.PantasyName ?? brick.MouldKingName ?? brick.UnknownName ?? "Unknown Name";
    }

    private string GetBrickImageUrl(MappedBrick brick)
    {
        var partNum = GetBrickPartNumber(brick);
        if (_imageCache.TryGetValue(partNum, out var url))
            return url;

        PreloadBrickImage(brick);
        return _imageCache.GetValueOrDefault(partNum, "/api/image/placeholder");
    }

    private void PreloadBrickImage(MappedBrick brick)
    {
        var partNum = GetBrickPartNumber(brick);
        if (_imageCache.ContainsKey(partNum))
            return;

        var imagePath = ImageService.GetMappedBrickImagePath(brick);
        _imageCache[partNum] = imagePath;
    }

    private string GetColorHex(BrickColor color)
    {
        // Verwende BrickColorHelper.GetHexForColor mit dem Farbnamen
        var hex = BrickColorHelper.GetHexForColor(color.Name);
        if (string.IsNullOrWhiteSpace(hex))
            return "CCCCCC"; // Fallback grau

        // Entferne # falls vorhanden, da wir es im HTML selbst hinzufügen
        return hex.TrimStart('#');
    }

    private string GetSelectedColorHex()
    {
        if (SelectedColorId == null)
            return "transparent";

        var color = _allColors.FirstOrDefault(c => c.Id == SelectedColorId.Value);
        if (color == null)
            return "transparent";

        return GetColorHex(color);
    }

    private void UpdateAvailableBrands()
    {
        if (SelectedBrick == null)
        {
            _availableBrands = new();
            return;
        }

        _availableBrands = new();

        if (HasBrandMapping(SelectedBrick, "Lego"))
            _availableBrands.Add("Lego");
        if (HasBrandMapping(SelectedBrick, "BlueBrixx"))
            _availableBrands.Add("BlueBrixx");
        if (HasBrandMapping(SelectedBrick, "Cada"))
            _availableBrands.Add("Cada");
        if (HasBrandMapping(SelectedBrick, "Pantasy"))
            _availableBrands.Add("Pantasy");
        if (HasBrandMapping(SelectedBrick, "Mould King"))
            _availableBrands.Add("Mould King");
        if (HasBrandMapping(SelectedBrick, "Unknown"))
            _availableBrands.Add("Unknown");

        // Setze default Brand auf die erste verfügbare
        if (_availableBrands.Any() && !_availableBrands.Contains(SelectedBrand))
        {
            SelectedBrand = _availableBrands.First();
        }
    }

    private bool HasBrandMapping(MappedBrick brick, string brand)
    {
        return brand switch
        {
            "Lego" => !string.IsNullOrWhiteSpace(brick.LegoPartNum) && !string.IsNullOrWhiteSpace(brick.LegoName),
            "BlueBrixx" => !string.IsNullOrWhiteSpace(brick.BluebrixxPartNum) &&
                           !string.IsNullOrWhiteSpace(brick.BluebrixxName),
            "Cada" => !string.IsNullOrWhiteSpace(brick.CadaPartNum) && !string.IsNullOrWhiteSpace(brick.CadaName),
            "Pantasy" => !string.IsNullOrWhiteSpace(brick.PantasyPartNum) &&
                         !string.IsNullOrWhiteSpace(brick.PantasyName),
            "Mould King" => !string.IsNullOrWhiteSpace(brick.MouldKingPartNum) &&
                            !string.IsNullOrWhiteSpace(brick.MouldKingName),
            "Unknown" => !string.IsNullOrWhiteSpace(brick.UnknownPartNum) &&
                         !string.IsNullOrWhiteSpace(brick.UnknownName),
            _ => false
        };
    }

    private string GetBrickPartNumberForBrand(MappedBrick brick, string brand)
    {
        return brand switch
        {
            "Lego" => brick.LegoPartNum ?? "No ID",
            "BlueBrixx" => brick.BluebrixxPartNum ?? "No ID",
            "Cada" => brick.CadaPartNum ?? "No ID",
            "Pantasy" => brick.PantasyPartNum ?? "No ID",
            "Mould King" => brick.MouldKingPartNum ?? "No ID",
            "Unknown" => brick.UnknownPartNum ?? "No ID",
            _ => GetBrickPartNumber(brick)
        };
    }

    private string GetBrickNameForBrand(MappedBrick brick, string brand)
    {
        return brand switch
        {
            "Lego" => brick.LegoName ?? "Unknown Name",
            "BlueBrixx" => brick.BluebrixxName ?? "Unknown Name",
            "Cada" => brick.CadaName ?? "Unknown Name",
            "Pantasy" => brick.PantasyName ?? "Unknown Name",
            "Mould King" => brick.MouldKingName ?? "Unknown Name",
            "Unknown" => brick.UnknownName ?? "Unknown Name",
            _ => GetBrickName(brick)
        };
    }

    private void OnBrandChanged(string newBrand)
    {
        SelectedBrand = newBrand;
        StateHasChanged();
    }
}