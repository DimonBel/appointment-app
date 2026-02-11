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
using AppoimentApp.DoctorAvaliablity.Data;
namespace DoctorAvailability.Data
{
    public class DoctorAvaliablityDBContext : IdentityDbContext
    {

        private readonly IHttpContextAccessor _httpContextAccessor;
        public DoctorAvaliablityDBContext(DbContextOptions<DoctorAvaliablityDBContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Slot>()
        .ToTable("Slots");
        }

        public override int SaveChanges()
        {

            return base.SaveChanges();
        }
        public DbSet<Slot> Slots { get; set; }
    }
}
