using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<VehicleEntry> VehicleEntries => Set<VehicleEntry>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VehicleEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.VehicleType).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Plate).HasMaxLength(10).IsRequired();
            entity.Property(e => e.EntryTime).IsRequired();
            entity.Property(e => e.ExitTime);
            entity.Property(e => e.TotalMinutes);
            entity.Property(e => e.Fee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.EmailSent).HasDefaultValue(false);

            entity.HasIndex(e => e.Plate)
                  .HasDatabaseName("IX_VehicleEntry_Plate_Active")
                  .HasFilter("[ExitTime] IS NULL")
                  .IsUnique();
        });
    }
}
