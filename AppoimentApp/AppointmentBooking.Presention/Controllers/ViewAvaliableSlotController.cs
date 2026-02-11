using AppointmentBooking.Domain.Models;
using AppointmentBooking.Domain.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Presention.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ViewAvaliableSlotController : ControllerBase
    {
        private readonly IViewAvaliableSlotService _viewAvaliableSlotService;
        public ViewAvaliableSlotController(IViewAvaliableSlotService viewAvaliableSlotService)
        {
            _viewAvaliableSlotService = viewAvaliableSlotService;
        }
        [HttpGet]
        public List<SlotModel> ViewAvaliableSlot()
        {
           return _viewAvaliableSlotService.ViewAvaliableSlot();
        }
    }
}
