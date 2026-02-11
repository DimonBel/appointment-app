using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using AppoimentApp.DoctorAvaliablity.Data;

namespace DoctorAvailability.Data.Repository
{
    public class BaseRepository<T> : IBaseRepository<T>
          where T : Slot
    {
        protected readonly DoctorAvaliablityDBContext _dbContext;


        public BaseRepository(DoctorAvaliablityDBContext dbContext)
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

        public int SaveChanges()
        {
            return _dbContext.SaveChanges();
        }
    }
}
