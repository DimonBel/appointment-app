using System;
using System.Linq;
using System.Threading.Tasks;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Postgres.Data;
using AppointmentApp.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Appointment.IntegrationTests.Fixtures;

/// <summary>
/// Fixture for setting up and tearing down the test database with all required entities
/// </summary>
public class TestDatabaseFixture : IDisposable
{
    public ServiceProvider ServiceProvider { get; private set; }
    public AppointmentDbContext DbContext { get; private set; }
    public UserManager<AppIdentityUser> UserManager { get; private set; }

    public TestDatabaseFixture()
    {
        var services = new ServiceCollection();

        // Configure in-memory database
        services.AddDbContext<AppointmentDbContext>(options =>
        {
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // Configure Identity
        services.AddIdentity<AppIdentityUser, AppIdentityRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<AppointmentDbContext>()
        .AddDefaultTokenProviders();

        // Register repositories
        services.AddScoped<IOrderRepository, AppointmentApp.Postgres.Repositories.OrderRepository>();
        services.AddScoped<IProfessionalRepository, AppointmentApp.Postgres.Repositories.ProfessionalRepository>();
        services.AddScoped<IAvailabilityRepository, AppointmentApp.Postgres.Repositories.AvailabilityRepository>();
        services.AddScoped<IAvailabilitySlotRepository, AppointmentApp.Postgres.Repositories.AvailabilitySlotRepository>();
        services.AddScoped<IDomainConfigurationRepository, AppointmentApp.Postgres.Repositories.DomainConfigurationRepository>();
        services.AddScoped<IPreOrderDataRepository, AppointmentApp.Postgres.Repositories.PreOrderDataRepository>();
        services.AddScoped<IOrderHistoryRepository, AppointmentApp.Postgres.Repositories.OrderHistoryRepository>();
        services.AddScoped<IUnitOfWork, AppointmentApp.Postgres.Repositories.UnitOfWork>();

        ServiceProvider = services.BuildServiceProvider();

        DbContext = ServiceProvider.GetRequiredService<AppointmentDbContext>();
        UserManager = ServiceProvider.GetRequiredService<UserManager<AppIdentityUser>>();

        // Create database and run migrations
        DbContext.Database.EnsureCreated();
    }

    public async Task ResetDatabaseAsync()
    {
        // Remove all data
        DbContext.OrderHistory.RemoveRange(DbContext.OrderHistory);
        DbContext.Orders.RemoveRange(DbContext.Orders);
        DbContext.AvailabilitySlots.RemoveRange(DbContext.AvailabilitySlots);
        DbContext.Availabilities.RemoveRange(DbContext.Availabilities);
        DbContext.PreOrderData.RemoveRange(DbContext.PreOrderData);
        DbContext.DomainConfigurations.RemoveRange(DbContext.DomainConfigurations);
        DbContext.Professionals.RemoveRange(DbContext.Professionals);
        DbContext.Users.RemoveRange(DbContext.Users);
        await DbContext.SaveChangesAsync();
    }

    public async Task<AppIdentityUser> CreateTestUserAsync(string email, string firstName, string lastName)
    {
        var user = new AppIdentityUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        await UserManager.CreateAsync(user, "TestPassword123!");
        return user;
    }

    public async Task<Professional> CreateTestProfessionalAsync(AppIdentityUser user, string? title = null, string? specialization = null)
    {
        var professional = new Professional
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = title,
            Specialization = specialization,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Professionals.Add(professional);
        await DbContext.SaveChangesAsync();

        return professional;
    }

    public async Task<Availability> CreateTestAvailabilityAsync(Guid professionalId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        var availability = new Availability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professionalId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            ScheduleType = ScheduleType.Recurring,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Availabilities.Add(availability);
        await DbContext.SaveChangesAsync();

        return availability;
    }

    public async Task<DomainConfiguration> CreateTestDomainConfigurationAsync(DomainType domainType, string name)
    {
        var configuration = new DomainConfiguration
        {
            Id = Guid.NewGuid(),
            DomainType = domainType,
            Name = name,
            DefaultDurationMinutes = 60,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.DomainConfigurations.Add(configuration);
        await DbContext.SaveChangesAsync();

        return configuration;
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        ServiceProvider?.Dispose();
    }
}

/// <summary>
/// Collection fixture to ensure database is created once and shared across tests
/// </summary>
public class TestDatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
    // This class is used for xUnit collection fixture
}