namespace brickapp.Components.Layout;

public partial class NavMenu
{
    private bool _isAuthenticated;
    private bool _isAdmin;
    private bool _hasRendered;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_hasRendered)
        {
            _isAuthenticated = await UserService.IsAuthenticatedAsync();
            var user = await UserService.GetCurrentUserAsync();
            _isAdmin = user?.IsAdmin ?? false;

            _hasRendered = true;
            StateHasChanged();
        }
    }

    public async Task RefreshMenuAsync()
    {
        _isAuthenticated = await UserService.IsAuthenticatedAsync();
        var user = await UserService.GetCurrentUserAsync();
        _isAdmin = user?.IsAdmin ?? false;

        StateHasChanged();
    }
}