using DocumentApp.Domain.Entity;
using DocumentApp.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DocumentApp.Postgres.Data;

public class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentAccess> DocumentAccesses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Document entity
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MinioPath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.MinioBucket).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OwnerName).HasMaxLength(255);
            
            entity.HasIndex("OwnerId");
            entity.HasIndex("LinkedEntityType");
            entity.HasIndex("LinkedEntityId");
            entity.HasIndex("DocumentType");
            entity.HasIndex("IsDeleted");
            entity.HasIndex("CreatedAt");
        });

        // Configure DocumentAccess entity
        modelBuilder.Entity<DocumentAccess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).HasMaxLength(255);

            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.DocumentId, e.UserId }).IsUnique();

            entity.HasOne(d => d.Document)
                .WithMany(d => d.AccessControls)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Enable JSON support for PostgreSQL
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public override int SaveChanges()
    {
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}