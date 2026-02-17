using System.ComponentModel.DataAnnotations;

namespace AppointmentApp.API.DTOs;

public class ApproveOrderDto
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}