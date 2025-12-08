using MudBlazor.Services;
using brickisbrickapp.Components;
using brickisbrickapp.Data;
using Microsoft.EntityFrameworkCore;
using brickisbrickapp.Services;
using brickisbrickapp.Data.Services;

var builder = WebApplication.CreateBuilder(args);

// Pfad zur SQLite-Datei (bricksdb.db im Projektverzeichnis)
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "bricksdb.db");
var connectionString = $"Data Source={dbPath}";

// EF Core registrieren
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<UserService>();


builder.Services.AddScoped<MappedBrickService>();
builder.Services.AddScoped<UserNotificationService>();
builder.Services.AddScoped<RequestService>(); // RequestService bekommt UserNotificationService über DI


builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ItemSetService>();

// Global Loading Service
builder.Services.AddSingleton<LoadingService>();


// Rebrickable API Part Image Service
builder.Services.AddHttpClient<RebrickablePartImageService>();

// ItemUploadService: needs wwwroot path
builder.Services.AddScoped<ItemUploadService>(sp =>
    new ItemUploadService(Path.Combine(builder.Environment.ContentRootPath, "wwwroot")));

// MudBlazor
builder.Services.AddMudServices();
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

app.Run();
