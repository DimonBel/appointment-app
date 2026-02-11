using AppointmentBooking.Domain.Models;
using AppointmentBooking.Domain.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Presention.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChangeAppoinmentStatusController : ControllerBase
    {
        private readonly IChangeAppoinmentStatusService _changeAppoinmentStatusService;
        public ChangeAppoinmentStatusController(IChangeAppoinmentStatusService changeAppoinmentStatusService)
        {
            _changeAppoinmentStatusService = changeAppoinmentStatusService;
        }
        [HttpPut]
        public void ChangeAppoinmentStatus(Guid SlotId, int StatusId)
        {
            _changeAppoinmentStatusService.ChangeAppoinmentStatus(SlotId,StatusId);
        }
        }
}
