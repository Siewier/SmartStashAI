using Microsoft.EntityFrameworkCore;
using SmartStashAI.Api.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SmartStashAI.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Household> Households { get; set; }
    public DbSet<ApplicationUser> Users { get; set; }
    public DbSet<StorageLocation> StorageLocations { get; set; }
    public DbSet<Item> Items { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Konfiguracja relacji samoodwołania (drzewo szafek)
        modelBuilder.Entity<StorageLocation>()
            .HasOne(l => l.ParentLocation)
            .WithMany(l => l.ChildLocations)
            .HasForeignKey(l => l.ParentLocationId)
            .OnDelete(DeleteBehavior.Cascade); // Usunięcie szafy usuwa informację o szufladach wewnątrz

        // Indeks na tokeny QR dla błyskawicznego wyszukiwania ze skanera
        modelBuilder.Entity<StorageLocation>()
            .HasIndex(l => l.QrCodeToken)
            .IsUnique();
    }
}