using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentBooking.Domain.Models
{
    public class AppoimentBookingModel
    {
            public DateTime ReservedAt { get; set; }
            public Guid SlotId { get; set; }
            public Guid PatientId { get; set; }
            public string PatientName { get; set; }
            public string DoctorName { get; set; }



    }
}
