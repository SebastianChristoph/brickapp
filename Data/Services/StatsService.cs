using brickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using brickapp.Data; // Dein Namespace für den AppDbContext
namespace brickapp.Data.Services
{
    public class StatsService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly UserService _userService;

        public StatsService(IDbContextFactory<AppDbContext> contextFactory, UserService userService)
        {
            _contextFactory = contextFactory;
            _userService = userService;
        }

        public async Task<BrickMappingStats> GetBrickMappingStatsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            var totalCount = await context.MappedBricks.CountAsync();
            var mappedCount = await context.MappedBricks.CountAsync(b => b.HasAtLeastOneMapping);
            
            return new BrickMappingStats
            {
                TotalBricks = totalCount,
                MappedCount = mappedCount,
                UnmappedCount = totalCount - mappedCount
            };
        }

        public async Task<AdminStats> GetAdminStatsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var stats = new AdminStats
            {
                // Users
                TotalUsers = await context.Users.CountAsync(),

                // Mocks & WantedLists
                TotalMocks = await context.Mocks.CountAsync(),
                TotalWantedLists = await context.WantedLists.CountAsync(),

                // MappedBricks
                TotalMappedBricks = await context.MappedBricks.CountAsync(),
                MappedBricksWithMapping = await context.MappedBricks.CountAsync(b => b.HasAtLeastOneMapping),
                
                // ItemSets
                TotalItemSets = await context.ItemSets.CountAsync(),

                // Inventory
                TotalInventoryItems = await context.InventoryItems.CountAsync(),

                // Requests by Status - Pending
                PendingRequests = await context.MappingRequests.CountAsync(r => r.Status == MappingRequestStatus.Pending) +
                                  await context.NewItemRequests.CountAsync(r => r.Status == NewItemRequestStatus.Pending) +
                                  await context.NewSetRequests.CountAsync(r => r.Status == NewSetRequestStatus.Pending) +
                                  await context.ItemImageRequests.CountAsync(r => r.Status == ItemImageRequestStatus.Pending),

                // Requests by Status - Approved
                ApprovedRequests = await context.MappingRequests.CountAsync(r => r.Status == MappingRequestStatus.Approved) +
                                   await context.NewItemRequests.CountAsync(r => r.Status == NewItemRequestStatus.Approved) +
                                   await context.NewSetRequests.CountAsync(r => r.Status == NewSetRequestStatus.Approved) +
                                   await context.ItemImageRequests.CountAsync(r => r.Status == ItemImageRequestStatus.Approved),

                // Requests by Status - Rejected
                RejectedRequests = await context.MappingRequests.CountAsync(r => r.Status == MappingRequestStatus.Rejected) +
                                   await context.NewItemRequests.CountAsync(r => r.Status == NewItemRequestStatus.Rejected) +
                                   await context.NewSetRequests.CountAsync(r => r.Status == NewSetRequestStatus.Rejected) +
                                   await context.ItemImageRequests.CountAsync(r => r.Status == ItemImageRequestStatus.Rejected),

                // Requests by Type
                TotalMappingRequests = await context.MappingRequests.CountAsync(),
                TotalNewItemRequests = await context.NewItemRequests.CountAsync(),
                TotalNewSetRequests = await context.NewSetRequests.CountAsync(),
                TotalItemImageRequests = await context.ItemImageRequests.CountAsync()
            };

            stats.MappedBricksWithoutMapping = stats.TotalMappedBricks - stats.MappedBricksWithMapping;
            stats.TotalRequests = stats.TotalMappingRequests + stats.TotalNewItemRequests + 
                                  stats.TotalNewSetRequests + stats.TotalItemImageRequests;

            return stats;
        }

        public async Task<UserStats?> GetUserStatsAsync()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return null;

            await using var context = await _contextFactory.CreateDbContextAsync();

            var stats = new UserStats
            {
                TotalInventoryItems = await context.InventoryItems
                    .Where(i => i.AppUserId == user.Id)
                    .CountAsync(),
                
                TotalMocks = await context.Mocks
                    .Where(m => m.UserUuid == user.Uuid)
                    .CountAsync(),
                
                TotalWantedLists = await context.WantedLists
                    .Where(w => w.AppUserId == user.Id.ToString())
                    .CountAsync(),
                
                TotalFavoriteSets = await context.UserSetFavorites
                    .Where(f => f.AppUserId == user.Id)
                    .CountAsync(),
                
                TotalMappingRequests = await context.MappingRequests
                    .Where(r => r.RequestedByUserId == user.Uuid)
                    .CountAsync(),
                
                ApprovedMappingRequests = await context.MappingRequests
                    .Where(r => r.RequestedByUserId == user.Uuid && r.Status == MappingRequestStatus.Approved)
                    .CountAsync(),
                
                TotalNewItemRequests = await context.NewItemRequests
                    .Where(r => r.RequestedByUserId == user.Uuid)
                    .CountAsync(),
                
                ApprovedNewItemRequests = await context.NewItemRequests
                    .Where(r => r.RequestedByUserId == user.Uuid && r.Status == NewItemRequestStatus.Approved)
                    .CountAsync()
            };

            return stats;
        }

        public async Task<List<RecentActivityItem>> GetRecentActivityAsync(int count = 5)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var activities = new List<RecentActivityItem>();

            // Neue Items (approved)
            var newItems = await context.NewItemRequests
                .Where(r => r.Status == NewItemRequestStatus.Approved)
                .OrderByDescending(r => r.ApprovedAt)
                .Take(count)
                .Select(r => new RecentActivityItem
                {
                    Type = "New Item",
                    Description = $"{r.Brand}: {r.Name}",
                    Username = r.RequestedByUser != null ? r.RequestedByUser.Name : "Unknown",
                    Timestamp = r.ApprovedAt ?? r.CreatedAt
                })
                .ToListAsync();

            activities.AddRange(newItems);

            // Mappings (approved)
            var mappings = await context.MappingRequests
                .Where(r => r.Status == MappingRequestStatus.Approved)
                .OrderByDescending(r => r.ApprovedAt)
                .Take(count)
                .Select(r => new RecentActivityItem
                {
                    Type = "Mapping",
                    Description = $"{r.Brand}: {r.MappingName}",
                    Username = r.RequestedByUser != null ? r.RequestedByUser.Name : "Unknown",
                    Timestamp = r.ApprovedAt ?? r.CreatedAt
                })
                .ToListAsync();

            activities.AddRange(mappings);

            // Neue Sets (approved)
            var newSets = await context.NewSetRequests
                .Where(r => r.Status == NewSetRequestStatus.Approved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(count)
                .Select(r => new RecentActivityItem
                {
                    Type = "New Set",
                    Description = $"{r.Brand}: {r.SetName}",
                    Username = r.UserId,
                    Timestamp = r.CreatedAt
                })
                .ToListAsync();

            activities.AddRange(newSets);

            return activities
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToList();
        }
    }

    public class BrickMappingStats
    {
        public int TotalBricks { get; set; }
        public int MappedCount { get; set; }
        public int UnmappedCount { get; set; }
    }

    public class AdminStats
    {
        // Users
        public int TotalUsers { get; set; }

        // Mocks & WantedLists
        public int TotalMocks { get; set; }
        public int TotalWantedLists { get; set; }

        // MappedBricks
        public int TotalMappedBricks { get; set; }
        public int MappedBricksWithMapping { get; set; }
        public int MappedBricksWithoutMapping { get; set; }

        // ItemSets
        public int TotalItemSets { get; set; }

        // Inventory
        public int TotalInventoryItems { get; set; }

        // Requests by Status
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int TotalRequests { get; set; }

        // Requests by Type
        public int TotalMappingRequests { get; set; }
        public int TotalNewItemRequests { get; set; }
        public int TotalNewSetRequests { get; set; }
        public int TotalItemImageRequests { get; set; }
    }

    public class UserStats
    {
        public int TotalInventoryItems { get; set; }
        public int TotalMocks { get; set; }
        public int TotalWantedLists { get; set; }
        public int TotalFavoriteSets { get; set; }
        public int TotalMappingRequests { get; set; }
        public int ApprovedMappingRequests { get; set; }
        public int TotalNewItemRequests { get; set; }
        public int ApprovedNewItemRequests { get; set; }
    }

    public class RecentActivityItem
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}