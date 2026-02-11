using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAppointmentManagement.Domain.RepositoryPort
{
    public interface IFN_GetUpcomingAppoinmentRepository
    {
        IQueryable<FN_GetUpcomingAppoinment> GetUpcomingAppoinment();
    }
}
