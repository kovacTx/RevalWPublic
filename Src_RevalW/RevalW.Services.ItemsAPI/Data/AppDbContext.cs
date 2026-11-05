using Microsoft.EntityFrameworkCore;
using RevalW.Services.ItemsAPI.Model;

namespace RevalW.Services.ItemsAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Item2Tag> Item2Tags { get; set; }
    public DbSet<ComboSource> ComboSources { get; set; }
    public DbSet<ComboItem> ComboItems { get; set; }
    public DbSet<CommonDataWrapper> CommonDataWrappers { get; set; }
    public DbSet<CommonDataItem> CommonDataItems { get; set; }
    public DbSet<GeneratedDocument> GeneratedDocuments { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Item>()
            .HasMany(e => e.Tags)
            .WithMany(e => e.Item)
            .UsingEntity<Item2Tag>(
                j => j
                    .HasOne(it => it.Tag)
                    .WithMany()
                    .HasForeignKey(ot => ot.TagId),
                j => j
                    .HasOne(ot => ot.Item)
                    .WithMany()
                    .HasForeignKey(ot => ot.ItemId),
                j =>
                {
                    j.HasKey(ot => new { ot.ItemId, ot.TagId });
                    j.ToTable(nameof(Item2Tag));
                });

        modelBuilder.Entity<Item>()
            .Property(e => e.ItemData)
            .HasColumnType("jsonb");
        modelBuilder.Entity<Item>()
            .Property(e => e.ConfigData)
            .HasColumnType("jsonb");
        modelBuilder.Entity<Item>()
            .Property(e => e.DeviceData)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Item>()
            .HasMany(i => i.GeneratedDocuments)
            .WithOne(gd => gd.Item)
            .HasForeignKey(gd => gd.ItemId);

        modelBuilder.Entity<CommonDataWrapper>()
            .HasMany(cw => cw.Items)
            .WithOne()
            .HasForeignKey(ci => ci.CommonDataWrapperId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ComboSource>()
            .HasMany(cs => cs.Items)
            .WithOne()
            .HasForeignKey(ci => ci.ComboSourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
