// ItemSetExportService für DI registrieren

// API-Controller-Support aktivieren
using MudBlazor.Services;
using Data;
using Microsoft.EntityFrameworkCore;
using Services;
using brickapp.Components;
using Data.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Pfad zur SQLite-Datei (bricksdb.db im Projektverzeichnis)
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "bricksdb.db");
var connectionString = $"Data Source={dbPath}";

// EF Core registrieren
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<UserService>();


builder.Services.AddScoped<MappedBrickService>();
builder.Services.AddScoped<UserNotificationService>();
builder.Services.AddScoped<RequestService>();

builder.Services.AddScoped<ItemSetExportService>(sp =>
    new ItemSetExportService(
        sp.GetRequiredService<AppDbContext>(),
        Path.Combine(builder.Environment.ContentRootPath, "mappedData", "exported_sets.json")
    )
);
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ItemSetService>();

// Global Loading Service
builder.Services.AddSingleton<LoadingService>();

// ImageService: needs wwwroot path
builder.Services.AddScoped<ImageService>(sp =>
    new ImageService(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot"),
        sp.GetRequiredService<NotificationService>()
    )
);

// MudBlazor
builder.Services.AddMudServices();
// MappedBrickExportService für DI registrieren
builder.Services.AddScoped<MappedBrickExportService>(sp =>
    new MappedBrickExportService(
        sp.GetRequiredService<AppDbContext>(),
        Path.Combine(builder.Environment.ContentRootPath, "mappedData", "exported_mappedbricks.json")
    )
);
// Global Notification Service
builder.Services.AddScoped<NotificationService>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// >>> DB erzeugen lassen (inkl. Tabellen & Seed-Daten)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Erstmal simpel: erstellt DB & Schema, falls nicht vorhanden
    db.Database.EnsureCreated();
    // Später kannst du auf db.Database.Migrate() umstellen, wenn du Migrations nutzt
      await RebrickableSeeder.SeedAsync(db, builder.Environment.ContentRootPath);
}

// Configure the HTTP request pipeline.
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
// API-Controller-Endpunkte aktivieren
app.MapControllers();

app.Run();
