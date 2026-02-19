using Identity.Domain.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Postgres.Data;

public class IdentityDbContext : IdentityDbContext<AppIdentityUser, AppIdentityRole, Guid>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<AppIdentityUser> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure User entity with custom properties
        builder.Entity<AppIdentityUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);

            // Create index on email for faster lookups
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Role entity with custom properties
        builder.Entity<AppIdentityRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(500);

            // Create index on name for faster lookups
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
}
