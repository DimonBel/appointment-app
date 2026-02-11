using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAppointmentManagement.Domain.ServicePort
{
    public interface IDoctorAppoinmentManagementService
    {
        IQueryable<FN_GetUpcomingAppoinment> UpcomingAppointments();
        void CancelAppointments(Guid SlotId);
        void UpdateAppointmentToComplete(Guid SlotId);
    }
}
