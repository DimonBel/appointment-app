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
        if (!await context.Professionals.AnyAsync())
        {
            var doctorUser = await userManager.FindByEmailAsync("doctor@appointment.com");
            if (doctorUser != null)
            {
                var professional = new Professional
                {
                    UserId = doctorUser.Id,
                    Title = "Dr.",
                    Qualifications = "MD, Internal Medicine",
                    Specialization = "General Practitioner",
                    HourlyRate = 150,
                    ExperienceYears = 15,
                    Bio = "Experienced medical professional with over 15 years of practice in internal medicine.",
                    IsAvailable = true
                };

                await context.Professionals.AddAsync(professional);
            }
        }
    }

    private static async Task SeedAvailabilitiesAsync(AppointmentDbContext context)
    {
        var professional = await context.Professionals.FirstOrDefaultAsync();
        if (professional != null && !await context.Availabilities.AnyAsync())
        {
            var availabilities = new[]
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
            };

            await context.Availabilities.AddRangeAsync(availabilities);
        }
    }
}