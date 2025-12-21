using Microsoft.EntityFrameworkCore;
using brickapp.Data.Entities;

namespace brickapp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // --- DbSets ----------------------------------------------------


    public DbSet<MappedBrick> MappedBricks => Set<MappedBrick>();
    public DbSet<BrickColor> BrickColors => Set<BrickColor>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ItemSet> ItemSets => Set<ItemSet>();
    public DbSet<ItemSetBrick> ItemSetBricks => Set<ItemSetBrick>();
    public DbSet<UserItemSet> UserItemSets => Set<UserItemSet>();
    public DbSet<MappingRequest> MappingRequests => Set<MappingRequest>();

    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<NewItemRequest> NewItemRequests => Set<NewItemRequest>();
    public DbSet<NewSetRequest> NewSetRequests => Set<NewSetRequest>();
    public DbSet<ItemImageRequest> ItemImageRequests => Set<ItemImageRequest>();

    public DbSet<Mock> Mocks => Set<Mock>();
    public DbSet<MockItem> MockItems => Set<MockItem>();

    // WantedList
    public DbSet<WantedList> WantedLists => Set<WantedList>();
    public DbSet<WantedListItem> WantedListItems => Set<WantedListItem>();

    // Set Favorites
    public DbSet<UserSetFavorite> UserSetFavorites => Set<UserSetFavorite>();

    // Tracking
    public DbSet<TrackingInfo> TrackingInfos => Set<TrackingInfo>();


    // --- Model-Konfiguration ---------------------------------------

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
            // NewSetRequest Tabelle
            modelBuilder.Entity<NewSetRequest>().ToTable("newSetRequests");
            modelBuilder.Entity<NewSetRequestItem>().ToTable("newSetRequestItems");

        modelBuilder.Entity<NewSetRequest>()
            .HasMany(nsr => nsr.Items)
            .WithOne()
            .HasForeignKey(i => i.NewSetRequestId)
            .OnDelete(DeleteBehavior.Cascade);

    // NewItemRequest Tabelle
            // NewItemRequest Tabelle
            modelBuilder.Entity<NewItemRequest>().ToTable("newItemRequests");

            // Beziehungen NewItemRequest
            modelBuilder.Entity<NewItemRequest>()
                .HasOne(nr => nr.RequestedByUser)
                .WithMany(u => u.NewItemRequestsRequested)
                .HasForeignKey(nr => nr.RequestedByUserId)
                .HasPrincipalKey(u => u.Uuid)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NewItemRequest>()
                .HasOne(nr => nr.ApprovedByUser)
                .WithMany(u => u.NewItemRequestsApproved)
            .HasPrincipalKey(u => u.Uuid)
            .OnDelete(DeleteBehavior.Restrict);

    // UserNotification Tabelle
        // UserNotification Tabelle
        modelBuilder.Entity<UserNotification>().ToTable("userNotifications");

        // Beziehung UserNotification -> AppUser (Uuid)
        modelBuilder.Entity<UserNotification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserUuid)
            .HasPrincipalKey(u => u.Uuid)
            .OnDelete(DeleteBehavior.Cascade);
            // MappingRequest Tabelle
            modelBuilder.Entity<MappingRequest>().ToTable("mappingRequests");

            // Beziehungen MappingRequest
            modelBuilder.Entity<MappingRequest>()
                .HasOne(mr => mr.Brick)
                .WithMany(b => b.MappingRequests)
                .HasForeignKey(mr => mr.BrickId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MappingRequest>()
                .HasOne(mr => mr.RequestedByUser)
                .WithMany(u => u.MappingRequestsRequested)
                .HasForeignKey(mr => mr.RequestedByUserId)
                .HasPrincipalKey(u => u.Uuid)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MappingRequest>()
                .HasOne(mr => mr.ApprovedByUser)
                .WithMany(u => u.MappingRequestsApproved)
                .HasPrincipalKey(u => u.Uuid)
                .OnDelete(DeleteBehavior.Restrict);

        // ItemImageRequest Tabelle und Beziehungen
        modelBuilder.Entity<ItemImageRequest>().ToTable("itemImageRequests");

        modelBuilder.Entity<ItemImageRequest>()
            .HasOne(iir => iir.MappedBrick)
            .WithMany()
            .HasForeignKey(iir => iir.MappedBrickId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemImageRequest>()
            .HasOne(iir => iir.RequestedByUser)
            .WithMany()
            .HasForeignKey(iir => iir.RequestedByUserId)
            .HasPrincipalKey(u => u.Uuid)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemImageRequest>()
            .HasOne(iir => iir.ApprovedByUser)
            .WithMany()
            .HasForeignKey(iir => iir.ApprovedByUserId)
            .HasPrincipalKey(u => u.Uuid)
            .OnDelete(DeleteBehavior.Restrict);

        // Tabellen-Namen
        // Tabellen-Namen
        modelBuilder.Entity<MappedBrick>().ToTable("mappedBricks");
        modelBuilder.Entity<BrickColor>().ToTable("colors");
        modelBuilder.Entity<InventoryItem>().ToTable("inventory");
        modelBuilder.Entity<AppUser>().ToTable("users");
        modelBuilder.Entity<ItemSet>().ToTable("itemsets");
        modelBuilder.Entity<ItemSetBrick>().ToTable("itemsetbricks");
        modelBuilder.Entity<UserItemSet>().ToTable("useritemsets");

        // ----------------- Beziehungen InventoryItem ----------------

        modelBuilder.Entity<InventoryItem>()
            .HasOne(i => i.MappedBrick)
            .WithMany(b => b.InventoryItems)
            .HasForeignKey(i => i.MappedBrickId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryItem>()
            .HasOne(i => i.BrickColor)
            .WithMany(c => c.InventoryItems)
            .HasForeignKey(i => i.BrickColorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryItem>()
            .HasOne(i => i.AppUser)
            .WithMany(u => u.InventoryItems)
            .HasForeignKey(i => i.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ----------------- Beziehungen ItemSetBrick -----------------

        modelBuilder.Entity<ItemSetBrick>()
            .HasOne(sb => sb.ItemSet)
            .WithMany(s => s.Bricks)
            .HasForeignKey(sb => sb.ItemSetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemSetBrick>()
            .HasOne(sb => sb.MappedBrick)
            .WithMany()
            .HasForeignKey(sb => sb.MappedBrickId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemSetBrick>()
            .HasOne(sb => sb.BrickColor)
            .WithMany()
            .HasForeignKey(sb => sb.BrickColorId)
            .OnDelete(DeleteBehavior.Restrict);

        // ----------------- Beziehungen UserItemSet ------------------

        modelBuilder.Entity<UserItemSet>()
            .HasOne(us => us.AppUser)
            .WithMany(u => u.UserItemSets)
            .HasForeignKey(us => us.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserItemSet>()
            .HasOne(us => us.ItemSet)
            .WithMany()
            .HasForeignKey(us => us.ItemSetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MissingItem>()
        .HasOne<WantedList>()
        .WithMany(w => w.MissingItems)
        .HasForeignKey("WantedListId") // Schatten-Eigenschaft, die EF generiert hat
        .OnDelete(DeleteBehavior.Cascade);
        
    // Das Gleiche für Mock, falls gewünscht
    modelBuilder.Entity<MissingItem>()
        .HasOne<Mock>()
        .WithMany(m => m.MissingItems)
        .HasForeignKey("MockId")
        .OnDelete(DeleteBehavior.Cascade);

        // ----------------- Minimal-Seed: Admin-User -----------------

        var adminUuid = Environment.GetEnvironmentVariable("ADMIN_UUID") ?? "111";
        var uweUuid = Environment.GetEnvironmentVariable("UWE_UUID") ?? "222";

        modelBuilder.Entity<AppUser>().HasData(
            new AppUser
            {
                Id = 1,
                Uuid = adminUuid,          // aus ENV
                Name = "Admin",
                IsAdmin = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AppUser
            {
                Id = 2,
                Uuid = uweUuid,            // aus ENV
                Name = "Uwe",
                IsAdmin = false,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Alle LEGO-Parts, Sets, Farben usw. kommen NICHT hier über HasData,
        // Alle LEGO-Parts, Sets, Farben usw. kommen NICHT hier über HasData,
        // sondern werden beim Start über den RebrickableSeeder aus den CSVs geladen.

        // ----------------- Mock & MockItem ------------------
        modelBuilder.Entity<Mock>().ToTable("mocks");
        modelBuilder.Entity<MockItem>().ToTable("mockitems");

        modelBuilder.Entity<Mock>()
            .HasMany(m => m.Items)
            .WithOne(i => i.Mock)
            .HasForeignKey(i => i.MockId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Mock>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserUuid)
            .HasPrincipalKey(u => u.Uuid)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MockItem>()
            .HasOne(mi => mi.MappedBrick)
            .WithMany()
            .HasForeignKey(mi => mi.MappedBrickId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MockItem>()
            .HasOne(mi => mi.BrickColor)
            .WithMany()
            .HasForeignKey(mi => mi.BrickColorId)
            .OnDelete(DeleteBehavior.Restrict);

        // ----------------- WantedList & WantedListItem ------------------

        modelBuilder.Entity<WantedList>().ToTable("wantedlists");
        modelBuilder.Entity<WantedListItem>().ToTable("wantedlistitems");

        // Explizite Beziehung: WantedList <-> WantedListItem
        modelBuilder.Entity<WantedList>()
            .HasMany(w => w.Items)
            .WithOne(i => i.WantedList)
            .HasForeignKey(i => i.WantedListId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WantedListItem>()
            .HasOne(i => i.MappedBrick)
            .WithMany()
            .HasForeignKey(i => i.MappedBrickId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WantedListItem>()
            .HasOne(i => i.BrickColor)
            .WithMany()
            .HasForeignKey(i => i.BrickColorId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }
}
