using brickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace brickapp.Data.Services;

public class SetFavoritesService(IDbContextFactory<AppDbContext> dbFactory, UserService userService)
{
    /// <summary>
    /// Adds an ItemSet to the user's favorites
    /// </summary>
    public async Task AddSetToFavoritesAsync(int itemSetId)
    {
        var user = await userService.GetCurrentUserAsync();
        if (user == null)
            throw new InvalidOperationException("User not found");

        await using var db = await dbFactory.CreateDbContextAsync();

        // Check if already exists
        var exists = await db.UserSetFavorites
            .AnyAsync(f => f.AppUserId == user.Id && f.ItemSetId == itemSetId);

        if (exists)
            return; // Already a favorite

        var favorite = new UserSetFavorite
        {
            AppUserId = user.Id,
            ItemSetId = itemSetId,
            CreatedAt = DateTime.UtcNow
        };

        db.UserSetFavorites.Add(favorite);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Removes an ItemSet from the user's favorites
    /// </summary>
    public async Task RemoveSetFromFavoritesAsync(int itemSetId)
    {
        var user = await userService.GetCurrentUserAsync();
        if (user == null)
            throw new InvalidOperationException("User not found");

        await using var db = await dbFactory.CreateDbContextAsync();

        var favorite = await db.UserSetFavorites
            .FirstOrDefaultAsync(f => f.AppUserId == user.Id && f.ItemSetId == itemSetId);

        if (favorite != null)
        {
            db.UserSetFavorites.Remove(favorite);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets all favorite ItemSets for the current user
    /// </summary>
    public async Task<List<ItemSet>> GetUserFavoriteSetListAsync()
    {
        var user = await userService.GetCurrentUserAsync();
        if (user == null)
            return new List<ItemSet>();

        await using var db = await dbFactory.CreateDbContextAsync();

        var favoriteSets = await db.UserSetFavorites
            .AsNoTracking()
            .Where(f => f.AppUserId == user.Id)
            .Include(f => f.ItemSet)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.ItemSet)
            .ToListAsync();

        return favoriteSets;
    }

    /// <summary>
    /// Checks if an ItemSet is in the user's favorites
    /// </summary>
    public async Task<bool> IsSetInUsersFavoritesAsync(int itemSetId)
    {
        var user = await userService.GetCurrentUserAsync();
        if (user == null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.UserSetFavorites
            .AnyAsync(f => f.AppUserId == user.Id && f.ItemSetId == itemSetId);
    }

    /// <summary>
    /// Gets all favorite ItemSet IDs for the current user (useful for bulk checking)
    /// </summary>
    public async Task<HashSet<int>> GetUserFavoriteSetIdsAsync()
    {
        var user = await userService.GetCurrentUserAsync();
        if (user == null)
            return new HashSet<int>();

        await using var db = await dbFactory.CreateDbContextAsync();

        var ids = await db.UserSetFavorites
            .AsNoTracking()
            .Where(f => f.AppUserId == user.Id)
            .Select(f => f.ItemSetId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    /// <summary>
    /// Toggles favorite status (adds if not present, removes if present)
    /// </summary>
    public async Task<bool> ToggleSetFavoriteAsync(int itemSetId)
    {
        var isFavorite = await IsSetInUsersFavoritesAsync(itemSetId);

        if (isFavorite)
        {
            await RemoveSetFromFavoritesAsync(itemSetId);
            return false; // Now not a favorite
        }
        else
        {
            await AddSetToFavoritesAsync(itemSetId);
            return true; // Now is a favorite
        }
    }
}
