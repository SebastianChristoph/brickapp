using brickapp.Components.Dialogs;
using brickapp.Components.Shared.PartsListUpload;
using brickapp.Data.DTOs;
using brickapp.Data.Entities;
using brickapp.Data.Services;
using MudBlazor;

namespace brickapp.Components.Pages;

public partial class MyWantedLists
{
    private readonly Dictionary<int, MappedBrick> _brickById = new();
    private readonly NewWantedListModel _newListModel = new();
    private List<MappedBrick> _allBricks = new();
    private bool _allBricksLoaded;
    private List<BrickColor> _brickColors = new();
    private string? _error;
    private string? _errorDetails;
    private readonly int _itemsRowsPerPage = 25;
    private bool _loading = true;

    private bool _saving;
    private List<WantedListService.WantedListSummary>? _wantedListSummaries;

    private void SetError(Exception ex, string? context = null)
    {
        _error = context == null
            ? $"An error happened: {ex.Message}"
            : $"An error happened ({context}): {ex.Message}";
        _errorDetails = ex.ToString();
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _loading = true;

            // parallel: Summaries + Colors
            var wantedTask = WantedListService.GetCurrentUserWantedListSummariesAsync();
            var colorsTask = MappedBrickService.GetAllColorsAsync();

            await Task.WhenAll(wantedTask, colorsTask);

            _wantedListSummaries = await wantedTask;
            _brickColors = await colorsTask;

            // Track page visit
            await TrackingService.TrackAsync("ViewMyWantedLists", $"Lists: {_wantedListSummaries?.Count ?? 0}",
                "/mywantedlists");

            // WICHTIG: _allBricks NICHT hier laden -> Performance!
        }
        catch (Exception ex)
        {
            SetError(ex, "OnInitializedAsync");
            _wantedListSummaries = new List<WantedListService.WantedListSummary>();
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task EnsureAllBricksLoadedAsync()
    {
        if (_allBricksLoaded) return;
        _allBricksLoaded = true;

        try
        {
            // Diese Query ist schwer – deshalb lazy
            _allBricks = await MappedBrickService.GetAllMappedBricksAsync();

            foreach (var b in _allBricks)
                _brickById[b.Id] = b;
        }
        catch (Exception ex)
        {
            // wenn das fehlschlägt, soll die Seite nicht komplett sterben
            SetError(ex, "LoadAllBricks");
            _allBricks = new List<MappedBrick>();
            _allBricksLoaded = false;
        }
    }

    private void GoToDetails(int wantedListId)
    {
        Nav.NavigateTo($"/mywantedlistdetails/{wantedListId}");
    }

    private async Task RemoveWantedList(int wantedListId)
    {
        try
        {
            await WantedListService.DeleteWantedListAsync(wantedListId);
            _wantedListSummaries = await WantedListService.GetCurrentUserWantedListSummariesAsync();
        }
        catch (Exception ex)
        {
            SetError(ex, "RemoveWantedList");
        }
        finally
        {
            StateHasChanged();
        }
    }

    private void HandleBrickAdded(BrickSelectionDto selection)
    {
        _brickById[selection.Brick.Id] = selection.Brick;

        AddOrMergeItem(_newListModel.Items, new NewWantedListItemModel
        {
            MappedBrickId = selection.Brick.Id,
            BrickName = selection.Brick.Name,
            ColorId = selection.BrickColorId,
            Quantity = selection.Quantity
        });

        NotificationService.Success("Item added to list.");
        StateHasChanged();
    }

    private void RemoveItem(NewWantedListItemModel item)
    {
        _newListModel.Items.Remove(item);
        StateHasChanged();
    }

    private async Task OpenEditItemDialog(NewWantedListItemModel item)
    {
        var parameters = new DialogParameters<EditItemDialog>
        {
            { x => x.CurrentQuantity, item.Quantity },
            { x => x.CurrentColorId, item.ColorId }
        };

        var dialog = await DialogService.ShowAsync<EditItemDialog>("Edit item", parameters);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is EditItemDialogResult editResult)
        {
            // Update das Item in der Liste
            item.Quantity = editResult.Quantity;
            item.ColorId = editResult.ColorId;

            NotificationService.Success("Item updated.");
            StateHasChanged();
        }
    }

    private async Task HandleCreateWantedList()
    {
        if (_saving) return;
        if (string.IsNullOrWhiteSpace(_newListModel.Name) || !_newListModel.Items.Any())
            return;

        _saving = true;
        if (_newListModel.UnmappedRows.Count > 0)
            LoadingService.Show("Saving wanted list and inform admin about missing items...");
        else
            LoadingService.Show("Saving wanted list...");

        try
        {
            var wantedListId = await WantedListService.CreateWantedListAndReturnIdAsync(_newListModel);
            if (wantedListId > 0)
            {
                NotificationService.Success("Wanted list created.");
                Nav.NavigateTo($"/mywantedlistdetails/{wantedListId}");
            }
        }
        catch (Exception ex)
        {
            SetError(ex, "HandleCreateWantedList");
            NotificationService.Error(_error ?? "An error happened.");
        }
        finally
        {
            LoadingService.Hide();
            _saving = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task OnUploadParsed(ParseResult<ParsedPart> result)
    {
        _newListModel.UnmappedRows = result.Unmapped;

        // Source sauber normalisieren (trim/lower/csv/xml weg)
        _newListModel.Source = WantedListService.NormalizeSource(result.AppliedFormat.ToString());

        return Task.CompletedTask;
    }

    private async void AddUploadItemsToNewList_FromComponent(List<ParsedPart> parts)
    {
        LoadingService.Show("Adding uploaded items to wanted list...");
        await EnsureAllBricksLoadedAsync();

        foreach (var p in parts)
        {
            _brickById.TryGetValue(p.MappedBrickId, out var brick);
            AddOrMergeItem(_newListModel.Items, new NewWantedListItemModel
            {
                MappedBrickId = p.MappedBrickId,
                BrickName = brick?.Name ?? $"Brick #{p.MappedBrickId}",
                ColorId = p.BrickColorId,
                Quantity = p.Quantity
            });
        }

        LoadingService.Hide();
        NotificationService.Success("Upload items added to list.");
        StateHasChanged();
    }

    private Task OnUploadCleared()
    {
        // optional: newListModel.UnmappedRows = new();
        return Task.CompletedTask;
    }

    private static void AddOrMergeItem(List<NewWantedListItemModel> list, NewWantedListItemModel item)
    {
        var existing = list.FirstOrDefault(x => x.MappedBrickId == item.MappedBrickId && x.ColorId == item.ColorId);
        if (existing != null)
        {
            existing.Quantity += item.Quantity;
            return;
        }

        list.Add(item);
    }
}