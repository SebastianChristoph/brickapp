using brickapp.Components.Dialogs;
using brickapp.Data;
using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace brickapp.Components.Pages;

public partial class MockDetail
{
    private readonly int _itemsRowsPerPage = 25;
    private string? _error;
    private string? _errorDetails;
    private bool _isResolving;
    private bool _loading = true;
    private Mock? _mock;
    private string? _mockImageUrl;
    private Dictionary<(int, int), Dictionary<string, int>> _ownedByBrand = new();
    private List<MissingItem>? _resolvableMocItems = new();
    private bool _showOnlyMissing;
    private List<MockItem> _tableItems = new();
    private Guid _tableKey = Guid.NewGuid();
    [Parameter] public int MockId { get; set; }

    private int TotalNeeded => _mock?.Items.Sum(i => i.Quantity) ?? 0;
    private int TotalOwned => _mock?.Items.Sum(i => Math.Min(GetOwnedQuantity(i), i.Quantity)) ?? 0;

    private void SetError(Exception ex, string? context = null)
    {
        _error = context == null
            ? $"An error happened: {ex.Message}"
            : $"An error happened ({context}): {ex.Message}";
        _errorDetails = ex.ToString();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        try
        {
            await LoadMockAsync();
        }
        catch (Exception ex)
        {
            SetError(ex, "OnAfterRender");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task LoadMockAsync()
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _mock = db.Mocks
                .Include(m => m.Items)
                .ThenInclude(i => i.MappedBrick)
                .Include(m => m.Items)
                .ThenInclude(i => i.BrickColor)
                // NEU: Die fehlenden Teile direkt vom Mock aus laden
                .Include(m => m.MissingItems)
                .FirstOrDefault(m => m.Id == MockId);

            if (_mock != null && _mock.MissingItems.Any()) await CheckForResolvableItems();

            _mockImageUrl = _mock != null
                ? ImageService.GetMockImagePath(_mock)
                : ImageService.GetPlaceHolder();

            var inventory = await InventoryService.GetCurrentUserInventoryAsync();

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
        catch (Exception ex)
        {
            SetError(ex, "LoadMockAsync");
            NotificationService.Error(_error ?? "An unknown error occurred");
            _mock = null;
        }
    }

    private async Task ResolveMissingMocItems()
    {
        if (_resolvableMocItems != null && (!_resolvableMocItems.Any() || _mock == null)) return;

        _isResolving = true;
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Den Mock inkl. beider Listen laden
            var mockToUpdate = await db.Mocks
                .Include(m => m.Items)
                .Include(m => m.MissingItems)
                .FirstOrDefaultAsync(m => m.Id == MockId);

            if (mockToUpdate == null) return;

            // Wir gehen die Liste der rettbaren Items durch
            if (_resolvableMocItems != null)
            {
                foreach (var resolvable in _resolvableMocItems)
                {
                    // Entsprechenden Brick in DB finden
                    var brick = await db.MappedBricks
                        .FirstOrDefaultAsync(mb => mb.LegoPartNum == resolvable.ExternalPartNum);

                    if (brick != null)
                    {
                        // 1. Neues MockItem zur Items-Liste des Mocs hinzufügen
                        mockToUpdate.Items.Add(new MockItem
                        {
                            MockId = MockId,
                            MappedBrickId = brick.Id,
                            BrickColorId = resolvable.ExternalColorId,
                            Quantity = resolvable.Quantity,
                            ExternalPartNum = resolvable.ExternalPartNum
                        });

                        // 2. Das MissingItem aus der MissingItems-Liste des Mocs entfernen
                        // Wir suchen das passende Objekt innerhalb der getrackten Liste von mockToUpdate
                        var toRemove = mockToUpdate.MissingItems
                            .FirstOrDefault(mi => mi.Id == resolvable.Id);

                        if (toRemove != null) mockToUpdate.MissingItems.Remove(toRemove);
                    }
                }

                // Speichert die neuen MockItems UND löscht die entfernten MissingItems
                await db.SaveChangesAsync();

                NotificationService.Success($"{_resolvableMocItems.Count} items were successfully updated.");

                _resolvableMocItems.Clear();
            }

            await LoadMockAsync();
            UpdateTableItems();
        }
        catch (Exception ex)
        {
            NotificationService.Error("Failed to update MOC items.");
            Console.WriteLine(ex.Message);
        }
        finally
        {
            _isResolving = false;
            StateHasChanged();
        }
    }

    private async Task CheckForResolvableItems()
    {
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var partNums = _mock?.MissingItems.Select(m => m.ExternalPartNum).Distinct().ToList() ?? new List<string?>();

        // Prüfen, welche PartNums jetzt gemappt sind
        var foundPartNums = await db.MappedBricks
            .Where(mb => mb.LegoPartNum != null && partNums.Contains(mb.LegoPartNum))
            .Select(mb => mb.LegoPartNum)
            .ToListAsync();

        _resolvableMocItems = _mock?.MissingItems
            .Where(m => foundPartNums.Contains(m.ExternalPartNum))
            .ToList();
    }

    private void UpdateTableItems()
    {
        if (_mock?.Items == null)
        {
            _tableItems = new List<MockItem>();
            return;
        }

        var items = _mock.Items.AsEnumerable();

        if (_showOnlyMissing) items = items.Where(i => GetOwnedQuantity(i) < i.Quantity);

        _tableItems = items.ToList();
    }

    private int GetOwnedQuantity(MockItem item)
    {
        return _ownedByBrand.TryGetValue(
            (item.MappedBrickId ?? 0, item.BrickColorId ?? 0),
            out var brands)
            ? brands.Values.Sum()
            : 0;
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

    private async Task OpenAddMockDialog()
    {
        try
        {
            var parameters = new DialogParameters { ["MockId"] = MockId, ["MockSource"] = _mock?.MockType };
            var dialog =
                await DialogService.ShowAsync<AddMockToInventoryDialog>("Add MOC Items", parameters);
            var result = await dialog.Result;

            if (result != null && !result.Canceled && result.Data is bool ok && ok)
            {
                NotificationService.Success("MOC items successfully added.");
                Nav.NavigateTo("/myinventory");
            }
        }
        catch (Exception ex)
        {
            SetError(ex, "OpenAddMockDialog");
            NotificationService.Error(_error ?? "An unknown error occurred");
        }
    }

    private void GoBack()
    {
        Nav.NavigateTo("/mymocs");
    }

    private async Task OpenEditItemDialog(int mockItemId, int currentQuantity, int currentColorId)
    {
        try
        {
            var parameters = new DialogParameters
            {
                ["CurrentQuantity"] = currentQuantity,
                ["CurrentColorId"] = currentColorId
            };

            var dialog = await DialogService.ShowAsync<EditItemDialog>("Edit MOC Item", parameters);
            var result = await dialog.Result;

            if (result != null && !result.Canceled && result.Data is EditItemDialogResult editResult)
            {
                using var scope = ServiceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var mockItem = await db.MockItems.FindAsync(mockItemId);
                if (mockItem != null)
                {
                    mockItem.Quantity = editResult.Quantity;
                    mockItem.BrickColorId = editResult.ColorId;
                    await db.SaveChangesAsync();

                    NotificationService.Success("MOC item updated successfully.");
                    await LoadMockAsync();
                }
            }
        }
        catch (Exception ex)
        {
            SetError(ex, "OpenEditItemDialog");
            NotificationService.Error(_error ?? "An unknown error occurred");
        }
    }

    private async Task OpenAddInventoryDialog(MappedBrick? brick)
    {
        if (brick == null) return;

        var parameters = new DialogParameters<AddInventoryItemDialog>();
        parameters.Add(x => x.PreselectedBrick, brick);
        var dialog = await DialogService.ShowAsync<AddInventoryItemDialog>("Add item to inventory", parameters);
        await dialog.Result;
    }

    private async Task OpenAddToWantedListDialog(MappedBrick? brick)
    {
        if (brick == null) return;

        var parameters = new DialogParameters<AddToWantedListDialog>();
        parameters.Add(x => x.PreselectedBrick, brick);
        var dialog = await DialogService.ShowAsync<AddToWantedListDialog>("Add item to wanted list", parameters);
        await dialog.Result;
    }
}