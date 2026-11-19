using Microsoft.EntityFrameworkCore;
using RevalW.Services.DevicesAPI.Model;

namespace RevalW.Services.DevicesAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Device2Tag> Device2Tags { get; set; }
    public DbSet<GeneratedDocument> GeneratedDocuments { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Device>()
            .HasMany(e => e.Tags)
            .WithMany(e => e.Device)
            .UsingEntity<Device2Tag>(
                j => j
                    .HasOne(it => it.Tag)
                    .WithMany()
                    .HasForeignKey(ot => ot.TagId),
                j => j
                    .HasOne(ot => ot.Device)
                    .WithMany()
                    .HasForeignKey(ot => ot.DeviceId),
                j =>
                {
                    j.HasKey(ot => new { ot.DeviceId, ot.TagId });
                    j.ToTable(nameof(Device2Tag));
                });

        modelBuilder.Entity<Device>()
            .HasMany(i => i.GeneratedDocuments)
            .WithOne(gd => gd.Device)
            .HasForeignKey(gd => gd.DeviceId);

        modelBuilder.Entity<Device>()
            .Property(e => e.DeviceData)
            .HasColumnType("jsonb");
        modelBuilder.Entity<Device>()
            .Property(e => e.ConfigData)
            .HasColumnType("jsonb");
        modelBuilder.Entity<Device>()
            .Property(e => e.OwnerEmails)
            .HasColumnType("jsonb");

    }
}
