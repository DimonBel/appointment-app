using IdentityApp.Domain.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityApp.Postgres.Data;

public class IdentityDbContext : IdentityDbContext<AppIdentityUser, AppIdentityRole, Guid>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure RefreshToken
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.JwtId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.JwtId);

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AppIdentityUser
        builder.Entity<AppIdentityUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.UserName).IsUnique();
        });

        // Configure AppIdentityRole
        builder.Entity<AppIdentityRole>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Seed default roles
        SeedRoles(builder);
    }

    private void SeedRoles(ModelBuilder builder)
    {
        var roles = new[]
        {
            new AppIdentityRole
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "Administrator role with full access",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new AppIdentityRole
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "User",
                NormalizedName = "USER",
                Description = "Regular user role",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new AppIdentityRole
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Professional",
                NormalizedName = "PROFESSIONAL",
                Description = "Professional service provider role",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            }
        };

        builder.Entity<AppIdentityRole>().HasData(roles);
    }
}
