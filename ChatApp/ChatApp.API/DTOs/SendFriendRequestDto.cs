namespace ChatApp.API.DTOs;

/// <summary>
/// DTO for sending a friend request to another user
/// </summary>
public record SendFriendRequestDto(Guid AddresseeId);