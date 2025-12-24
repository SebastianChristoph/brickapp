using brickapp.Components.Dialogs;
using brickapp.Data.Entities;
using brickapp.Data.Services;
using MudBlazor;

namespace brickapp.Components.Pages;

public partial class Home
{
    private readonly string[] _chartLabels = ["Mapped", "Unmapped"];

    private readonly DialogOptions _dialogOptions = new()
    {
        BackdropClick = false,
        CloseButton = true,
        CloseOnEscapeKey = true
    };

    private readonly ChartOptions _options = new()
    {
        ChartPalette = ["#4CAF50", "#E0E0E0"] // Grün für Mapped, Grau für Unmapped
    };

    private double[] _chartData = [];
    private List<ItemSet>? _favoriteSets;
    private MappedBrick? _randomBrick;
    private List<RecentActivityItem>? _recentActivity;
    private BrickMappingStats? _stats;
    private string? _username;
    private UserStats? _userStats;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _username = await UserService.GetUsernameAsync();

        if (_username != null)
        {
            _userStats = await StatsService.GetUserStatsAsync();
            _randomBrick = await BrickService.GetRandomMappedBrickAsync();
            _favoriteSets = await SetFavoritesService.GetUserFavoriteSetListAsync();
            _recentActivity = await StatsService.GetRecentActivityAsync();
        }

        _stats = await StatsService.GetBrickMappingStatsAsync();

        if (_stats != null) _chartData = new double[] { _stats.MappedCount, _stats.UnmappedCount };

        StateHasChanged();
    }

    private async Task LoadRandomBrick()
    {
        try
        {
            _randomBrick = await BrickService.GetRandomMappedBrickAsync();
            StateHasChanged();
        }
        catch (Exception)
        {
            // Silent fail
        }
    }

    private async Task OpenHelpMappingDialog(string brand)
    {
        if (_randomBrick == null) return;

        var parameters = new DialogParameters();
        parameters.Add("Brick", _randomBrick);
        parameters.Add("Brand", brand);

        var dialog = await DialogService.ShowAsync<HelpMappingDialog>(
            $"Help mapping to {brand}",
            parameters,
            _dialogOptions);

        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is ValueTuple<string, string, string> tuple)
            await UpdateMappingAsync(_randomBrick, tuple.Item1, tuple.Item2, tuple.Item3);
    }

    private async Task UpdateMappingAsync(MappedBrick brick, string brand, string mappingName, string mappingItemId)
    {
        try
        {
            var userId = await UserService.GetTokenAsync();
            if (userId is null)
                throw new InvalidOperationException("User ist nicht eingeloggt (kein Token).");

            await RequestService.CreateMappingRequestAsync(brick.Id, brand, mappingName, mappingItemId, userId);
        }
        catch (Exception ex)
        {
            NotificationService.Error("Failed to create mapping request: " + ex.Message);
        }
    }

    private async Task OpenUploadImageDialog(MappedBrick brick)
    {
        var parameters = new DialogParameters();
        parameters.Add("Brick", brick);

        var dialog = await DialogService.ShowAsync<UploadItemImageDialog>(
            "Upload Item Image",
            parameters,
            _dialogOptions);

        var result = await dialog.Result;

        if (result is not null && !result.Canceled) await LoadRandomBrick(); // Refresh to show new image
    }

    private string GetRelativeTime(DateTime timestamp)
    {
        var timeSpan = DateTime.UtcNow - timestamp;

        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";

        return timestamp.ToString("MMM dd");
    }
}