using DocumentApp.Domain.Enums;

namespace DocumentApp.API.DTOs;

public class GrantAccessDto
{
    public Guid UserId { get; set; }
    public AccessControlType AccessType { get; set; }
}