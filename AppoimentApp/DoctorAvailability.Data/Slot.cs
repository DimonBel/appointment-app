using Microsoft.Extensions.Hosting;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AppoimentApp.DoctorAvaliablity.Data
{
    public class Slot
    {

        public Guid Id {  get; set; }
        public DateTime Date { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; }
        public bool IsReserved { get; set; }
        public decimal Cost { get; set; }

    }
}
