using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class HelpMappingDialog
{
    [Parameter] public MappedBrick? Brick { get; set; }
    [Parameter] public string? Brand { get; set; }
    [Parameter] public bool PartNumberOnlyMode { get; set; }

    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    private bool _searchingLegoPreview;
    private int _legoPreviewRequestId;
    private string _errorMessage = string.Empty;
    private List<MappedBrick> _allBricks = new();
    private List<MappedBrick> _legoPreviewBricks = new();

    protected override async Task OnInitializedAsync()
    {
        // Track dialog opening
        await TrackingService.TrackAsync("OpenHelpMappingDialog", $"Brick: {Brick?.Name}, Brand: {Brand}");

        // Load all mapped bricks for duplicate check
        _allBricks = await BrickService.GetAllMappedBricksAsync();
        _legoPreviewBricks = new List<MappedBrick>();

        // Wenn PartNumberOnlyMode aktiv ist, setze den Namen entsprechend der Brand
        if (PartNumberOnlyMode && Brick != null && !string.IsNullOrWhiteSpace(Brand))
        {
            MappingName = Brand switch
            {
                "Lego" => Brick.LegoName ?? string.Empty,
                "BlueBrixx" => Brick.BluebrixxName ?? string.Empty,
                "Cada" => Brick.CadaName ?? string.Empty,
                "Pantasy" => Brick.PantasyName ?? string.Empty,
                "Mould King" => Brick.MouldKingName ?? string.Empty,
                "Unknown" => Brick.UnknownName ?? string.Empty,
                _ => string.Empty
            };
        }
    }

    private async void OnMappingItemIdChanged(string value)
    {
        MappingItemId = value;

        // neue Request-ID (alles davor ist "alt")
        var reqId = ++_legoPreviewRequestId;

        if (!(Brand == "Lego" && !string.IsNullOrWhiteSpace(MappingItemId) && MappingItemId.Length >= 3))
        {
            _legoPreviewBricks = new();
            _searchingLegoPreview = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        _searchingLegoPreview = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            // Optional: kleine Debounce, damit nicht bei jedem Tastendruck sofort gefiltert wird
            await Task.Delay(150);

            // wenn inzwischen neu getippt wurde: abbrechen/ignorieren
            if (reqId != _legoPreviewRequestId) return;

            _legoPreviewBricks = _allBricks
                .Where(b => !string.IsNullOrWhiteSpace(b.LegoPartNum) &&
                            b.LegoPartNum.Contains(MappingItemId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(b => b.LegoPartNum!.Length)
                .ThenBy(b => b.LegoPartNum)
                .Take(10)
                .ToList();
        }
        finally
        {
            if (reqId == _legoPreviewRequestId)
            {
                _searchingLegoPreview = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private void SelectPreviewBrick(MappedBrick b)
    {
        _legoPreviewRequestId++;
        _searchingLegoPreview = false;

        MappingItemId = b.LegoPartNum ?? string.Empty;
        MappingName = b.LegoName ?? string.Empty;
        _legoPreviewBricks = new();
        StateHasChanged();
    }

    private string MappingName { get; set; } = string.Empty;
    private string MappingItemId { get; set; } = string.Empty;

    private void OnSave()
    {
        _errorMessage = string.Empty;
        if (Brick == null) return;

        // Im PartNumberOnlyMode muss nur die ItemId angegeben werden
        if (PartNumberOnlyMode)
        {
            if (string.IsNullOrWhiteSpace(MappingItemId))
            {
                _errorMessage = "Please enter an item part number.";
                return;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(MappingName) && string.IsNullOrWhiteSpace(MappingItemId))
            {
                _errorMessage = "Please enter a mapping name or item ID.";
                return;
            }
        }

        // Check for duplicate mapping for this brand and item ID or name
        if (!string.IsNullOrWhiteSpace(Brand))
        {
            bool idExists = !string.IsNullOrWhiteSpace(MappingItemId) && _allBricks.Any(b => b.Id != Brick.Id &&
                (Brand == "BlueBrixx" && b.BluebrixxPartNum == MappingItemId
                 || Brand == "Cada" && b.CadaPartNum == MappingItemId
                 || Brand == "Pantasy" && b.PantasyPartNum == MappingItemId
                 || Brand == "Mould King" && b.MouldKingPartNum == MappingItemId
                 || Brand == "Unknown" && b.UnknownPartNum == MappingItemId));

            // Im PartNumberOnlyMode prüfen wir NICHT ob der Name bereits existiert,
            // da wir ja bewusst eine PartNumber für einen existierenden Namen nachtragen
            bool nameExists = !PartNumberOnlyMode && !string.IsNullOrWhiteSpace(MappingName) && _allBricks.Any(b =>
                b.Id != Brick.Id &&
                (Brand == "BlueBrixx" && b.BluebrixxName == MappingName
                 || Brand == "Cada" && b.CadaName == MappingName
                 || Brand == "Pantasy" && b.PantasyName == MappingName
                 || Brand == "Mould King" && b.MouldKingName == MappingName
                 || Brand == "Unknown" && b.UnknownName == MappingName));

            if (idExists)
            {
                _errorMessage =
                    $"The item ID '{MappingItemId}' is already mapped for brand '{Brand}'. Please choose a different ID.";
                return;
            }

            if (nameExists)
            {
                _errorMessage =
                    $"The mapping name '{MappingName}' is already mapped for brand '{Brand}'. Please choose a different name.";
                return;
            }
        }

        NotificationService.Success(
            $"Mapping-Request was successfully created for '{MappingName}' ({Brand}) and now needs to be approved by an admin.");
        MudDialog?.Close(DialogResult.Ok((Brand, MappingName, MappingItemId)));
    }

    private void Cancel() => MudDialog?.Cancel();
}