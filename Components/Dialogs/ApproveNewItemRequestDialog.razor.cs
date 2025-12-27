using brickapp.Data.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class ApproveNewItemRequestDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    [Parameter] public string? CurrentName { get; set; }
    [Parameter] public string? PartNum { get; set; }
    [Parameter] public string? Brand { get; set; }
    [Inject] private BricklinkScraperService BricklinkScraperService { get; set; } = null!;
    private string _name = "";
    private string _originalName = "";
    private bool _isFetching;
    private string? _fetchMessage;
    private bool _fetchSuccess;

    private bool NameChanged => !string.IsNullOrWhiteSpace(_name) &&
                                !string.IsNullOrWhiteSpace(_originalName) &&
                                _name.Trim() != _originalName.Trim();

    protected override void OnInitialized()
    {
        _name = CurrentName ?? "";
        _originalName = CurrentName ?? "";
    }

    private async Task FetchBricklinkName()
    {
        if (string.IsNullOrWhiteSpace(PartNum))
            return;

        _isFetching = true;
        _fetchMessage = null;
        StateHasChanged();

        try
        {
            var fetchedName = await BricklinkScraperService.GetBricklinkItemNameAsync(PartNum);

            if (!string.IsNullOrWhiteSpace(fetchedName))
            {
                _name = fetchedName;
                _fetchSuccess = true;
                _fetchMessage = "Successfully fetched name from Bricklink!";
            }
            else
            {
                _fetchSuccess = false;
                _fetchMessage = "Could not extract name from Bricklink page.";
            }
        }
        catch (Exception ex)
        {
            _fetchSuccess = false;
            _fetchMessage = $"Error fetching name: {ex.Message}";
        }
        finally
        {
            _isFetching = false;
            StateHasChanged();
        }
    }

    private void Cancel()
        => MudDialog?.Close(DialogResult.Cancel());

    private void Approve()
    {
        var result = new ApproveItemDialogResult
        {
            Name = _name.Trim(),
            NameChanged = NameChanged,
            OriginalName = _originalName.Trim()
        };
        MudDialog?.Close(DialogResult.Ok(result));
    }
}