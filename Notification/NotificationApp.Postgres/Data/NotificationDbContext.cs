using Microsoft.EntityFrameworkCore;
using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;

namespace NotificationApp.Postgres.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<NotificationPreference> NotificationPreferences { get; set; } = null!;
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; } = null!;
    public DbSet<NotificationSchedule> NotificationSchedules { get; set; } = null!;
    public DbSet<NotificationEvent> NotificationEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ReferenceType).HasMaxLength(100);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Channel).HasConversion<int>();
            entity.Property(e => e.Priority).HasConversion<int>();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.UserId, e.Status });

            entity.HasOne(e => e.Template)
                .WithMany(t => t.Notifications)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // NotificationPreference
        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NotificationType).HasConversion<int>();
            entity.HasIndex(e => new { e.UserId, e.NotificationType }).IsUnique();
        });

        // NotificationTemplate
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
            entity.Property(e => e.TitleTemplate).IsRequired().HasMaxLength(500);
            entity.Property(e => e.BodyTemplate).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // NotificationSchedule
        modelBuilder.Entity<NotificationSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReferenceType).HasMaxLength(100);
            entity.Property(e => e.NotificationType).HasConversion<int>();
            entity.Property(e => e.TemplateData).HasColumnType("jsonb");
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => new { e.IsProcessed, e.IsCancelled });
        });

        // NotificationEvent
        modelBuilder.Entity<NotificationEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceService).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EventName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Payload).IsRequired().HasColumnType("jsonb");
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => e.IsProcessed);
            entity.HasIndex(e => e.ReceivedAt);
        });

        // Seed default templates
        SeedTemplates(modelBuilder);
    }

    private static void SeedTemplates(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationTemplate>().HasData(
            new NotificationTemplate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                Key = "order_created",
                Name = "Order Created",
                TitleTemplate = "New Appointment Request",
                BodyTemplate = "You have a new appointment request from {PatientName} for {AppointmentDate} at {AppointmentTime}.",
                Type = NotificationType.OrderCreated,
                IsActive = true
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                Key = "order_approved",
                Name = "Order Approved",
                TitleTemplate = "Appointment Approved",
                BodyTemplate = "Your appointment with {DoctorName} on {AppointmentDate} at {AppointmentTime} has been approved.",
                Type = NotificationType.OrderApproved,
                IsActive = true
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                Key = "order_declined",
                Name = "Order Declined",
                TitleTemplate = "Appointment Declined",
                BodyTemplate = "Your appointment with {DoctorName} on {AppointmentDate} has been declined. Reason: {Reason}",
                Type = NotificationType.OrderDeclined,
                IsActive = true
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111104"),
                Key = "order_cancelled",
                Name = "Order Cancelled",
                TitleTemplate = "Appointment Cancelled",
                BodyTemplate = "The appointment on {AppointmentDate} at {AppointmentTime} has been cancelled.",
                Type = NotificationType.OrderCancelled,
                IsActive = true
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111105"),
                Key = "order_reminder",
                Name = "Appointment Reminder",
                TitleTemplate = "Upcoming Appointment Reminder",
                BodyTemplate = "Reminder: You have an appointment with {DoctorName} on {AppointmentDate} at {AppointmentTime}.",
                Type = NotificationType.OrderReminder,
                IsActive = true
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111106"),
                Key = "chat_message",
                Name = "New Chat Message",
                TitleTemplate = "New Message",
                BodyTemplate = "You have a new message from {SenderName}.",
                Type = NotificationType.ChatMessage,
                IsActive = true
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111107"),
                Key = "order_rescheduled",
                Name = "Order Rescheduled",
                TitleTemplate = "Appointment Rescheduled",
                BodyTemplate = "Your appointment has been rescheduled to {AppointmentDate} at {AppointmentTime}.",
                Type = NotificationType.OrderRescheduled,
                IsActive = true
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111108"),
                Key = "order_completed",
                Name = "Order Completed",
                TitleTemplate = "Appointment Completed",
                BodyTemplate = "Your appointment with {DoctorName} on {AppointmentDate} has been marked as completed.",
                Type = NotificationType.OrderCompleted,
                IsActive = true
            }
        );
    }
}
