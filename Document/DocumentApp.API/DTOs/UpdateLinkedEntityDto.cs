using DocumentApp.Domain.Enums;

namespace DocumentApp.API.DTOs;

public class UpdateLinkedEntityDto
{
    public LinkedEntityType LinkedEntityType { get; set; }
    public Guid LinkedEntityId { get; set; }
}