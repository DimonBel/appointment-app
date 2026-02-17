namespace NotificationApp.Domain.Enums;

public enum NotificationType
{
    OrderCreated = 0,
    OrderApproved = 1,
    OrderDeclined = 2,
    OrderCancelled = 3,
    OrderCompleted = 4,
    OrderRescheduled = 5,
    OrderReminder = 6,
    ChatMessage = 7,
    SystemAlert = 8,
    ProfileUpdated = 9,
    NewProfessionalRegistered = 10,
    DocumentUploaded = 11,
    FriendRequest = 12,
    FriendRequestAccepted = 13,
    FriendRequestDeclined = 14,
    PasswordChanged = 15,
    BookingConfirmation = 16
}
