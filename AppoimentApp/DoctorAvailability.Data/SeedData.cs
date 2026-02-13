using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AppoimentApp.DoctorAvaliablity.Data;

namespace DoctorAvailability.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new DoctorAvaliablityDBContext(
                serviceProvider.GetRequiredService<DbContextOptions<DoctorAvaliablityDBContext>>(),
                serviceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>());

            // Check if data already exists
            if (context.Slots.Any())
            {
                return; // DB has been seeded
            }

            var doctorNames = new[]
            {
                "Dr. Sarah Johnson",
                "Dr. Michael Chen",
                "Dr. Emily Rodriguez",
                "Dr. James Williams",
                "Dr. Olivia Martinez",
                "Dr. David Kim",
                "Dr. Jessica Taylor",
                "Dr. Robert Anderson"
            };

            var costs = new[] { 75m, 100m, 125m, 150m, 200m };
            var random = new Random();
            var slots = new List<Slot>();

            // Create slots for the next 30 days
            for (int day = 0; day < 30; day++)
            {
                var date = DateTime.Now.AddDays(day);
                
                // Create 3-5 slots per day
                var slotsPerDay = random.Next(3, 6);
                
                for (int i = 0; i < slotsPerDay; i++)
                {
                    var doctorName = doctorNames[random.Next(doctorNames.Length)];
                    var hour = random.Next(9, 17); // 9 AM to 5 PM
                    var minute = random.Next(0, 2) * 30; // 0 or 30 minutes
                    
                    var slotDate = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
                    
                    // Only add future slots
                    if (slotDate > DateTime.Now)
                    {
                        slots.Add(new Slot
                        {
                            Id = Guid.NewGuid(),
                            Date = slotDate,
                            DoctorId = Guid.NewGuid(),
                            DoctorName = doctorName,
                            IsReserved = false, // Most slots are available
                            Cost = costs[random.Next(costs.Length)]
                        });
                    }
                }
            }

            // Mark some random slots as reserved (about 20%)
            var reservedCount = (int)(slots.Count * 0.2);
            for (int i = 0; i < reservedCount; i++)
            {
                var randomIndex = random.Next(slots.Count);
                slots[randomIndex].IsReserved = true;
            }

            context.Slots.AddRange(slots);
            await context.SaveChangesAsync();
        }
    }
}
