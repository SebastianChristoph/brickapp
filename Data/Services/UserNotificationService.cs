using brickapp.Data;
using brickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;
namespace brickapp.Data.Services
{
    public class UserNotificationService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public UserNotificationService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task DeleteNotificationAsync(int notificationId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var notification = await db.UserNotifications.FindAsync(notificationId);
            if (notification != null)
            {
                db.UserNotifications.Remove(notification);
                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteAllNotificationsAsync(string userUuid)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var notifications = await db.UserNotifications
                .Where(n => n.UserUuid == userUuid)
                .ToListAsync();

            if (notifications.Count > 0)
            {
                db.UserNotifications.RemoveRange(notifications);
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<UserNotification>> GetNotificationsForUserAsync(string userUuid)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.UserNotifications
                .AsNoTracking()
                .Where(n => n.UserUuid == userUuid)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task AddNotificationAsync(string userUuid, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var notification = new UserNotification
            {
                UserUuid = userUuid,
                Title = title,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                CreatedAt = DateTime.UtcNow
            };

            db.UserNotifications.Add(notification);
            await db.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var notification = await db.UserNotifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await db.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userUuid)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var notifications = await db.UserNotifications
                .Where(n => n.UserUuid == userUuid && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
                notification.IsRead = true;

            if (notifications.Count > 0)
                await db.SaveChangesAsync();
        }
    }
}
