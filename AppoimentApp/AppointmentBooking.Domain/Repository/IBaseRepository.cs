using AppointmentBooking.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentBooking.Domain.Repository
{
    public interface IBaseRepository<T>
    {
        IQueryable<T> GetAll();
        void Create(T obj);
    }
}