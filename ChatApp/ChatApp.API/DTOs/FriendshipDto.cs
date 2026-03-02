namespace ChatApp.API.DTOs;

/// <summary>
/// DTO representing a friendship relationship between two users
/// </summary>
public record FriendshipDto(
    Guid Id,
    Guid RequesterId,
    Guid AddresseeId,
    string RequesterName,
    string AddresseeName,
    string RequesterEmail,
    string AddresseeEmail,
    string? RequesterAvatarUrl,
    string? AddresseeAvatarUrl,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);