using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class AddMockToInventoryDialog
{
    [Parameter] public int MockId { get; set; }
    [Parameter] public string? MockSource { get; set; }
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    private bool _loading;
    private string? _error;
    private string? _mockType;

    private static string? NormalizeSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        return source.Trim().ToLower() switch
        {
            "bricklink" => "bricklink",
            "bricklinkxml" => "bricklink",
            "bricklink csv" => "bricklink",
            "bricklink-csv" => "bricklink",
            "rebrickable" => "rebrickable",
            "rebrickable csv" => "rebrickable",
            "rebrickablecsv" => "rebrickable",
            _ => source.Trim().ToLower()
        };
    }

    protected override Task OnInitializedAsync()
    {
        _mockType = NormalizeSource(MockSource);
        return Task.CompletedTask;
    }

    private async Task AddAllAsync()
    {
        _loading = true;
        _error = null;
        StateHasChanged(); // UI updaten für den Button-Disabled-Status

        // Globalen Spinner zeigen
        LoadingService.Show(message: "Adding MOC items to inventory...");

        try
        {
            // Kleiner Delay, damit der Spinner im UI erscheint
            await Task.Delay(100);

            if (string.IsNullOrWhiteSpace(_mockType))
            {
                LoadingService.Hide();
                _error = "Mock type is not set.";
                _loading = false;
                return;
            }

            var result = await InventoryService.AddMockItemsToInventoryAsync(MockId, _mockType);

            if (result)
            {
                // Erst Spinner weg, dann Dialog zu
                LoadingService.Hide();
                MudDialog?.Close(DialogResult.Ok(true));
            }
            else
            {
                LoadingService.Hide();
                _error = "Error adding mock items to inventory.";
                _loading = false;
            }
        }
        catch (Exception ex)
        {
            LoadingService.Hide();
            _error = "Exception: " + ex.Message;
            _loading = false;
        }
    }
}