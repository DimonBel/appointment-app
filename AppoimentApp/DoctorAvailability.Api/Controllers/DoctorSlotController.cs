using DoctorAvailability.Business;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppoimentApp.DoctorAvaliablity.Presention
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorSlotController : ControllerBase
    {
        private readonly IDoctorSlotService _doctorSlotService;
        public DoctorSlotController(IDoctorSlotService doctorSlotService)
        {
            _doctorSlotService = doctorSlotService;
        }
        [HttpGet("all")]
        public IQueryable<SlotModel> GetAllSlots()
        {
            return _doctorSlotService.GetAllSlots();

        }
        [HttpGet("available")]
        public IQueryable<SlotModel> GetAllAvaliableSlots()
        {
            return _doctorSlotService.GetAllAvaliableSlots();

        }
        [HttpPost]
        public void AddSlot(SlotModel slot)
        {
            _doctorSlotService.AddSlot(slot);
        }
    }
}
