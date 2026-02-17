namespace ChatApp.API.DTOs;

public record SendFriendRequestDto(Guid AddresseeId);

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

public record FriendStatusDto(
    Guid UserId,
    string Status, // "none", "pending_sent", "pending_received", "friends"
    Guid? FriendshipId
);
