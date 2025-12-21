using brickapp.Data.Services.Storage;

using MudBlazor.Services;
using brickapp.Data.Services.PartsListUpload;
using brickapp.Data;
using Microsoft.EntityFrameworkCore;
using brickapp.Data.Services;
using brickapp.Components;


var builder = WebApplication.CreateBuilder(args);
var cs = builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(cs))
    cs = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");

if (string.IsNullOrWhiteSpace(cs))
    throw new Exception("Connection string not set. Provide ConnectionStrings:Default or POSTGRES_CONNECTION.");

var connectionString = cs;



builder.Services.AddControllers();
// 1. Azure Verbindung holen
var blobConn = Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION");
if (string.IsNullOrWhiteSpace(blobConn))
    throw new Exception("AZURE_BLOB_CONNECTION not set");

// 2. Bilder IMMER in Azure (wie von dir gewünscht)
builder.Services.AddSingleton<IImageStorage>(sp => 
    new AzureBlobImageStorage(blobConn));

// 3. Export-Strategie nach Umgebung trennen
if (builder.Environment.IsDevelopment())
{
    // Lokal: In den Ordner auf der Platte
    builder.Services.AddSingleton<IExportStorage>(sp =>
    {
        var env = sp.GetRequiredService<IWebHostEnvironment>();
        var baseDir = Path.Combine(env.ContentRootPath, "mappedData");
        return new LocalExportStorage(baseDir);
    });
}
else
{
    // Produktion: In den Azure Container
    const string exportContainer = "brickapp"; 
    builder.Services.AddSingleton<IExportStorage>(_ =>
        new AzureBlobExportStorage(blobConn, exportContainer));
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
builder.Services.AddScoped<WantedListService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ItemSetService>();
builder.Services.AddScoped<ItemSetExportService>();
builder.Services.AddScoped<MappedBrickExportService>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<SetFavoritesService>();
builder.Services.AddScoped<TrackingService>();
builder.Services.AddHttpClient<RebrickableApiService>();

builder.Services.AddScoped<IPartsListUploadService, PartsListUploadService>();

builder.Services.AddScoped<IPartsListFormatParser, RebrickableCsvParser>();
builder.Services.AddScoped<IPartsListFormatParser, RebrickableXmlParser>();
builder.Services.AddScoped<IPartsListFormatParser, BricklinkXmlParser>();


// Global Services
builder.Services.AddSingleton<LoadingService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ImageService>();

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
var exportStorage = scope.ServiceProvider.GetRequiredService<IExportStorage>();
await RebrickableSeeder.SeedAsync(db, factory, exportStorage, builder.Environment.ContentRootPath);

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
