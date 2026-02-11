using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAppointmentManagement.Domain.Models
{
    public class SlotModel
    {
        public Guid SlotId { get; set; }
        public Guid PatientName { get; set; }
        public DateTime SlotDate { get; set; }
    }
}
