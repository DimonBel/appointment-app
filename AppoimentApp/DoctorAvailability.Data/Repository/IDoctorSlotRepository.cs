using AppoimentApp.DoctorAvaliablity.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAvailability.Data.Repository
{
    public interface IDoctorSlotRepository
    {
        IQueryable<Slot> GetAllSlot();
        void AddSlot(Slot slot);
        int SaveChanges();
    }
}
