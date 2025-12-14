using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Services
{
    public class UserNotificationService

          
    {
        private readonly AppDbContext _db;
        public UserNotificationService(AppDbContext db)
        {
            _db = db;
        }


  public async Task DeleteNotificationAsync(int notificationId)
            {
                var notification = await _db.UserNotifications.FindAsync(notificationId);
                if (notification != null)
                {
                    _db.UserNotifications.Remove(notification);
                    await _db.SaveChangesAsync();
                }
            }

        public async Task DeleteAllNotificationsAsync(string userUuid)
        {
            var notifications = await _db.UserNotifications
                .Where(n => n.UserUuid == userUuid)
                .ToListAsync();
            if (notifications.Count > 0)
            {
                _db.UserNotifications.RemoveRange(notifications);
                await _db.SaveChangesAsync();
            }
        }
            
        public async Task<List<UserNotification>> GetNotificationsForUserAsync(string userUuid)
        {
            return await _db.UserNotifications
                .Where(n => n.UserUuid == userUuid)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task AddNotificationAsync(string userUuid, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null)
        {
            var notification = new UserNotification
            {
                UserUuid = userUuid,
                Title = title,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                CreatedAt = DateTime.UtcNow
            };
            _db.UserNotifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _db.UserNotifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _db.SaveChangesAsync();
            }
        }

            public async Task MarkAllAsReadAsync(string userUuid)
            {
                var notifications = await _db.UserNotifications
                    .Where(n => n.UserUuid == userUuid && !n.IsRead)
                    .ToListAsync();
                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }
                if (notifications.Count > 0)
                {
                    await _db.SaveChangesAsync();
                }
            }
    }
}
