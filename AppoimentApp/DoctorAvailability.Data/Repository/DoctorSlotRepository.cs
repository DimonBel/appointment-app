using AppoimentApp.DoctorAvaliablity.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace DoctorAvailability.Data.Repository
{
    public class DoctorSlotRepository : BaseRepository<Slot>, IDoctorSlotRepository
    {
        public DoctorSlotRepository(DoctorAvaliablityDBContext dbContext) : base(dbContext)
        {
        }

        public IQueryable<Slot> GetAllSlot()
        {
            var query = this.GetAll();

            return query;
        }

        public void AddSlot(Slot slot)
        {
            this.Create(slot);
        }

        public int SaveChanges() => _dbContext.SaveChanges();
    }
}
