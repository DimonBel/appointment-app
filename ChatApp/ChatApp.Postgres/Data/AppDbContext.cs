using ChatApp.Domain.Entity;
using ChatApp.Postgres.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Postgres.Data;

public class AppDbContext : IdentityDbContext<AppIdentityUser, AppIdentityRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);



        // Configure ChatMessage - relationships to AppIdentityUser (AspNetUsers table)
        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Define relationships to AppIdentityUser (AspNetUsers table)
            entity.HasOne<AppIdentityUser>()
                .WithMany()
                .HasForeignKey(e => e.SenderId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ChatMessages_AspNetUsers_SenderId");

            entity.HasOne<AppIdentityUser>()
                .WithMany()
                .HasForeignKey(e => e.ReceiverId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ChatMessages_AspNetUsers_ReceiverId");
            
            // Ignore the navigation properties that don't match the relationship
            entity.Ignore(e => e.Sender);
            entity.Ignore(e => e.Receiver);
        });
    }
}