using AppointmentBooking.Domain.Entity;
using AppointmentBooking.Domain.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentBooking.Infrastructure.Repository
{
    public class AppointmentBookingRepository : BaseRepository<AppoimentBooking>, IAppointmentBookingRepository
    {
        public AppointmentBookingRepository(AppoinmentBookingDBContext dbContext) : base(dbContext)
        {
        }
        public IQueryable<AppoimentBooking> GetAllAppoinment()
        {
            return this.GetAll();
        }


        public void AddAppoimentBooking(AppoimentBooking appoimentBooking) => this.Create(appoimentBooking);

        public int SaveChanges() => _dbContext.SaveChanges();
    }
}
