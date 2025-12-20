using brickapp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Konfiguration so laden, wie EF sie zur Design-Time nutzen kann
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("Default")
                 ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");

        if (string.IsNullOrWhiteSpace(cs))
            throw new Exception("Connection string not set. Provide ConnectionStrings:Default or POSTGRES_CONNECTION.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new AppDbContext(options);
    }
}
