using AutomationApp.Domain.Entity;
using AutomationApp.Domain.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutomationApp.Postgres.Data;

public class AutomationDbContext : IdentityDbContext<AppIdentityUser, AppIdentityRole, Guid>
{
    public AutomationDbContext(DbContextOptions<AutomationDbContext> options) : base(options)
    {
    }

    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ConversationMessage> ConversationMessages { get; set; }
    public DbSet<BookingDraft> BookingDrafts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.ContextData).HasColumnType("jsonb");
            entity.Property(e => e.State).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<ConversationState>(v));
            entity.Property(e => e.DetectedIntent).HasConversion(
                v => v.ToString(),
                v => v != null ? Enum.Parse<UserIntent>(v) : (UserIntent?)null);
        });

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId);
            entity.Property(e => e.SuggestedOptions).HasColumnType("jsonb");
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingDraft>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.AdditionalData).HasColumnType("jsonb");
            entity.Property(e => e.Status).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<BookingDraftStatus>(v));
            entity.HasOne(e => e.Conversation)
                .WithOne()
                .HasForeignKey<BookingDraft>(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppIdentityUser>(entity =>
        {
            entity.ToTable("AspNetUsers");
        });

        modelBuilder.Entity<AppIdentityRole>(entity =>
        {
            entity.ToTable("AspNetRoles");
        });
    }
}