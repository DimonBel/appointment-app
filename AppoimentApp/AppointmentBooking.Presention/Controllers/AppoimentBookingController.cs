
using AppointmentBooking.Domain.Models;
using AppointmentBooking.Domain.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Presention.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppoimentBookingController : ControllerBase
    {
        private readonly IBookAppoimentService _bookAppoimentService;
        public AppoimentBookingController(IBookAppoimentService bookAppoimentService)
        {
            _bookAppoimentService = bookAppoimentService;
        }
        [HttpPost]
        public void BookAppoiment(AppoimentBookingModel appoimentBookingModel)
        {
            _bookAppoimentService.BookAppoiment(appoimentBookingModel);
        }
    }
}
