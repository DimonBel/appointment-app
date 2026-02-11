using AppointmentBooking.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentBooking.Domain.Repository
{
    public interface IAppointmentBookingRepository
    {
        IQueryable<AppoimentBooking> GetAllAppoinment();
        void AddAppoimentBooking(AppoimentBooking appoimentBooking);
        int SaveChanges();
    }
}
