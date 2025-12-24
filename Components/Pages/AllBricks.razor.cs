using brickapp.Components.Dialogs;
using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Pages;

public partial class AllBricks
{
    private readonly DialogOptions _dialogOptions = new()
    {
        BackdropClick = false,
        CloseButton = true,
        BackgroundClass =
            "my-custom-class",
        CloseOnEscapeKey = true
    };

    private List<MappedBrick> _bricks = new();

    private int _currentPage;
    private int _loadRequestId;
    private string? _pageError;
    private int _pageSize = 25;
    private MappedBrick? _randomBrick;
    private bool _searchingBricks;
    private string _searchText = string.Empty;

    private bool _showHalloAlert = true;
    private bool _showMappedOnly;

    [Inject] private IDialogService DialogService { get; set; } = default!;

    private async Task ShowMappedItems()
    {
        ClearError();

        try
        {
            _showMappedOnly = true;
            _searchText = string.Empty;
            _currentPage = 0;

            var result = await BrickService.GetPaginatedMappedBricksAsync(
                1,
                10000,
                null,
                true);

            _bricks = result.Items;
        }
        catch (Exception ex)
        {
            SetError(ex, "Konnte gemappte Items nicht laden");
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadRandomBrick()
    {
        try
        {
            _randomBrick = await BrickService.GetRandomMappedBrickAsync();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            SetError(ex, "Konnte zufälliges Item nicht laden");
        }
    }

    private async Task OpenAddInventoryDialog(MappedBrick brick)
    {
        var parameters = new DialogParameters<AddInventoryItemDialog>();
        parameters.Add(x => x.PreselectedBrick, brick);
        var dialog =
            await DialogService.ShowAsync<AddInventoryItemDialog>("Add item to inventory", parameters, _dialogOptions);
        await dialog.Result;
        /* if (result is not null && result.Data is bool b && b)
        {
            NotificationService.Success("Item added to inventory!");
        } */
    }

    private async Task OpenAddToWantedListDialog(MappedBrick brick)
    {
        var parameters = new DialogParameters<AddToWantedListDialog>();
        parameters.Add(x => x.PreselectedBrick, brick);
        var dialog =
            await DialogService.ShowAsync<AddToWantedListDialog>("Add item to wanted list", parameters, _dialogOptions);
        await dialog.Result;
        /* if (result is not null && result.Data is bool b && b)
        {
            NotificationService.Success("Item added to wanted list!");
        } */
    }

    private void SetError(Exception ex, string context)
    {
        // Für User: kurze Message. (Details lieber in Logs)
        _pageError = $"{context}: {ex.Message}";
        Console.Error.WriteLine(ex); // optional: Logger nutzen
    }

    private void ClearError()
    {
        _pageError = null;
    }

    private async Task OpenAddItemDialog()
    {
        ClearError();
        try
        {
            var dialog = await DialogService.ShowAsync<AddItemDialog>("Add New Item", _dialogOptions);
            var result = await dialog.Result;

            if (result is not null && !result.Canceled)
                await RefreshBricks();
        }
        catch (Exception ex)
        {
            SetError(ex, "Dialog 'Add Item' fehlgeschlagen");
        }
    }

    private async Task OnSearchTextChanged(string value)
    {
        _searchText = value;
        if (!string.IsNullOrWhiteSpace(_searchText) && _searchText.Length < 3)
        {
            _loadRequestId++; // laufende Loads ungültig machen
            _searchingBricks = false;
            _bricks.Clear();
            await InvokeAsync(StateHasChanged);
            return;
        }

        _currentPage = 0;
        if (!string.IsNullOrWhiteSpace(_searchText) && _searchText.Length >= 3) _showMappedOnly = false;

        await LoadBricksAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        ClearError();

        try
        {
            await UserService.GetUsernameAsync();
            await LoadRandomBrick();
            await LoadBricksAsync();
        }
        catch (Exception ex)
        {
            SetError(ex, "Initiales Laden fehlgeschlagen");
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task OnRowsPerPageChanged(int size)
    {
        ClearError();
        try
        {
            _pageSize = size;
            _currentPage = 0;
            await LoadBricksAsync();
        }
        catch (Exception ex)
        {
            SetError(ex, "Rows-per-page Änderung fehlgeschlagen");
        }
    }

    private async Task LoadBricksAsync()
    {
        var reqId = ++_loadRequestId;

        _searchingBricks = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            await Task.Delay(150);
            if (reqId != _loadRequestId) return;

            ClearError();

            var result = await BrickService.GetPaginatedMappedBricksAsync(
                _currentPage + 1,
                _pageSize,
                _searchText,
                _showMappedOnly // <-- wichtig: Modus berücksichtigen
            );

            if (reqId != _loadRequestId) return;

            _bricks = result.Items;
        }
        catch (Exception ex)
        {
            if (reqId == _loadRequestId)
                SetError(ex, "Konnte Bricks nicht laden");
        }
        finally
        {
            if (reqId == _loadRequestId)
            {
                _searchingBricks = false;
                await InvokeAsync(StateHasChanged);
            }
        }
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
            await RefreshBricks();
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
            await RefreshBricks();
        }
    }

    private async Task OpenUploadImageDialog(MappedBrick brick)
    {
        var parameters = new DialogParameters();
        parameters.Add("Brick", brick);
        var dialog =
            await DialogService.ShowAsync<UploadItemImageDialog>("Upload Item Image", parameters, _dialogOptions);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled) await RefreshBricks();
    }

    private async Task UpdateMappingAsync(MappedBrick brick, string brand, string mappingName, string mappingItemId)
    {
        ClearError();
        try
        {
            var userId = await UserService.GetTokenAsync();
            if (userId is null)
                throw new InvalidOperationException("User ist nicht eingeloggt (kein Token).");

            await RequestService.CreateMappingRequestAsync(brick.Id, brand, mappingName, mappingItemId, userId);
            await RefreshBricks();
        }
        catch (Exception ex)
        {
            SetError(ex, "Mapping Request konnte nicht erstellt werden");
        }
    }

    private async Task RefreshBricks()
    {
        await LoadBricksAsync();
    }
}