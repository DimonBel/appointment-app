namespace IdentityApp.Domain.DTOs;

public record UserStatisticsDto
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int InactiveUsers { get; init; }
    public int OnlineUsers { get; init; }
    public Dictionary<string, int> UsersByRole { get; init; } = new();
    public int UsersRegisteredToday { get; init; }
    public int UsersRegisteredThisWeek { get; init; }
    public int UsersRegisteredThisMonth { get; init; }
}