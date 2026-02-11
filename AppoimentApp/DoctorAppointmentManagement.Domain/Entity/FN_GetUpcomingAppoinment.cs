using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAppointmentManagement.Domain
{
    public class FN_GetUpcomingAppoinment
    {
        public Guid SlotId { get; set; }
        public DateTime Date { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
      
    }
}
