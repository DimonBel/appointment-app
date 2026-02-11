using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentBooking.Domain.Service
{
    public interface IChangeAppoinmentStatusService
    {
        void ChangeAppoinmentStatus(Guid SlotId, int StatusId);
    }
}
