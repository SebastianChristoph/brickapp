using brickapp.Data.DTOs;
using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class AddInventoryItemDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    [Parameter] public MappedBrick? PreselectedBrick { get; set; }

    private string _errorMessage = "";

    private async Task HandleBrickAdded(BrickSelectionDto? selection)
    {
        _errorMessage = "";

        if (selection?.Brick == null || selection.BrickColorId <= 0 || selection.Quantity <= 0)
        {
            _errorMessage = "Invalid selection.";
            return;
        }

        var success = await InventoryService.AddInventoryItemAsync(
            selection.Brick.Id,
            selection.BrickColorId,
            selection.Brand,
            selection.Quantity);

        if (success)
        {
            NotificationService.Success(
                $"{selection.Quantity}x {selection.Brick.Name} ({selection.Brand}) added to inventory.");
            MudDialog?.Close(true);
        }
        else
        {
            _errorMessage = "Error adding item to inventory.";
        }
    }

    private void Cancel()
    {
        MudDialog?.Cancel();
    }
}