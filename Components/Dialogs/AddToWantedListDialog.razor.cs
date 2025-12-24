using brickapp.Data.DTOs;
using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class AddToWantedListDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    [Parameter] public MappedBrick? PreselectedBrick { get; set; }

    private string _errorMessage = "";
    private int _step = 1;
    private bool _loadingLists;
    private bool _saving;
    private string _error = "";
    private BrickSelectionDto? _selectedBrick;
    private string _selectedBrickName = "";
    private string _selectedColorName = "";
    private int _selectedQuantity;
    private int? _selectedWantedListId;
    private string _newListName = "";
    private List<WantedList> _wantedLists = new();

    private async Task HandleBrickSelected(BrickSelectionDto? selection)
    {
        _errorMessage = "";

        if (selection?.Brick == null || selection.BrickColorId <= 0 || selection.Quantity <= 0)
        {
            _errorMessage = "Invalid selection.";
            return;
        }

        _selectedBrick = selection;
        _selectedBrickName = selection.Brick.Name;
        _selectedQuantity = selection.Quantity;

        // Get color name
        var colors = await BrickService.GetAllColorsAsync();
        var color = colors.FirstOrDefault(c => c.Id == selection.BrickColorId);
        _selectedColorName = color?.Name ?? "Unknown";

        // Move to step 2
        _step = 2;
        await LoadWantedLists();
        StateHasChanged();
    }

    private async Task LoadWantedLists()
    {
        _loadingLists = true;
        try
        {
            _wantedLists = await WantedListService.GetCurrentUserWantedListsAsync();
        }
        finally
        {
            _loadingLists = false;
        }
    }

    private void BackToStep1()
    {
        _step = 1;
        _error = "";
        StateHasChanged();
    }

    private async Task Save()
    {
        _error = "";

        if (_selectedBrick == null)
        {
            _error = "No item selected.";
            return;
        }

        var createNew = !string.IsNullOrWhiteSpace(_newListName);

        if (!createNew && !_selectedWantedListId.HasValue)
        {
            _error = "Please select an existing wanted list OR enter a name for a new one.";
            return;
        }

        _saving = true;
        LoadingService?.Show();

        try
        {
            var itemToAdd = new NewWantedListItemModel
            {
                MappedBrickId = _selectedBrick.Brick.Id,
                BrickName = _selectedBrickName,
                ColorId = _selectedBrick.BrickColorId,
                Quantity = _selectedBrick.Quantity
            };

            bool ok;

            if (createNew)
            {
                var model = new NewWantedListModel
                {
                    Name = _newListName.Trim(),
                    Items = [itemToAdd]
                };

                ok = await WantedListService.CreateWantedListAsync(model);
            }
            else
            {
                ok = await WantedListService.AddItemsToWantedListAsync(
                    _selectedWantedListId!.Value,
                    new List<NewWantedListItemModel> { itemToAdd });
            }

            if (!ok)
            {
                _error = "Saving failed.";
                return;
            }

            NotificationService.Success($"{_selectedQuantity}x {_selectedBrickName} added to wanted list.");
            MudDialog?.Close(true);
        }
        catch (Exception ex)
        {
            _error = $"Error: {ex.Message}";
        }
        finally
        {
            LoadingService?.Hide();
            _saving = false;
        }
    }

    private void Cancel()
    {
        MudDialog?.Cancel();
    }
}