using DoctorAppointmentManagement.Domain.RepositoryPort;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using DoctorAppointmentManagement.Domain;

namespace DoctorAppointmentManagement.Infrastructure.Repository
{
    public class FN_GetUpcomingAppoinmentRepository : IFN_GetUpcomingAppoinmentRepository
    {
        protected readonly ManagementDBContext _dbContext;
        public FN_GetUpcomingAppoinmentRepository(ManagementDBContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IQueryable<FN_GetUpcomingAppoinment> GetUpcomingAppoinment()
        {
            var result = _dbContext.FN_GetUpcomingAppoinments.FromSqlRaw
             (@$"SELECT * FROM [dbo].[FN_GetUpcomingAppoinments]");
            return result;
        }

    }
}
