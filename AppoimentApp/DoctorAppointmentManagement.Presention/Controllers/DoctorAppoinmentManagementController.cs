
using DoctorAppointmentManagement.Domain.ServicePort;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DoctorAppointmentManagement.Presention.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorAppoinmentManagementController : ControllerBase
    {
        private readonly IDoctorAppoinmentManagementService _doctorAppoinmentManagementService;
        public DoctorAppoinmentManagementController(IDoctorAppoinmentManagementService doctorAppoinmentManagementService)
        {
            _doctorAppoinmentManagementService = doctorAppoinmentManagementService;
        }
        [HttpPut("cancel")]
        public void CancelAppointments(Guid SlotId)
        {
            _doctorAppoinmentManagementService.CancelAppointments(SlotId);
        }
        [HttpPut("complete")]
        public void UpdateAppointmentToComplete(Guid SlotId)
        {
            _doctorAppoinmentManagementService.UpdateAppointmentToComplete(SlotId);
        }
    }
}
