namespace AutomationApp.Domain.Enums;

public enum ConversationState
{
    Idle = 0,
    Greeting = 1,
    CollectingInfo = 2,
    SelectingService = 3,
    SelectingProfessional = 4,
    SelectingDateTime = 5,
    SelectingTimeSlot = 6,
    ConfirmingBooking = 7,
    BookingComplete = 8,
    FAQ = 9,
    Error = 10
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