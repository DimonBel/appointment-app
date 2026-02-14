using AppointmentApp.Domain.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppointmentApp.Postgres.Data;

public class AppointmentDbContext : IdentityDbContext<AppIdentityUser, AppIdentityRole, Guid>
{
    public AppointmentDbContext(DbContextOptions<AppointmentDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderHistory> OrderHistory { get; set; }
    public DbSet<Professional> Professionals { get; set; }
    public DbSet<Availability> Availabilities { get; set; }
    public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
    public DbSet<DomainConfiguration> DomainConfigurations { get; set; }
    public DbSet<PreOrderData> PreOrderData { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Order entity
        builder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.DeclineReason).HasMaxLength(500);
            entity.Property(e => e.ApprovalReason).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).IsRequired(false);

            // Configure relationships
            entity.HasOne(o => o.Client)
                .WithMany()
                .HasForeignKey(o => o.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<AppIdentityUser>()
                .WithMany()
                .HasForeignKey(o => o.ProfessionalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.DomainConfiguration)
                .WithMany()
                .HasForeignKey(o => o.DomainConfigurationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(o => o.PreOrderData)
                .WithOne(p => p.Order)
                .HasForeignKey<Order>(o => o.PreOrderDataId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(o => o.OrderHistory)
                .WithOne(h => h.Order)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure OrderHistory entity
        builder.Entity<OrderHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(h => h.Order)
                .WithMany(o => o.OrderHistory)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Professional entity
        builder.Entity<Professional>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Qualifications).HasMaxLength(500);
            entity.Property(e => e.Specialization).HasMaxLength(200);
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).IsRequired(false);

            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Availabilities)
                .WithOne(a => a.Professional)
                .HasForeignKey(a => a.ProfessionalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Availability entity
        builder.Entity<Availability>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).IsRequired(false);

            entity.HasOne(a => a.Professional)
                .WithMany(p => p.Availabilities)
                .HasForeignKey(a => a.ProfessionalId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(a => a.Slots)
                .WithOne(s => s.Availability)
                .HasForeignKey(s => s.AvailabilityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AvailabilitySlot entity
        builder.Entity<AvailabilitySlot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).IsRequired(false);

            entity.HasOne(s => s.Availability)
                .WithMany(a => a.Slots)
                .HasForeignKey(s => s.AvailabilityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Order)
                .WithOne()
                .HasForeignKey<Order>(o => o.PreOrderDataId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure DomainConfiguration entity
        builder.Entity<DomainConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).IsRequired(false);

            entity.Property(e => e.RequiredFields)
                .HasColumnType("jsonb");
        });

        // Configure PreOrderData entity
        builder.Entity<PreOrderData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).IsRequired(false);

            entity.Property(e => e.DataFields)
                .HasColumnType("jsonb");

            entity.HasOne(p => p.Order)
                .WithOne(o => o.PreOrderData)
                .HasForeignKey<Order>(o => o.PreOrderDataId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.Client)
                .WithMany()
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure indexes for better query performance
        builder.Entity<Order>()
            .HasIndex(o => o.ClientId);

        builder.Entity<Order>()
            .HasIndex(o => o.ProfessionalId);

        builder.Entity<Order>()
            .HasIndex(o => o.Status);

        builder.Entity<Order>()
            .HasIndex(o => o.ScheduledDateTime);

        builder.Entity<Availability>()
            .HasIndex(a => a.ProfessionalId);

        builder.Entity<AvailabilitySlot>()
            .HasIndex(s => s.AvailabilityId);

        builder.Entity<AvailabilitySlot>()
            .HasIndex(s => s.SlotDate);

        builder.Entity<AvailabilitySlot>()
            .HasIndex(s => new { s.SlotDate, s.StartTime, s.EndTime });
    }
}