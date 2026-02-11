using DoctorAppointmentManagement.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAppointmentManagement.Infrastructure
{
    public class ManagementDBContext
    : IdentityDbContext
    {

        private readonly IHttpContextAccessor _httpContextAccessor;
        public ManagementDBContext(DbContextOptions<ManagementDBContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<FN_GetUpcomingAppoinment>(entity =>
            {
                entity.HasNoKey();
                entity.ToView(null);
            });
        }
        public DbSet<FN_GetUpcomingAppoinment> FN_GetUpcomingAppoinments { get; set; }
    }
}
