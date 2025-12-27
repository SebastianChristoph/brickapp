using brickapp.Components.Dialogs;
using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Pages;

public partial class MyWantedListDetails
{
    private readonly int _itemsRowsPerPage = 25;
    private bool _deletingList;
    private bool _isResolving;
    private bool _loading = true;
    private Dictionary<(int, int), Dictionary<string, int>> _ownedByBrand = new();
    private List<MissingItem> _resolvableItems = new();
    private bool _showOnlyMissing;
    private List<WantedListItem> _tableItems = new();
    private Guid _tableKey = Guid.NewGuid();
    private WantedList? _wantedList;
    [Parameter] public int WantedListId { get; set; }

    protected int TotalNeeded => _wantedList?.Items.Sum(b => b.Quantity) ?? 0;
    protected int TotalOwned => _wantedList?.Items.Sum(b => Math.Min(GetOwnedQuantity(b), b.Quantity)) ?? 0;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        _wantedList = await WantedListService.GetWantedListByIdAsync(WantedListId);

        await LoadData();
        _resolvableItems = await WantedListService.GetResolvableMissingItemsAsync(WantedListId);
        UpdateTableItems();
        _loading = false;
    }

    private async Task LoadData()
    {
        _wantedList = await WantedListService.GetWantedListByIdAsync(WantedListId);
        var inventory = await InventoryService.GetCurrentUserInventoryAsync();
        _ownedByBrand = inventory
            .GroupBy(i => (i.MappedBrickId, i.BrickColorId))
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(i => i.Brand).ToDictionary(bg => bg.Key, bg => bg.Sum(i => i.Quantity))
            );
    }

    private void UpdateTableItems()
    {
        if (_wantedList?.Items == null)
        {
            _tableItems = new List<WantedListItem>();
            return;
        }

        var items = _wantedList.Items.AsEnumerable();

        if (_showOnlyMissing) items = items.Where(i => GetOwnedQuantity(i) < i.Quantity);

        _tableItems = items.OrderBy(b => b.BrickColor?.Name).ToList();
    }

    private void ShowAllItems()
    {
        _showOnlyMissing = false;
        UpdateTableItems();
        _tableKey = Guid.NewGuid();
        StateHasChanged();
    }

    private void ShowOnlyMissingItems()
    {
        _showOnlyMissing = true;
        UpdateTableItems();
        _tableKey = Guid.NewGuid();
        StateHasChanged();
    }

    private async Task AddResolvableItems()
    {
        if (_resolvableItems.Count == 0) return;

        _isResolving = true;
        var ids = _resolvableItems.Select(i => i.Id).ToList();

        await WantedListService.ResolveMissingItemsAsync(WantedListId, ids);

        NotificationService.Success($"{ids.Count} items were successfully added to your list.");

        // Daten neu laden
        _resolvableItems.Clear();
        await LoadData();
        UpdateTableItems();
        _isResolving = false;
    }

    private async Task DeleteList()
    {
        _deletingList = true;
        await WantedListService.DeleteWantedListAsync(WantedListId);
        NotificationService.Success("Wanted List deleted successfully.");
        Nav.NavigateTo("/mywantedlists");
    }

    private async Task DeleteItem(int id)
    {
        await WantedListService.DeleteWantedListItemAsync(id);

        _wantedList = await WantedListService.GetWantedListByIdAsync(WantedListId);
        UpdateTableItems();
    }

    private async Task OpenEditItemDialog(int itemId, int currentQuantity, int currentColorId)
    {
        var parameters = new DialogParameters<EditItemDialog>
        {
            { x => x.CurrentQuantity, currentQuantity },
            { x => x.CurrentColorId, currentColorId }
        };

        var dialog = await DialogService.ShowAsync<EditItemDialog>("Edit item", parameters);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: EditItemDialogResult editResult })
        {
            var success =
                await WantedListService.UpdateWantedListItemAsync(itemId, editResult.Quantity, editResult.ColorId);
            if (success)
            {
                NotificationService.Success("Item updated successfully.");
                await LoadData();
                UpdateTableItems();
                StateHasChanged();
            }
            else
            {
                NotificationService.Error("Failed to update item.");
            }
        }
    }

    private int GetOwnedQuantity(WantedListItem item)
    {
        return _ownedByBrand.TryGetValue(
            (item.MappedBrickId, item.BrickColorId),
            out var brandDict)
            ? brandDict.Values.Sum()
            : 0;
    }

    private void GoBack()
    {
        Nav.NavigateTo("/mywantedlists");
    }
}