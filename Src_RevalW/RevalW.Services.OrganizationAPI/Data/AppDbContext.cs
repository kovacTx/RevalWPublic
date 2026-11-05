using Microsoft.EntityFrameworkCore;
using RevalW.Services.OrganizationAPI.Model;

namespace RevalW.Services.OrganizationAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<NewUserProperty> NewUserProperties { get; set; }
    public DbSet<NewItemProperty> NewItemProperties { get; set; }
    public DbSet<NewDeviceProperty> NewDeviceProperties { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ItemGridColumn> ItemGridColumns { get; set; }
    public DbSet<DeviceGridColumn> DeviceGridColumns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organization>()
            .HasMany(o => o.Tags)
            .WithMany(t => t.Organizations)
            .UsingEntity<Organization2Tag>(
                j => j
                    .HasOne(ot => ot.Tag)
                    .WithMany()
                    .HasForeignKey(ot => ot.TagId),
                j => j
                    .HasOne(ot => ot.Organization)
                    .WithMany()
                    .HasForeignKey(ot => ot.OrganizationId),
                j =>
                {
                    j.HasKey(ot => new { ot.OrganizationId, ot.TagId });
                    j.ToTable(nameof(Organization2Tag));
                });

        //grid columns
        modelBuilder.Entity<Organization>()
            .HasMany(o => o.ItemGridColumns)
            .WithOne()
            .HasForeignKey(mgc => mgc.OrganizationId);
        modelBuilder.Entity<Organization>()
            .HasMany(o => o.DeviceGridColumns)
            .WithOne()
            .HasForeignKey(dgc => dgc.OrganizationId);

        //dynamic properties
        modelBuilder.Entity<Organization>()
            .HasMany(o => o.NewItemProperties)
            .WithOne()
            .HasForeignKey(dp => dp.ConfigId);
        modelBuilder.Entity<Organization>()
            .HasMany(o => o.NewUserProperties)
            .WithOne()
            .HasForeignKey(dp => dp.ConfigId);
        modelBuilder.Entity<Organization>()
            .HasMany(o => o.NewDeviceProperties)
            .WithOne()
            .HasForeignKey(dp => dp.ConfigId);
    }
}
