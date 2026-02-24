using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

namespace Appointment.E2ETests.Fixtures;

/// <summary>
/// E2E test fixture with complete system setup
/// Simulates real user journeys from registration to booking completion
/// </summary>
public class E2ETestFixture : WebApplicationFactory<Program>
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
                options.UseInMemoryDatabase("E2ETestDb");
                options.EnableSensitiveDataLogging();
            });

            // Replace UserManager
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

    #region User Management

    public async Task<TestUser> RegisterAndLoginUserAsync(string email, string firstName, string lastName, string password = "Test123!")
    {
        var client = CreateClient();

        // Register user
        var registerRequest = new
        {
            email,
            password,
            userName = email.Split('@')[0],
            firstName,
            lastName
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var userResponse = await registerResponse.Content.ReadFromJsonAsync<TestUserResponse>();
        var token = _factory.CreateTestToken(userResponse!.Id, userResponse.Email, "Client");

        return new TestUser
        {
            Id = userResponse.Id,
            Email = userResponse.Email,
            FirstName = firstName,
            LastName = lastName,
            Token = token
        };
    }

    public async Task<TestUser> CreateClientAsync(string email = "client@test.com")
    {
        return await RegisterAndLoginUserAsync(email, "John", "Doe");
    }

    public async Task<TestUser> CreateProfessionalAsync(string email = "doctor@test.com")
    {
        var user = await RegisterAndLoginUserAsync(email, "Dr. Jane", "Smith");

        // Create professional profile
        var client = CreateAuthenticatedClient(user.Token);
        var professionalRequest = new
        {
            userId = user.Id,
            title = "Dr.",
            qualifications = "MD",
            specialization = "General Medicine"
        };

        var response = await client.PostAsJsonAsync("/api/professionals", professionalRequest);
        response.EnsureSuccessStatusCode();

        var professional = await response.Content.ReadFromJsonAsync<Professional>();
        user.ProfessionalId = professional!.Id;

        return user;
    }

    public async Task<TestUser> CreateAdminAsync(string email = "admin@test.com")
    {
        return await RegisterAndLoginUserAsync(email, "Admin", "User");
    }

    #endregion

    #region Professional Setup

    public async Task<Professional> SetupProfessionalAvailabilityAsync(Guid professionalId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        var user = new TestUser { Token = _factory.CreateTestToken(Guid.NewGuid(), "test@test.com", "Admin") };
        var client = CreateAuthenticatedClient(user.Token);

        var availabilityRequest = new
        {
            professionalId,
            dayOfWeek = (int)dayOfWeek,
            startTime = startTime.ToString(@"hh\:mm\:ss"),
            endTime = endTime.ToString(@"hh\:mm\:ss"),
            scheduleType = (int)ScheduleType.Recurring
        };

        var response = await client.PostAsJsonAsync("/api/availabilities", availabilityRequest);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Professional>();
    }

    public async Task<List<AvailabilitySlot>> GenerateSlotsAsync(Guid professionalId, DateTime date)
    {
        var user = new TestUser { Token = _factory.CreateTestToken(Guid.NewGuid(), "test@test.com", "Client") };
        var client = CreateAuthenticatedClient(user.Token);

        var response = await client.GetAsync($"/api/availabilities/slots?professionalId={professionalId}&date={date:yyyy-MM-dd}");
        response.EnsureSuccessStatusCode();

        var slots = await response.Content.ReadFromJsonAsync<List<AvailabilitySlot>>();
        return slots ?? new List<AvailabilitySlot>();
    }

    #endregion

    #region Domain Configuration

    public async Task<DomainConfiguration> CreateDomainConfigurationAsync(DomainType domainType, string name)
    {
        var user = new TestUser { Token = _factory.CreateTestToken(Guid.NewGuid(), "admin@test.com", "Admin") };
        var client = CreateAuthenticatedClient(user.Token);

        var configRequest = new
        {
            domainType = (int)domainType,
            name,
            defaultDurationMinutes = 60
        };

        var response = await client.PostAsJsonAsync("/api/domain-configurations", configRequest);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DomainConfiguration>();
    }

    #endregion

    #region Booking Operations

    public async Task<Order> BookAppointmentAsync(string token, Guid professionalId, DateTime scheduledDateTime, int durationMinutes, string? title = null)
    {
        var client = CreateAuthenticatedClient(token);

        var bookingRequest = new
        {
            professionalId,
            scheduledDateTime = scheduledDateTime.ToString("O"),
            durationMinutes,
            title = title ?? "General Consultation"
        };

        var response = await client.PostAsJsonAsync("/api/orders", bookingRequest);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Order>();
    }

    public async Task<Order> ApproveAppointmentAsync(string token, Guid orderId, string reason = "Approved")
    {
        var client = CreateAuthenticatedClient(token);

        var approveRequest = new
        {
            reason
        };

        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/approve", approveRequest);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Order>();
    }

    public async Task<Order> CompleteAppointmentAsync(string token, Guid orderId, string notes = "Completed")
    {
        var client = CreateAuthenticatedClient(token);

        var completeRequest = new
        {
            notes
        };

        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/complete", completeRequest);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Order>();
    }

    public async Task<Order> CancelAppointmentAsync(string token, Guid orderId, string reason = "Cancelled")
    {
        var client = CreateAuthenticatedClient(token);

        var cancelRequest = new
        {
            reason
        };

        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", cancelRequest);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Order>();
    }

    #endregion

    #region Pre-Order Data

    public async Task<PreOrderData> SubmitPreOrderDataAsync(string token, Guid orderId, Dictionary<string, string> dataFields)
    {
        var client = CreateAuthenticatedClient(token);

        var dataRequest = new
        {
            orderId,
            clientId = Guid.NewGuid(), // Will be replaced with actual client ID
            dataFields
        };

        var response = await client.PostAsJsonAsync("/api/preorder-data", dataRequest);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PreOrderData>();
    }

    public async Task<PreOrderData> CompletePreOrderDataAsync(string token, Guid preOrderDataId)
    {
        var client = CreateAuthenticatedClient(token);

        var response = await client.PostAsync($"/api/preorder-data/{preOrderDataId}/complete", null);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PreOrderData>();
    }

    #endregion

    #region Verification

    public async Task VerifyOrderStatusAsync(Guid orderId, OrderStatus expectedStatus)
    {
        var context = await GetDbContextAsync();
        var order = await context.Orders.FindAsync(orderId);
        order.Should().NotBeNull();
        order!.Status.Should().Be(expectedStatus);
    }

    public async Task VerifyOrderHistoryAsync(Guid orderId, int expectedHistoryCount)
    {
        var context = await GetDbContextAsync();
        var history = await context.OrderHistory.Where(h => h.OrderId == orderId).ToListAsync();
        history.Should().HaveCount(expectedHistoryCount);
    }

    public async Task VerifySlotBookedAsync(Guid professionalId, DateTime dateTime, int durationMinutes)
    {
        var context = await GetDbContextAsync();
        var slots = await context.AvailabilitySlots
            .Where(s => s.SlotDate == dateTime.Date)
            .Where(s => s.StartTime >= dateTime.TimeOfDay)
            .Where(s => s.StartTime < dateTime.TimeOfDay.Add(TimeSpan.FromMinutes(durationMinutes)))
            .ToListAsync();

        slots.Should().NotBeEmpty();
        slots.All(s => !s.IsAvailable).Should().BeTrue();
    }

    public async Task<List<Order>> GetUserOrdersAsync(string token)
    {
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/orders");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<Order>>() ?? new List<Order>();
    }

    #endregion

    #region HTTP Client Helpers

    public HttpClient CreateAuthenticatedClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public string CreateTestToken(Guid userId, string email, params string[] roles)
    {
        var tokenPayload = new
        {
            sub = userId.ToString(),
            email = email,
            name = email.Split('@')[0],
            roles = roles,
            exp = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()
        };

        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tokenPayload)));
        return token;
    }

    public StringContent CreateJsonContent<T>(T data)
    {
        return new StringContent(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json");
    }

    #endregion
}

public record TestUser
{
    public Guid Id { get; init; }
    public string Email { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string Token { get; init; }
    public Guid? ProfessionalId { get; set; }
}

public record TestUserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
}