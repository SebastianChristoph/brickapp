
using brickapp.Data;
using brickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace brickapp.Data.Services
{
    public class UserService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IJSRuntime _js;
        private readonly NavigationManager _nav;

        private AppUser? _currentUser;

        private const string TokenKey = "authToken";
        private const string UsernameKey = "username";

        public UserService(IDbContextFactory<AppDbContext> dbFactory, IJSRuntime js, NavigationManager nav)
        {
            _dbFactory = dbFactory;
            _js = js;
            _nav = nav;
        }

            public async Task<AppUser?> AddUserAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            var uuid = Guid.NewGuid().ToString();
            var user = new AppUser
            {
                Uuid = uuid,
                Name = username,
                IsAdmin = false,
                CreatedAt = DateTime.UtcNow
            };

            await using var db = await _dbFactory.CreateDbContextAsync();
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await _js.InvokeAsync<string>("userAuth.getAuthToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                _currentUser = null;
                return false;
            }

            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Uuid == token);

            if (user == null)
            {
                await _js.InvokeVoidAsync("userAuth.clearAuth");
                _currentUser = null;
                return false;
            }

            _currentUser = user;
            await _js.InvokeVoidAsync("userAuth.setAuthInfo", user.Uuid, user.Name ?? string.Empty);
            return true;
        }

        public async Task<bool> LoginWithTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Uuid == token);

            if (user == null)
                return false;

            _currentUser = user;
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

        public async Task<string?> GetUserUuidAsync()
    {
        // Falls wir den User schon im Cache haben, direkt die Uuid geben
        if (_currentUser != null)
            return _currentUser.Uuid;

        // Ansonsten prüfen, ob ein Token im LocalStorage/Cookie ist
        var isAuth = await IsAuthenticatedAsync();
        return isAuth ? _currentUser?.Uuid : null;
    }

        public async Task<string?> GetUsernameAsync()
        {
            if (_currentUser != null)
                return _currentUser.Name;

            var isAuth = await IsAuthenticatedAsync();
            return isAuth ? _currentUser?.Name : null;
        }

        public async Task<string?> GetTokenAsync()
        {
            if (_currentUser != null)
                return _currentUser.Uuid;

            var isAuth = await IsAuthenticatedAsync();
            return isAuth ? _currentUser?.Uuid : null;
        }

        public async Task LogoutAsync()
        {
            _currentUser = null;
            await _js.InvokeVoidAsync("userAuth.clearAuth");
        }
    }
}