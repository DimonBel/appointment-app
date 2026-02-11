using AppointmentBooking.Domain.Repository;
using AppointmentBooking.Domain.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentBooking.Application.ApplicationService
{
    public class ChangeAppoinmentStatusService : IChangeAppoinmentStatusService
    {
        private readonly IAppointmentBookingRepository _appointmentBookingRepository;
        public ChangeAppoinmentStatusService(IAppointmentBookingRepository appointmentBookingRepository)
        {
            _appointmentBookingRepository = appointmentBookingRepository;
        }

        public void ChangeAppoinmentStatus(Guid SlotId, int StatusId)
        {
            var slot = _appointmentBookingRepository.GetAllAppoinment().Where(c => c.SlotId == SlotId).FirstOrDefault();
            if (slot == null)
            {
                return;
            }

            slot.AppoinmentStatus = StatusId;
            _appointmentBookingRepository.SaveChanges();
        }
    }
}
