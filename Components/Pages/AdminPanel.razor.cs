using brickapp.Components.Dialogs;
using brickapp.Data.DTOs;
using brickapp.Data.Entities;
using brickapp.Data.Services;
using MudBlazor;

namespace brickapp.Components.Pages;

public partial class AdminPanel
{
    private string _createUserMessage = string.Empty;
    private string _deleteMessage = string.Empty;
    private bool _deleteSuccess;
    private bool _loading = true;
    private bool _loadingTracking = true;
    private string _newUserName = string.Empty;
    private AdminStats? _stats;
    private string _statusMessage = "";
    private Dictionary<string, List<TrackingInfo>>? _trackingByUser;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _stats = await StatsService.GetAdminStatsAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error loading statistics: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }

        // Load tracking data
        try
        {
            _trackingByUser = await TrackingService.GetTrackingInfosGroupedByUserAsync();
        }
        catch (Exception ex)
        {
            _statusMessage += $" Error loading tracking: {ex.Message}";
        }
        finally
        {
            _loadingTracking = false;
        }
    }

    private async Task DeleteAllTrackings()
    {
        try
        {
            await TrackingService.DeleteAllTrackingsAsync();
            _trackingByUser = new Dictionary<string, List<TrackingInfo>>();
            _statusMessage = "All tracking data deleted successfully.";
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error deleting tracking data: {ex.Message}";
        }
    }

    private async Task CreateUser()
    {
        _createUserMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(_newUserName))
        {
            _createUserMessage = "Please enter a username.";
            return;
        }

        var user = await UserService.AddUserAsync(_newUserName);
        if (user != null)
        {
            _createUserMessage = $"User created: {user.Name} (UUID: {user.Uuid})";
            _newUserName = string.Empty;

            // Refresh stats
            _stats = await StatsService.GetAdminStatsAsync();
        }
        else
        {
            _createUserMessage = "Error creating user.";
        }
    }

    private async Task ExportMappings()
    {
        try
        {
            var count = await MappedExport.ExportMappedBricksAsync();
            _statusMessage = $"Export successful: {count} mappings saved. (Path: {MappedExport.GetExportPath()})";
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error during export: {ex.Message}";
        }
    }

    private async Task ExportSets()
    {
        try
        {
            var count = await SetExport.ExportSetsAsync();
            _statusMessage = $"Sets export successful: {count} sets saved.";
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error during sets export: {ex.Message}";
        }
    }

    private async Task HandleDeleteBrick(BrickSelectionDto? selection)
    {
        if (selection?.Brick == null)
            return;

        var brick = selection.Brick;
        var brickName = brick.Name;
        var brickPartNum = brick.LegoPartNum ?? brick.BluebrixxPartNum ?? "Unknown";

        var message =
            $"Are you sure you want to permanently delete brick '{brickName}' (Part# {brickPartNum})? This action cannot be undone and will also delete all related mapping requests.";

        var parameters = new DialogParameters
        {
            ["Message"] = message
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small };
        var dialog = await DialogService.ShowAsync<DeleteBrickConfirmDialog>("Confirm Deletion", parameters, options);
        var result = await dialog.Result;

        if (result == null || result.Canceled)
            return;

        try
        {
            var success = await MappedBrickService.DeleteMappedBrickAsync(brick.Id);
            if (success)
            {
                _deleteSuccess = true;
                _deleteMessage = $"Successfully deleted brick '{brickName}' (Part# {brickPartNum})";
                NotificationService.Success($"Brick deleted: {brickName}");

                // Refresh stats
                _stats = await StatsService.GetAdminStatsAsync();
            }
            else
            {
                _deleteSuccess = false;
                _deleteMessage = "Failed to delete brick. It may not exist in the database.";
                NotificationService.Error("Delete failed");
            }
        }
        catch (Exception ex)
        {
            _deleteSuccess = false;
            _deleteMessage = $"Error deleting brick: {ex.Message}";
            NotificationService.Error($"Error: {ex.Message}");
        }

        StateHasChanged();
    }
}