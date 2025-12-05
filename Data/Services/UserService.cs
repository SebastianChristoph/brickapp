using brickisbrickapp.Data;
using brickisbrickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace brickisbrickapp.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly IJSRuntime _js;
    private readonly NavigationManager _nav;

    private AppUser? _currentUser;
    private bool _hasTriedLoadFromStorage = false;

    private const string TokenKey = "authToken";
    private const string UsernameKey = "username";

    public UserService(AppDbContext db, IJSRuntime js, NavigationManager nav)
    {
        _db = db;
        _js = js;
        _nav = nav;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        if (_currentUser != null)
            return true;

        // Verhindern, dass bei jedem Call erneut aus localStorage gelesen wird
        if (_hasTriedLoadFromStorage)
            return _currentUser != null;

        _hasTriedLoadFromStorage = true;

        var token = await _js.InvokeAsync<string>("userAuth.getAuthToken");

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Uuid == token);

        if (user == null)
        {
            // Token ungültig -> localStorage aufräumen
            await _js.InvokeVoidAsync("userAuth.clearAuth");
            return false;
        }

        _currentUser = user;

        // sicherstellen, dass Username auch im Storage steht
        await _js.InvokeVoidAsync("userAuth.setAuthInfo", user.Uuid, user.Name ?? string.Empty);

        return true;
    }

    public async Task<bool> LoginWithTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Uuid == token);

        if (user == null)
        {
            // kein User für dieses Token
            return false;
        }

        _currentUser = user;
        _hasTriedLoadFromStorage = true;

        await _js.InvokeVoidAsync("userAuth.setAuthInfo", user.Uuid, user.Name ?? string.Empty);

        return true;
    }

    public async Task<AppUser?> GetCurrentUserAsync()
    {
        if (_currentUser != null)
            return _currentUser;

        var isAuth = await IsAuthenticatedAsync();
        return isAuth ? _currentUser : null;
    }

    public async Task<string?> GetUsernameAsync()
    {
        if (_currentUser != null)
            return _currentUser.Name;

        var isAuth = await IsAuthenticatedAsync();
        if (!isAuth) return null;

        return _currentUser?.Name;
    }

    public async Task<string?> GetTokenAsync()
    {
        if (_currentUser != null)
            return _currentUser.Uuid;

        var isAuth = await IsAuthenticatedAsync();
        if (!isAuth) return null;

        return _currentUser?.Uuid;
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        _hasTriedLoadFromStorage = false;
        await _js.InvokeVoidAsync("userAuth.clearAuth");
    }
}
