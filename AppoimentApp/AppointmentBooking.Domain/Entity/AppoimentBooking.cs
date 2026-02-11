using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentBooking.Domain.Entity
{
    public class AppoimentBooking
    {
        public Guid Id { get; set; }
        public DateTime ReservedAt { get; set; }
        public Guid SlotId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }

        public int? AppoinmentStatus { get; set; }//1 for complete 2 for cancel

    }
}
