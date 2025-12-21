using brickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace brickapp.Data.Services;

public class TrackingService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly UserService _userService;

    public TrackingService(IDbContextFactory<AppDbContext> dbFactory, UserService userService)
    {
        _dbFactory = dbFactory;
        _userService = userService;
    }

    /// <summary>
    /// Tracks an action/event
    /// </summary>
    public async Task TrackAsync(string action, string? details = null, string? pageUrl = null)
    {
        try
        {
            var userUuid = await _userService.GetTokenAsync();
            var user = await _userService.GetCurrentUserAsync();

            // Don't track admin actions
            if (user?.IsAdmin == true)
                return;

            await using var db = await _dbFactory.CreateDbContextAsync();

            var trackingInfo = new TrackingInfo
            {
                UserUuid = userUuid,
                AppUserId = user?.Id,
                Action = action,
                Details = details,
                PageUrl = pageUrl,
                CreatedAt = DateTime.UtcNow
            };

            db.TrackingInfos.Add(trackingInfo);
            await db.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Silent fail - tracking should never break the app
        }
    }

    /// <summary>
    /// Gets all tracking infos, ordered by creation date (newest first)
    /// </summary>
    public async Task<List<TrackingInfo>> GetAllTrackingInfosAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.TrackingInfos
            .AsNoTracking()
            .Include(t => t.AppUser)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets tracking infos for a specific user
    /// </summary>
    public async Task<List<TrackingInfo>> GetTrackingInfosByUserAsync(string userUuid)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.TrackingInfos
            .AsNoTracking()
            .Include(t => t.AppUser)
            .Where(t => t.UserUuid == userUuid)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets tracking infos grouped by user
    /// </summary>
    public async Task<Dictionary<string, List<TrackingInfo>>> GetTrackingInfosGroupedByUserAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var allTracking = await db.TrackingInfos
            .AsNoTracking()
            .Include(t => t.AppUser)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return allTracking
            .Where(t => !string.IsNullOrWhiteSpace(t.UserUuid))
            .GroupBy(t => t.UserUuid!)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Gets tracking stats (most common actions)
    /// </summary>
    public async Task<Dictionary<string, int>> GetActionStatsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.TrackingInfos
            .AsNoTracking()
            .GroupBy(t => t.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.Action, x => x.Count);
    }

    /// <summary>
    /// Deletes old tracking infos (older than specified days)
    /// </summary>
    public async Task CleanupOldTrackingAsync(int olderThanDays = 90)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        var oldTracking = await db.TrackingInfos
            .Where(t => t.CreatedAt < cutoffDate)
            .ToListAsync();

        db.TrackingInfos.RemoveRange(oldTracking);
        await db.SaveChangesAsync();
    }
}
