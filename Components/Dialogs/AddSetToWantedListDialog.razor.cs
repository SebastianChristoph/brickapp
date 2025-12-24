using brickapp.Data.DTOs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class AddSetToWantedListDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    [Parameter] public string? SetName { get; set; }
    [Parameter] public List<NewWantedListItemModel> Items { get; set; } = new();
    private bool _loadingLists = true;
    private bool _saving;
    private string _error = "";
    private int? _selectedWantedListId;
    private string _newListName = "";
    private List<Data.Entities.WantedList> _wantedLists = new();

    protected override async Task OnInitializedAsync()
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

    private void Cancel() => MudDialog?.Cancel();

    private async Task Save()
    {
        _error = "";

        if (Items.Count == 0)
        {
            _error = "This set has no items.";
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
            bool ok;

            if (createNew)
            {
                var model = new NewWantedListModel
                {
                    Name = _newListName.Trim(),
                    Items = Items
                };

                ok = await WantedListService.CreateWantedListAsync(model);
            }
            else
            {
                ok = await WantedListService.AddItemsToWantedListAsync(_selectedWantedListId!.Value, Items);
            }

            if (!ok)
            {
                _error = "Saving failed.";
                return;
            }

            NotificationService.Success("Wanted list successfully updated.");
            MudDialog?.Close(true);
        }
        finally
        {
            LoadingService?.Hide();
            _saving = false;
        }
    }
}