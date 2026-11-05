using Microsoft.EntityFrameworkCore;
using RevalW.Services.TemplatesAPI.Model;

namespace RevalW.Services.TemplatesAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Config> Configs { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Config2Tag> Config2Tags { get; set; }
    public DbSet<DocumentsResourceFile> DocumentsResourceFiles { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<NewItemProperty> NewItemProperties { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Config>()
            .HasMany(e => e.Tags)
            .WithMany(e => e.Configs)
            .UsingEntity<Config2Tag>(
                j => j
                    .HasOne(it => it.Tag)
                    .WithMany()
                    .HasForeignKey(ot => ot.TagId),
                j => j
                    .HasOne(ot => ot.Config)
                    .WithMany()
                    .HasForeignKey(ot => ot.ConfigId),
                j =>
                {
                    j.HasKey(ot => new { ot.ConfigId, ot.TagId });
                    j.ToTable(nameof(Config2Tags));
                });

        modelBuilder.Entity<Config>()
            .HasMany(c => c.NewItemProperties)
            .WithOne()
            .HasForeignKey(dp => dp.ConfigId);
    }
}
