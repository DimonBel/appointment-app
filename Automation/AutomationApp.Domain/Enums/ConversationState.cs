namespace AutomationApp.Domain.Enums;

public enum ConversationState
{
    Idle = 0,
    Greeting = 1,
    CollectingInfo = 2,
    SelectingService = 3,
    SelectingProfessional = 4,
    SelectingDateTime = 5,
    ConfirmingBooking = 6,
    BookingComplete = 7,
    FAQ = 8,
    Error = 9
}

public enum UserIntent
{
    Unknown = 0,
    BookAppointment = 1,
    CheckAvailability = 2,
    AskFAQ = 3,
    ViewServices = 4,
    CancelAppointment = 5,
    RescheduleAppointment = 6,
    GeneralInquiry = 7
}