using Microsoft.EntityFrameworkCore;
using brickisbrickapp.Data.Entities;

namespace brickisbrickapp.Data;

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


    // --- Model-Konfiguration ---------------------------------------

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        // ----------------- Minimal-Seed: Admin-User -----------------

        modelBuilder.Entity<AppUser>().HasData(
            new AppUser
            {
                Id = 1,
                Uuid = "111",          // dein Admin-Token
                Name = "Admin",
                IsAdmin = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Alle LEGO-Parts, Sets, Farben usw. kommen NICHT hier über HasData,
        // sondern werden beim Start über den RebrickableSeeder aus den CSVs geladen.
    }
}
