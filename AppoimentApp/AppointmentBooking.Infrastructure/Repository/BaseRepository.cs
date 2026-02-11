using AppointmentBooking.Domain.Entity;
using AppointmentBooking.Domain.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentBooking.Infrastructure.Repository
{
    public class BaseRepository<T> : IBaseRepository<T>
          where T : AppoimentBooking
    {
        protected readonly AppoinmentBookingDBContext _dbContext;


        public BaseRepository(AppoinmentBookingDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public virtual IQueryable<T> GetAll()
        {
            return _dbContext.Set<T>().AsQueryable();
        }

        public virtual void Create(T obj)
        {
            _dbContext.Attach(obj);
            _dbContext.Entry(obj).State = EntityState.Added;
        }
    }
}
