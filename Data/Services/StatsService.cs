using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Data; // Dein Namespace für den AppDbContext

namespace Services
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
    }

    public class BrickMappingStats
    {
        public int TotalBricks { get; set; }
        public int MappedCount { get; set; }
        public int UnmappedCount { get; set; }
    }
}