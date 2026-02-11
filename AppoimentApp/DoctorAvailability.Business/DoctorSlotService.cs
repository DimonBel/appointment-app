using AppoimentApp.DoctorAvaliablity.Data;
using DoctorAvailability.Business;
using DoctorAvailability.Data.Repository;

namespace AppoimentApp.DoctorAvaliablity.Service
{
    public class DoctorSlotService : IDoctorSlotService
    {
        private readonly IDoctorSlotRepository _doctorSlotRepository;
        public DoctorSlotService(IDoctorSlotRepository doctorSlotRepository)
        {
            _doctorSlotRepository = doctorSlotRepository;
        }
        public IQueryable<SlotModel> GetAllSlots()
        {
            return _doctorSlotRepository.GetAllSlot().Select(c => new SlotModel()
            {
                Id = c.Id,
                DoctorId = c.DoctorId,
                Date = c.Date,
                Cost = c.Cost,
                DoctorName = c.DoctorName
            });
        }
        public IQueryable<SlotModel> GetAllAvaliableSlots()
        {
            return _doctorSlotRepository.GetAllSlot().Where(c => !c.IsReserved).Select(c => new SlotModel()
            {
                Id = c.Id,
                DoctorId = c.DoctorId,
                Date = c.Date,
                Cost = c.Cost,
                DoctorName = c.DoctorName
            });
        }
        public void AddSlot(SlotModel slotModel)
        {
            var slot = new Slot()
            {
                Id = Guid.NewGuid(),
                DoctorId = slotModel.DoctorId,
                Date = slotModel.Date,
                Cost = slotModel.Cost,
                DoctorName = slotModel.DoctorName,
                IsReserved = false
            };
            _doctorSlotRepository.AddSlot(slot);
            _doctorSlotRepository.SaveChanges();
        }
    }
}
