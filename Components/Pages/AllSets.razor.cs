namespace brickapp.Components.Pages;

public partial class AllSets
{
    private List<ItemSet>? _favoriteSets;
    private bool _loadingFavorites = true;
    private bool _searchingSets;
    private int _searchRequestId;
    private string _searchText = string.Empty;
    private List<ItemSet>? _sets;

    private List<ItemSet> FilteredSets =>
        string.IsNullOrWhiteSpace(_searchText) || _searchText.Length < 3
            ? _sets ?? new List<ItemSet>()
            : (_sets ?? new List<ItemSet>()).Where(s =>
                (s.Name?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (s.SetNum?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || s.Id.ToString().Contains(_searchText)
            ).ToList();

    private async Task OnSearchChanged(string value)
    {
        _searchText = value;

        var reqId = ++_searchRequestId;

        if (_searchText.Length < 3)
        {
            _searchingSets = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        _searchingSets = true;
        await InvokeAsync(StateHasChanged);

        await Task.Delay(200);

        if (reqId != _searchRequestId) return;

        _searchingSets = false;
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await UserService.GetUsernameAsync();
        // Load ALL sets for client-side pagination
        var (allSets, _) = await SetService.GetPaginatedItemSetsAsync(1, int.MaxValue);
        _sets = allSets;

        // Load favorite sets
        _favoriteSets = await SetFavoritesService.GetUserFavoriteSetListAsync();
        _loadingFavorites = false;

        // Track page visit
        await TrackingService.TrackAsync("ViewAllSets",
            $"Total Sets: {_sets?.Count ?? 0}, Favorites: {_favoriteSets?.Count ?? 0}", "/allsets");

        StateHasChanged();
    }
}