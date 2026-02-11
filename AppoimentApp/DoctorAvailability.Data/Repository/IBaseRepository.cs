using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAvailability.Data.Repository
{
    public interface IBaseRepository<T>
    {
        IQueryable<T> GetAll();
        void Create(T obj);
        int SaveChanges();
    }
}
