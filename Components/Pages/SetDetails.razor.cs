using brickapp.Components.Dialogs;
using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Pages;

public partial class SetDetails
{
    [Parameter] public int itemSetId { get; set; }
    private ItemSet? _itemSet;
    private UserItemSet? _userItemSet;
    private int _itemsPage = 0;
    private int _itemsRowsPerPage = 25;
    private bool _loading = true;
    private bool _showOnlyMissing = false;
    private bool _isFavorite = false;
    private Guid _tableKey = Guid.NewGuid();
    private List<ItemSetBrick> _tableItems = new();
    private Dictionary<(int BrickId, int ColorId), Dictionary<string, int>> _ownedByBrand = new();

    private enum BrickOwnershipState
    {
        None,
        Partial,
        Enough
    }

    private string? _error;
    private string? _errorDetails;

    private void SetError(Exception ex)
    {
        _error = $"An error happened: {ex.Message}";
        _errorDetails = ex.ToString();
    }

    protected int TotalNeeded => _itemSet?.Bricks?.Sum(b => b.Quantity) ?? 0;
    protected int TotalOwned => _itemSet?.Bricks?.Sum(b => Math.Min(GetOwnedQuantity(b), b.Quantity)) ?? 0;

    private void OnItemsPageChanged(int page)
    {
        _itemsPage = page;
    }

    private void OnItemsRowsPerPageChanged(int size)
    {
        _itemsRowsPerPage = size;
        _itemsPage = 0;
    }

    protected int Percent => (TotalNeeded > 0 && TotalOwned == TotalNeeded)
        ? 100
        : (TotalNeeded > 0 ? (int)Math.Floor(100.0 * (double)TotalOwned / TotalNeeded) : 0);

    protected bool HasAll => TotalNeeded > 0 && TotalOwned == TotalNeeded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task LoadDataAsync()
    {
        _userItemSet = await ItemSetService.GetCurrentUserItemSetAsync(itemSetId);

        if (_userItemSet != null)
            _itemSet = _userItemSet.ItemSet;
        else
            _itemSet = await ItemSetService.GetItemSetByIdAsync(itemSetId);

        // Check if set is in favorites
        _isFavorite = await SetFavoritesService.IsSetInUsersFavoritesAsync(itemSetId);

        var inventory = await InventoryService.GetCurrentUserInventoryAsync() ?? new List<InventoryItem>();

        _ownedByBrand = inventory
            .GroupBy(i => (i.MappedBrickId, i.BrickColorId))
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(i => i.Brand)
                    .ToDictionary(bg => bg.Key, bg => bg.Sum(i => i.Quantity))
            );

        UpdateTableItems();
        _tableKey = Guid.NewGuid();
    }

    private async Task OpenAddSetToWantedListDialog()
    {
        try
        {
            if (_itemSet?.Bricks == null || !_itemSet.Bricks.Any())
                return;

            var items = _itemSet.Bricks
                .Where(b => b.MappedBrickId > 0 && b.BrickColorId > 0 && b.Quantity > 0)
                .Select(b => new Data.DTOs.NewWantedListItemModel
                {
                    MappedBrickId = b.MappedBrickId,
                    ColorId = b.BrickColorId,
                    Quantity = b.Quantity
                })
                .ToList();

            var parameters = new DialogParameters<AddSetToWantedListDialog>();
            parameters.Add(x => x.SetName, _itemSet.Name);
            parameters.Add(x => x.Items, items);

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            await DialogService.ShowAsync<AddSetToWantedListDialog>("Wanted List", parameters, options);
        }
        catch (Exception ex)
        {
            SetError(ex);
            NotificationService.Error(_error ?? "An error happened.");
            StateHasChanged();
        }
    }

    private void UpdateTableItems()
    {
        var all = _itemSet?.Bricks ?? Enumerable.Empty<ItemSetBrick>();

        IEnumerable<ItemSetBrick> filtered = all;

        if (_showOnlyMissing)
        {
            // missing = nicht genug (None + Partial)
            filtered = all.Where(b => GetOwnershipState(b) != BrickOwnershipState.Enough);
        }

        _tableItems = filtered
            .OrderBy(b => b.BrickColor?.Name)
            .ToList();
    }

    private void ShowAllItems()
    {
        _showOnlyMissing = false;
        UpdateTableItems();
        _itemsPage = 0;
        _tableKey = Guid.NewGuid();
        StateHasChanged();
    }

    private void ShowOnlyMissingItems()
    {
        _showOnlyMissing = true;
        UpdateTableItems();
        _itemsPage = 0;
        _tableKey = Guid.NewGuid();
        StateHasChanged();
    }

    private void GoBack()
    {
        Nav.NavigateTo("/allsets");
    }

    private async Task OpenAddSetDialog()
    {
        try
        {
            if (_itemSet?.Bricks == null || !_itemSet.Bricks.Any())
                return;

            var itemCount = _itemSet.Bricks.Count;

            var parameters = new DialogParameters<AddSetToInventoryDialog>();
            parameters.Add(x => x.ItemCount, itemCount);
            parameters.Add(x => x.ItemSetId, itemSetId);

            var dialog = await DialogService.ShowAsync<AddSetToInventoryDialog>("Add Set to Inventory", parameters);
            var result = await dialog.Result;

            if (result != null && !result.Canceled && result.Data is bool confirmed && confirmed)
            {
                _loading = true;
                StateHasChanged();

                await LoadDataAsync();

                _loading = false;
                StateHasChanged();

                NotificationService.Success("Items were successfully added to the inventory.");
            }
        }
        catch (Exception ex)
        {
            SetError(ex);
            NotificationService.Error(_error ?? "An error happened.");
            StateHasChanged();
        }
    }

    private BrickOwnershipState GetOwnershipState(ItemSetBrick brick)
    {
        if (!_ownedByBrand.TryGetValue((brick.MappedBrickId, brick.BrickColorId), out var brandDict) ||
            brandDict.Values.Sum()
            <= 0)
            return BrickOwnershipState.None;

        var total = brandDict.Values.Sum();
        if (total < brick.Quantity)
            return BrickOwnershipState.Partial;

        return BrickOwnershipState.Enough;
    }

    private int GetOwnedQuantity(ItemSetBrick brick)
    {
        return _ownedByBrand.TryGetValue((brick.MappedBrickId, brick.BrickColorId), out var brandDict)
            ? brandDict.Values.Sum()
            : 0;
    }

    private async Task OpenHelpMappingDialog(MappedBrick brick, string brand)
    {
        var parameters = new DialogParameters();
        parameters.Add("Brick", brick);
        parameters.Add("Brand", brand);
        var dialog = await DialogService.ShowAsync<HelpMappingDialog>("Help with mapping", parameters);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled && result.Data is ValueTuple<string, string, string> tuple)
        {
            await UpdateMappingAsync(brick, tuple.Item1, tuple.Item2, tuple.Item3);
            await RefreshSetData();
        }
    }

    private async Task OpenHelpMappingDialogForPartNumber(MappedBrick brick, string brand)
    {
        var parameters = new DialogParameters();
        parameters.Add("Brick", brick);
        parameters.Add("Brand", brand);
        parameters.Add("PartNumberOnlyMode", true);
        var dialog = await DialogService.ShowAsync<HelpMappingDialog>("Help mapping the part number", parameters);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled && result.Data is ValueTuple<string, string, string> tuple)
        {
            await UpdateMappingAsync(brick, tuple.Item1, tuple.Item2, tuple.Item3);
            await RefreshSetData();
        }
    }

    private async Task OpenUploadImageDialog(MappedBrick brick)
    {
        var parameters = new DialogParameters();
        parameters.Add("Brick", brick);
        var dialogOptions = new DialogOptions { BackdropClick = false, CloseButton = true, CloseOnEscapeKey = true };
        var dialog =
            await DialogService.ShowAsync<UploadItemImageDialog>("Upload Item Image", parameters, dialogOptions);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            await RefreshSetData();
        }
    }

    private async Task UpdateMappingAsync(MappedBrick brick, string brand, string mappingName, string mappingItemId)
    {
        try
        {
            var userId = await UserService.GetTokenAsync();
            if (userId is null)
                throw new InvalidOperationException("User is not logged in (no token).");

            await RequestService.CreateMappingRequestAsync(brick.Id, brand, mappingName, mappingItemId, userId);
            NotificationService.Success(
                $"Mapping request for '{mappingName ?? mappingItemId}' ({brand}) was successfully created.");
        }
        catch (Exception ex)
        {
            SetError(ex);
            NotificationService.Error(_error ?? "Mapping request could not be created.");
        }
    }

    private async Task RefreshSetData()
    {
        try
        {
            _loading = true;
            StateHasChanged();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task ToggleFavorite()
    {
        if (_itemSet == null) return;

        try
        {
            var newFavoriteStatus = await SetFavoritesService.ToggleSetFavoriteAsync(itemSetId);
            _isFavorite = newFavoriteStatus;

            if (_isFavorite)
            {
                NotificationService?.Success($"'{_itemSet.Name}' was added to your favorites.");
            }
            else
            {
                NotificationService?.Success($"'{_itemSet.Name}' was removed from your favorites.");
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            SetError(ex);
            NotificationService?.Error(_error ?? "Error updating favorites.");
            StateHasChanged();
        }
    }

    private async Task OpenAddInventoryDialog(MappedBrick? brick)
    {
        if (brick == null) return;

        var parameters = new DialogParameters<AddInventoryItemDialog>();
        parameters.Add(x => x.PreselectedBrick, brick);
        var dialog = await DialogService.ShowAsync<AddInventoryItemDialog>("Add item to inventory", parameters);
        var result = await dialog.Result;
    }

    private async Task OpenAddToWantedListDialog(MappedBrick? brick)
    {
        if (brick == null) return;

        var parameters = new DialogParameters<AddToWantedListDialog>();
        parameters.Add(x => x.PreselectedBrick, brick);
        var dialog = await DialogService.ShowAsync<AddToWantedListDialog>("Add item to wanted list", parameters);
        var result = await dialog.Result;
    }
}