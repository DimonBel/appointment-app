using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppointmentApp.Postgres.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppointmentDbContext context, UserManager<AppIdentityUser> userManager, RoleManager<AppIdentityRole> roleManager)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed roles
        await SeedRolesAsync(roleManager);

        // Seed users
        await SeedUsersAsync(userManager, roleManager);

        // Seed domain configurations
        await SeedDomainConfigurationsAsync(context);

        // Seed professionals
        await SeedProfessionalsAsync(context, userManager);

        // Seed availabilities
        await SeedAvailabilitiesAsync(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<AppIdentityRole> roleManager)
    {
        var roles = new[] { "Admin", "Professional", "Client" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new AppIdentityRole
                {
                    Name = roleName,
                    Description = $"{roleName} role"
                };
                await roleManager.CreateAsync(role);
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<AppIdentityUser> userManager, RoleManager<AppIdentityRole> roleManager)
    {
        // Admin user
        if (await userManager.FindByEmailAsync("admin@appointment.com") == null)
        {
            var admin = new AppIdentityUser
            {
                UserName = "admin@appointment.com",
                Email = "admin@appointment.com",
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Professional user
        if (await userManager.FindByEmailAsync("doctor@appointment.com") == null)
        {
            var doctor = new AppIdentityUser
            {
                UserName = "doctor@appointment.com",
                Email = "doctor@appointment.com",
                FirstName = "Dr. John",
                LastName = "Smith",
                PhoneNumber = "+1234567890",
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(doctor, "Doctor123!");
            await userManager.AddToRoleAsync(doctor, "Professional");
        }

        // Additional Professional Users
        if (await userManager.FindByEmailAsync("sarah.johnson@appointment.com") == null)
        {
            var doctor = new AppIdentityUser
            {
                UserName = "sarah.johnson@appointment.com",
                Email = "sarah.johnson@appointment.com",
                FirstName = "Dr. Sarah",
                LastName = "Johnson",
                PhoneNumber = "+1234567891",
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(doctor, "Doctor123!");
            await userManager.AddToRoleAsync(doctor, "Professional");
        }

        if (await userManager.FindByEmailAsync("michael.chen@appointment.com") == null)
        {
            var doctor = new AppIdentityUser
            {
                UserName = "michael.chen@appointment.com",
                Email = "michael.chen@appointment.com",
                FirstName = "Dr. Michael",
                LastName = "Chen",
                PhoneNumber = "+1234567892",
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(doctor, "Doctor123!");
            await userManager.AddToRoleAsync(doctor, "Professional");
        }

        if (await userManager.FindByEmailAsync("emily.rodriguez@appointment.com") == null)
        {
            var doctor = new AppIdentityUser
            {
                UserName = "emily.rodriguez@appointment.com",
                Email = "emily.rodriguez@appointment.com",
                FirstName = "Dr. Emily",
                LastName = "Rodriguez",
                PhoneNumber = "+1234567893",
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(doctor, "Doctor123!");
            await userManager.AddToRoleAsync(doctor, "Professional");
        }

        if (await userManager.FindByEmailAsync("david.williams@appointment.com") == null)
        {
            var doctor = new AppIdentityUser
            {
                UserName = "david.williams@appointment.com",
                Email = "david.williams@appointment.com",
                FirstName = "Dr. David",
                LastName = "Williams",
                PhoneNumber = "+1234567894",
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(doctor, "Doctor123!");
            await userManager.AddToRoleAsync(doctor, "Professional");
        }

        if (await userManager.FindByEmailAsync("lisa.patel@appointment.com") == null)
        {
            var doctor = new AppIdentityUser
            {
                UserName = "lisa.patel@appointment.com",
                Email = "lisa.patel@appointment.com",
                FirstName = "Dr. Lisa",
                LastName = "Patel",
                PhoneNumber = "+1234567895",
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(doctor, "Doctor123!");
            await userManager.AddToRoleAsync(doctor, "Professional");
        }

        // Client user
        if (await userManager.FindByEmailAsync("client@appointment.com") == null)
        {
            var client = new AppIdentityUser
            {
                UserName = "client@appointment.com",
                Email = "client@appointment.com",
                FirstName = "Jane",
                LastName = "Doe",
                PhoneNumber = "+0987654321",
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(client, "Client123!");
            await userManager.AddToRoleAsync(client, "Client");
        }
    }

    private static async Task SeedDomainConfigurationsAsync(AppointmentDbContext context)
    {
        if (!await context.DomainConfigurations.AnyAsync())
        {
            var configurations = new[]
            {
                new DomainConfiguration
                {
                    DomainType = DomainType.Medical,
                    Name = "Medical Consultation",
                    Description = "General medical consultation appointments",
                    DefaultDurationMinutes = 30,
                    IsActive = true,
                    RequiredFields = new Dictionary<string, string>
                    {
                        { "Symptoms", "text" },
                        { "MedicalHistory", "text" }
                    }
                },
                new DomainConfiguration
                {
                    DomainType = DomainType.Legal,
                    Name = "Legal Consultation",
                    Description = "Legal advice and consultation appointments",
                    DefaultDurationMinutes = 60,
                    IsActive = true,
                    RequiredFields = new Dictionary<string, string>
                    {
                        { "CaseDescription", "text" },
                        { "LegalCategory", "select" }
                    }
                },
                new DomainConfiguration
                {
                    DomainType = DomainType.Consulting,
                    Name = "Business Consulting",
                    Description = "Business strategy and management consulting",
                    DefaultDurationMinutes = 45,
                    IsActive = true,
                    RequiredFields = new Dictionary<string, string>
                    {
                        { "BusinessType", "text" },
                        { "ConsultationArea", "text" }
                    }
                }
            };

            await context.DomainConfigurations.AddRangeAsync(configurations);
        }
    }

    private static async Task SeedProfessionalsAsync(AppointmentDbContext context, UserManager<AppIdentityUser> userManager)
    {
        var existingCount = await context.Professionals.CountAsync();
        
        if (existingCount == 0)
        {
            var professionals = new List<Professional>();

            // Dr. John Smith - General Practitioner
            var doctorUser = await userManager.FindByEmailAsync("doctor@appointment.com");
            if (doctorUser != null)
            {
                professionals.Add(new Professional
                {
                    UserId = doctorUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Internal Medicine",
                    Specialization = "General Practitioner",
                    HourlyRate = 150,
                    ExperienceYears = 15,
                    Bio = "Experienced medical professional with over 15 years of practice in internal medicine.",
                    IsAvailable = true
                });
            }

            // Dr. Sarah Johnson - Cardiologist
            var sarahUser = await userManager.FindByEmailAsync("sarah.johnson@appointment.com");
            if (sarahUser != null)
            {
                professionals.Add(new Professional
                {
                    UserId = sarahUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Cardiology, FACC",
                    Specialization = "Cardiologist",
                    HourlyRate = 200,
                    ExperienceYears = 12,
                    Bio = "Board-certified cardiologist specializing in preventive cardiology and heart disease management.",
                    IsAvailable = true
                });
            }

            // Dr. Michael Chen - Pediatrician
            var michaelUser = await userManager.FindByEmailAsync("michael.chen@appointment.com");
            if (michaelUser != null)
            {
                professionals.Add(new Professional
                {
                    UserId = michaelUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Pediatrics",
                    Specialization = "Pediatrician",
                    HourlyRate = 175,
                    ExperienceYears = 10,
                    Bio = "Dedicated pediatrician providing comprehensive care for children from infancy through adolescence.",
                    IsAvailable = true
                });
            }

            // Dr. Emily Rodriguez - Dermatologist
            var emilyUser = await userManager.FindByEmailAsync("emily.rodriguez@appointment.com");
            if (emilyUser != null)
            {
                professionals.Add(new Professional
                {
                    UserId = emilyUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Dermatology, FAAD",
                    Specialization = "Dermatologist",
                    HourlyRate = 180,
                    ExperienceYears = 8,
                    Bio = "Expert in medical and cosmetic dermatology, treating various skin conditions with the latest techniques.",
                    IsAvailable = true
                });
            }

            // Dr. David Williams - Orthopedic Surgeon
            var davidUser = await userManager.FindByEmailAsync("david.williams@appointment.com");
            if (davidUser != null)
            {
                professionals.Add(new Professional
                {
                    UserId = davidUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Orthopedic Surgery",
                    Specialization = "Orthopedic Surgeon",
                    HourlyRate = 250,
                    ExperienceYears = 18,
                    Bio = "Specialized in sports medicine and joint replacement surgeries with extensive surgical experience.",
                    IsAvailable = true
                });
            }

            // Dr. Lisa Patel - Psychiatrist
            var lisaUser = await userManager.FindByEmailAsync("lisa.patel@appointment.com");
            if (lisaUser != null)
            {
                professionals.Add(new Professional
                {
                    UserId = lisaUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Psychiatry",
                    Specialization = "Psychiatrist",
                    HourlyRate = 190,
                    ExperienceYears = 14,
                    Bio = "Compassionate psychiatrist focusing on anxiety, depression, and mood disorders with evidence-based treatments.",
                    IsAvailable = true
                });
            }

            if (professionals.Any())
            {
                await context.Professionals.AddRangeAsync(professionals);
            }
        }
        else if (existingCount == 1)
        {
            // Add the additional professionals if only the first one exists
            var professionals = new List<Professional>();

            // Check and add each professional individually
            var sarahUser = await userManager.FindByEmailAsync("sarah.johnson@appointment.com");
            if (sarahUser != null && !await context.Professionals.AnyAsync(p => p.UserId == sarahUser.Id))
            {
                professionals.Add(new Professional
                {
                    UserId = sarahUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Cardiology, FACC",
                    Specialization = "Cardiologist",
                    HourlyRate = 200,
                    ExperienceYears = 12,
                    Bio = "Board-certified cardiologist specializing in preventive cardiology and heart disease management.",
                    IsAvailable = true
                });
            }

            var michaelUser = await userManager.FindByEmailAsync("michael.chen@appointment.com");
            if (michaelUser != null && !await context.Professionals.AnyAsync(p => p.UserId == michaelUser.Id))
            {
                professionals.Add(new Professional
                {
                    UserId = michaelUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Pediatrics",
                    Specialization = "Pediatrician",
                    HourlyRate = 175,
                    ExperienceYears = 10,
                    Bio = "Dedicated pediatrician providing comprehensive care for children from infancy through adolescence.",
                    IsAvailable = true
                });
            }

            var emilyUser = await userManager.FindByEmailAsync("emily.rodriguez@appointment.com");
            if (emilyUser != null && !await context.Professionals.AnyAsync(p => p.UserId == emilyUser.Id))
            {
                professionals.Add(new Professional
                {
                    UserId = emilyUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Dermatology, FAAD",
                    Specialization = "Dermatologist",
                    HourlyRate = 180,
                    ExperienceYears = 8,
                    Bio = "Expert in medical and cosmetic dermatology, treating various skin conditions with the latest techniques.",
                    IsAvailable = true
                });
            }

            var davidUser = await userManager.FindByEmailAsync("david.williams@appointment.com");
            if (davidUser != null && !await context.Professionals.AnyAsync(p => p.UserId == davidUser.Id))
            {
                professionals.Add(new Professional
                {
                    UserId = davidUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Orthopedic Surgery",
                    Specialization = "Orthopedic Surgeon",
                    HourlyRate = 250,
                    ExperienceYears = 18,
                    Bio = "Specialized in sports medicine and joint replacement surgeries with extensive surgical experience.",
                    IsAvailable = true
                });
            }

            var lisaUser = await userManager.FindByEmailAsync("lisa.patel@appointment.com");
            if (lisaUser != null && !await context.Professionals.AnyAsync(p => p.UserId == lisaUser.Id))
            {
                professionals.Add(new Professional
                {
                    UserId = lisaUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Psychiatry",
                    Specialization = "Psychiatrist",
                    HourlyRate = 190,
                    ExperienceYears = 14,
                    Bio = "Compassionate psychiatrist focusing on anxiety, depression, and mood disorders with evidence-based treatments.",
                    IsAvailable = true
                });
            }

            if (professionals.Any())
            {
                await context.Professionals.AddRangeAsync(professionals);
            }
        }
    }

    private static async Task SeedAvailabilitiesAsync(AppointmentDbContext context)
    {
        if (!await context.Availabilities.AnyAsync())
        {
            var professionals = await context.Professionals.ToListAsync();
            var availabilities = new List<Availability>();

            foreach (var professional in professionals)
            {
                // Add Monday to Friday availability for each professional
                availabilities.AddRange(new[]
                {
                    new Availability
                    {
                        ProfessionalId = professional.Id,
                        DayOfWeek = DayOfWeek.Monday,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        ScheduleType = ScheduleType.Weekly,
                        IsActive = true
                    },
                    new Availability
                    {
                        ProfessionalId = professional.Id,
                        DayOfWeek = DayOfWeek.Tuesday,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        ScheduleType = ScheduleType.Weekly,
                        IsActive = true
                    },
                    new Availability
                    {
                        ProfessionalId = professional.Id,
                        DayOfWeek = DayOfWeek.Wednesday,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        ScheduleType = ScheduleType.Weekly,
                        IsActive = true
                    },
                    new Availability
                    {
                        ProfessionalId = professional.Id,
                        DayOfWeek = DayOfWeek.Thursday,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        ScheduleType = ScheduleType.Weekly,
                        IsActive = true
                    },
                    new Availability
                    {
                        ProfessionalId = professional.Id,
                        DayOfWeek = DayOfWeek.Friday,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(13, 0, 0),
                        ScheduleType = ScheduleType.Weekly,
                        IsActive = true
                    }
                });
            }

            if (availabilities.Any())
            {
                await context.Availabilities.AddRangeAsync(availabilities);
            }
        }
    }
}