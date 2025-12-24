using brickapp.Components.Dialogs;
using brickapp.Data.Entities;
using MudBlazor;

namespace brickapp.Components.Pages;

public partial class MyInventory
{
    private bool _authChecked;
    private bool _isAuth;
    private List<InventoryItem>? _items;

    private Dictionary<(int, int), List<string>> _wantedListNamesByItem = new();

    private static string BrandIdSelector(InventoryItem i)
    {
        var brand = i.Brand.ToLowerInvariant();
        return brand switch
        {
            "lego" => i.MappedBrick.LegoPartNum ?? string.Empty,
            "bluebrixx" => i.MappedBrick.BluebrixxPartNum ?? string.Empty,
            "cada" => i.MappedBrick.CadaPartNum ?? string.Empty,
            "pantasy" => i.MappedBrick.PantasyPartNum ?? string.Empty,
            "mould king" => i.MappedBrick.MouldKingPartNum ?? string.Empty,
            _ => string.Empty
        };
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _authChecked)
            return;

        _authChecked = true;

        _isAuth = await UserService.IsAuthenticatedAsync();

        if (!_isAuth)
        {
            Nav.NavigateTo("/login", true);
            return;
        }

        await UserService.GetUsernameAsync();
        _items = await InventoryService.GetCurrentUserInventoryAsync();

        // Track page visit
        await TrackingService.TrackAsync("ViewMyInventory", $"Items: {_items?.Count ?? 0}", "/myinventory");

        var user = await UserService.GetCurrentUserAsync();
        if (user != null)
            _wantedListNamesByItem = await WantedListService.GetWantedListNamesByBrickAndColorAsync(user.Id);
        else
            _wantedListNamesByItem = new Dictionary<(int, int), List<string>>();

        StateHasChanged();
    }

    private async Task OpenEditDialog(int itemId, int currentQuantity, int currentColorId)
    {
        var parameters = new DialogParameters<EditItemDialog>
        {
            { x => x.CurrentQuantity, currentQuantity },
            { x => x.CurrentColorId, currentColorId }
        };

        var dialog = await DialogService.ShowAsync<EditItemDialog>("Edit item", parameters);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is EditItemDialogResult editResult)
        {
            // Update mit neuer Quantity und ColorId
            var success =
                await InventoryService.UpdateInventoryItemAsync(itemId, editResult.Quantity, editResult.ColorId);
            if (success)
            {
                _items = await InventoryService.GetCurrentUserInventoryAsync();
                NotificationService.Success("Item updated successfully.");
                StateHasChanged();
            }
            else
            {
                NotificationService.Error("Failed to update item.");
            }
        }
    }

    private async Task OpenDeleteDialog(int itemId)
    {
        var parameters = new DialogParameters();
        var dialog = await DialogService.ShowAsync<DeleteConfirmDialog>("Delete item?", parameters);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is bool confirmed && confirmed)
        {
            await InventoryService.DeleteInventoryItemAsync(itemId);
            _items = await InventoryService.GetCurrentUserInventoryAsync();
            StateHasChanged();
        }
    }

    private async Task OpenAddItemDialog()
    {
        var dialog = await DialogService.ShowAsync<AddInventoryItemDialog>("Add item");
        var result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            _items = await InventoryService.GetCurrentUserInventoryAsync();
            NotificationService.Success("Item added to inventory.");
            StateHasChanged();
        }
    }

    private async Task OpenDeleteAllDialog()
    {
        var parameters = new DialogParameters();
        var dialog = await DialogService.ShowAsync<DeleteConfirmDialog>("Really delete entire inventory?", parameters);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is bool confirmed && confirmed)
        {
            await InventoryService.DeleteAllInventoryItemsAsync();
            _items = await InventoryService.GetCurrentUserInventoryAsync();
            StateHasChanged();
        }
    }
}