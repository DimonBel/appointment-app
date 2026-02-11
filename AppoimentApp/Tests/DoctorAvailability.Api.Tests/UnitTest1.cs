using System.Net.Http.Json;
using DoctorAvailability.Business;
using DoctorAvailability.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DoctorAvailability.Api.Tests;

public sealed class DoctorAvailabilityApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<DoctorAvaliablityDBContext>));
            services.RemoveAll<DoctorAvaliablityDBContext>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<DoctorAvaliablityDBContext>(options => options.UseSqlite(_connection));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DoctorAvaliablityDBContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}

public class DoctorSlotEndpointsTests(DoctorAvailabilityApiFactory factory) : IClassFixture<DoctorAvailabilityApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAllSlots_InitiallyEmpty_ReturnsEmptyArray()
    {
        var slots = await _client.GetFromJsonAsync<List<SlotModel>>("/api/DoctorSlot/all");
        Assert.NotNull(slots);
        Assert.Empty(slots);
    }

    [Fact]
    public async Task PostSlot_ThenGetAvailable_ReturnsSlot()
    {
        var slot = new SlotModel
        {
            Id = Guid.Empty,
            DoctorId = Guid.NewGuid(),
            DoctorName = "Test Doctor",
            Date = DateTime.UtcNow,
            Cost = 123
        };

        var post = await _client.PostAsJsonAsync("/api/DoctorSlot", slot);
        Assert.True(post.IsSuccessStatusCode);

        var available = await _client.GetFromJsonAsync<List<SlotModel>>("/api/DoctorSlot/available");
        Assert.NotNull(available);
        Assert.Single(available);
        Assert.NotEqual(Guid.Empty, available[0].Id);
        Assert.Equal(slot.DoctorId, available[0].DoctorId);
        Assert.Equal(slot.DoctorName, available[0].DoctorName);
    }
}
