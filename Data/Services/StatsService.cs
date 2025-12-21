using brickapp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using brickapp.Data; // Dein Namespace für den AppDbContext
namespace brickapp.Data.Services
{
    public class StatsService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public StatsService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
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
}