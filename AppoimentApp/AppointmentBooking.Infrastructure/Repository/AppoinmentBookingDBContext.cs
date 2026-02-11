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
using AppointmentBooking.Domain;
using AppointmentBooking.Domain.Entity;

namespace AppointmentBooking.Infrastructure.Repository
{
    public class AppoinmentBookingDBContext : IdentityDbContext
    {

        private readonly IHttpContextAccessor _httpContextAccessor;
        public AppoinmentBookingDBContext(DbContextOptions<AppoinmentBookingDBContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<AppoimentBooking>()
        .ToTable("AppoimentBooking");
        }

        public override int SaveChanges()
        {

            return base.SaveChanges();
        }
        public DbSet<AppoimentBooking> AppoimentBookings { get; set; }
    }
}