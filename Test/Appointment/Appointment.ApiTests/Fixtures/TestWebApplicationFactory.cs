using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AppointmentApp.API;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Postgres.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Appointment.ApiTests.Fixtures;

/// <summary>
/// Custom web application factory for API testing with in-memory database
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppointmentDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database
            services.AddDbContext<AppointmentDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
                options.EnableSensitiveDataLogging();
            });

            // Replace UserManager with a test-friendly version
            services.RemoveAll<UserManager<AppIdentityUser>>();
            services.RemoveAll<IPasswordHasher<AppIdentityUser>>();

            services.AddIdentityCore<AppIdentityUser>()
                .AddEntityFrameworkStores<AppointmentDbContext>()
                .AddDefaultTokenProviders();

            // Ensure database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
            context.Database.EnsureCreated();
        });
    }

    public async Task<AppointmentDbContext> GetDbContextAsync()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
    }

    public async Task ResetDatabaseAsync()
    {
        var context = await GetDbContextAsync();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    public async Task<AppIdentityUser> CreateTestUserAsync(string email, string firstName, string lastName)
    {
        var context = await GetDbContextAsync();
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

        var passwordHasher = Services.GetRequiredService<IPasswordHasher<AppIdentityUser>>();
        user.PasswordHash = passwordHasher.HashPassword(user, "Test123!");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<Professional> CreateTestProfessionalAsync(Guid userId)
    {
        var context = await GetDbContextAsync();
        var professional = new Professional
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Professionals.Add(professional);
        await context.SaveChangesAsync();

        return professional;
    }

    public async Task<Availability> CreateTestAvailabilityAsync(Guid professionalId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        var context = await GetDbContextAsync();
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

        context.Availabilities.Add(availability);
        await context.SaveChangesAsync();

        return availability;
    }

    public async Task<DomainConfiguration> CreateTestDomainConfigurationAsync(DomainType domainType, string name)
    {
        var context = await GetDbContextAsync();
        var domainConfig = new DomainConfiguration
        {
            Id = Guid.NewGuid(),
            DomainType = domainType,
            Name = name,
            DefaultDurationMinutes = 60,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.DomainConfigurations.Add(domainConfig);
        await context.SaveChangesAsync();

        return domainConfig;
    }

    public string CreateTestToken(Guid userId, string email, params string[] roles)
    {
        // Create a mock JWT token for testing
        // In a real scenario, this would use the actual JWT token generation
        var tokenPayload = new
        {
            sub = userId.ToString(),
            email = email,
            name = $"{email.Split('@')[0]}",
            roles = roles,
            exp = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()
        };

        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tokenPayload)));
        return $"Bearer {token}";
    }

    public HttpClient CreateAuthenticatedClient(Guid userId, string email, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateTestToken(userId, email, roles));
        return client;
    }

    public StringContent CreateJsonContent<T>(T data)
    {
        return new StringContent(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json");
    }
}

public static class HttpClientExtensions
{
    public static async Task<T?> ReadFromJsonAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}