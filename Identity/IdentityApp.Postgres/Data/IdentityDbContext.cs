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
    public DbSet<DoctorProfile> DoctorProfiles { get; set; }

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

        // Configure DoctorProfile
        builder.Entity<DoctorProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Specialty).HasMaxLength(200);
            entity.Property(e => e.Bio).HasMaxLength(2000);
            entity.Property(e => e.Qualifications).HasMaxLength(1000);
            entity.Property(e => e.Services).HasMaxLength(1000);
            entity.Property(e => e.Languages).HasMaxLength(500);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);

            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<DoctorProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.Specialty);
            entity.HasIndex(e => e.City);
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
            },
            new AppIdentityRole
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Patient",
                NormalizedName = "PATIENT",
                Description = "Patient role for medical appointments",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new AppIdentityRole
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Doctor",
                NormalizedName = "DOCTOR",
                Description = "Doctor role for medical professionals",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new AppIdentityRole
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Name = "Jurist",
                NormalizedName = "JURIST",
                Description = "Legal professional role",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new AppIdentityRole
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Name = "Management",
                NormalizedName = "MANAGEMENT",
                Description = "Management role for appointment operations oversight",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            }
        };

        builder.Entity<AppIdentityRole>().HasData(roles);
    }
}
