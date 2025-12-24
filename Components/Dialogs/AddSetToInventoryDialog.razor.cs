using brickapp.Data.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class AddSetToInventoryDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    [Parameter] public int ItemCount { get; set; }

    [Inject] public LoadingService LoadingService { get; set; } = null!;

    [Inject] public InventoryService InventoryService { get; set; } = null!;

    [Parameter] public int ItemSetId { get; set; }

    private bool _loading;

    private void Cancel()
    {
        if (!_loading)
            MudDialog?.Close(false);
    }

    private async Task ConfirmAsync()
    {
        _loading = true;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(100); // UI-Render-Pause für Spinner
        LoadingService.Show(message: "Adding set bricks to inventory...");
        try
        {
            await InventoryService.AddSetBricksToInventoryAsync(ItemSetId);
            MudDialog?.Close(true);
        }
        finally
        {
            LoadingService.Hide();
            _loading = false;
        }
    }
}