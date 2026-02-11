using AppointmentBooking.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentBooking.Domain.Service
{
    public interface IViewAvaliableSlotService
    {
        List<SlotModel> ViewAvaliableSlot();
    }
}
