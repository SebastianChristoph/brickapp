// API-Controller-Support aktivieren
using MudBlazor.Services;
using Data;
using Microsoft.EntityFrameworkCore;
using Services;
using brickapp.Components;
using Data.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Configuration / Environment
// ----------------------------
builder.Services.AddControllers();

var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new Exception("POSTGRES_CONNECTION environment variable is not set");
}

// ----------------------------
// Database (PostgreSQL)
// ----------------------------
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// ----------------------------
// Services
// ----------------------------
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<MappedBrickService>();
builder.Services.AddScoped<UserNotificationService>();
builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ItemSetService>();

builder.Services.AddScoped<ItemSetExportService>(sp =>
    new ItemSetExportService(
        sp.GetRequiredService<IDbContextFactory<AppDbContext>>(),
        Path.Combine(builder.Environment.ContentRootPath, "mappedData", "exported_sets.json")
    )
);

builder.Services.AddScoped<MappedBrickExportService>(sp =>
    new MappedBrickExportService(
        sp.GetRequiredService<IDbContextFactory<AppDbContext>>(),
        Path.Combine(builder.Environment.ContentRootPath, "mappedData", "exported_mappedbricks.json")
    )
);

// Global Services
builder.Services.AddSingleton<LoadingService>();
builder.Services.AddScoped<NotificationService>();

// ImageService (wwwroot)
builder.Services.AddScoped<ImageService>(sp =>
    new ImageService(
        sp.GetRequiredService<IWebHostEnvironment>().WebRootPath,
        sp.GetRequiredService<NotificationService>()
    )
);

// ----------------------------
// UI / Blazor / MudBlazor
// ----------------------------
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ----------------------------
// Database Migration & Seeding
// ----------------------------
using (var scope = app.Services.CreateScope())
{
  var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
await using var db = await factory.CreateDbContextAsync();

await db.Database.MigrateAsync();
await RebrickableSeeder.SeedAsync(db, factory, builder.Environment.ContentRootPath);
}

// ----------------------------
// HTTP Pipeline
// ----------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();
