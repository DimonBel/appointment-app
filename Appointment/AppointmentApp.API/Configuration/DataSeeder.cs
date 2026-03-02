using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppointmentApp.Postgres.Data;
using AppointmentApp.Domain.Entity;

namespace AppointmentApp.API.Configuration;

/// <summary>
/// Database data seeder
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Seed demo data into the database
    /// </summary>
    public static async Task SeedDemoDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
        
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppIdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppIdentityRole>>();

            await SeedData.SeedAsync(context, userManager, roleManager);
            
            logger.LogInformation("Demo data seeded successfully.");
            Console.WriteLine("Demo data seeded successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding demo data: {Message}", ex.Message);
            Console.WriteLine($"Error seeding demo data: {ex.Message}");
            throw;
        }
    }
}