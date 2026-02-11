using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAvailability.Business
{
    public interface IDoctorSlotService
    {
        IQueryable<SlotModel> GetAllSlots();
        IQueryable<SlotModel> GetAllAvaliableSlots();
        void AddSlot(SlotModel slotModel);
    }
}
