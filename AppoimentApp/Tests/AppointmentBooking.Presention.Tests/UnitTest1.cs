using System.Net.Http.Json;
using AppointmentBooking.Domain.Entity;
using AppointmentBooking.Domain.Models;
using AppointmentBooking.Domain.Repository;
using AppointmentBooking.Domain.Service;
using AppointmentBooking.Infrastructure.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppointmentBooking.Presention.Tests;

public sealed class AppointmentBookingApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppoinmentBookingDBContext>));
            services.RemoveAll<AppoinmentBookingDBContext>();

            services.RemoveAll<IBookAppoimentService>();
            services.RemoveAll<IViewAvaliableSlotService>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<AppoinmentBookingDBContext>(options => options.UseSqlite(_connection));

            services.AddScoped<IBookAppoimentService, TestBookAppoimentService>();
            services.AddScoped<IViewAvaliableSlotService, TestViewAvaliableSlotService>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppoinmentBookingDBContext>();
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

internal sealed class TestBookAppoimentService(IAppointmentBookingRepository repository) : IBookAppoimentService
{
    public void BookAppoiment(AppoimentBookingModel appoimentBookingModel)
    {
        var appoiment = new AppoimentBooking
        {
            PatientId = appoimentBookingModel.PatientId,
            PatientName = appoimentBookingModel.PatientName,
            ReservedAt = DateTime.UtcNow,
            SlotId = appoimentBookingModel.SlotId,
            AppoinmentStatus = null
        };

        repository.AddAppoimentBooking(appoiment);
        repository.SaveChanges();
    }
}

internal sealed class TestViewAvaliableSlotService : IViewAvaliableSlotService
{
    public List<SlotModel> ViewAvaliableSlot() =>
    [
        new SlotModel
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            DoctorId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DoctorName = "Stub Doctor",
            Date = DateTime.UtcNow,
            Cost = 50
        }
    ];
}

public class AppointmentBookingEndpointsTests(AppointmentBookingApiFactory factory) : IClassFixture<AppointmentBookingApiFactory>
{
    private readonly AppointmentBookingApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_ViewAvaliableSlot_ReturnsStubbedSlots()
    {
        var slots = await _client.GetFromJsonAsync<List<SlotModel>>("/api/ViewAvaliableSlot");
        Assert.NotNull(slots);
        Assert.Single(slots);
        Assert.Equal("Stub Doctor", slots[0].DoctorName);
    }

    [Fact]
    public async Task Post_AppoimentBooking_PersistsBooking()
    {
        var slotId = Guid.NewGuid();
        var payload = new AppoimentBookingModel
        {
            SlotId = slotId,
            PatientId = Guid.NewGuid(),
            PatientName = "Test Patient",
            DoctorName = "Ignored by service",
            ReservedAt = DateTime.UtcNow
        };

        var post = await _client.PostAsJsonAsync("/api/AppoimentBooking", payload);
        Assert.True(post.IsSuccessStatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppoinmentBookingDBContext>();
        var saved = await db.AppoimentBookings.SingleAsync(c => c.SlotId == slotId);
        Assert.Equal(slotId, saved.SlotId);
        Assert.Equal(payload.PatientName, saved.PatientName);
    }

    [Fact]
    public async Task Put_ChangeAppoinmentStatus_UpdatesStatus()
    {
        var slotId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppoinmentBookingDBContext>();
            db.AppoimentBookings.Add(new AppoimentBooking
            {
                SlotId = slotId,
                PatientId = Guid.NewGuid(),
                PatientName = "Seed",
                ReservedAt = DateTime.UtcNow,
                AppoinmentStatus = null
            });
            await db.SaveChangesAsync();
        }

        var put = await _client.PutAsync($"/api/ChangeAppoinmentStatus?SlotId={slotId}&StatusId=2", content: null);
        Assert.True(put.IsSuccessStatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppoinmentBookingDBContext>();
        var updated = await db2.AppoimentBookings.SingleAsync(c => c.SlotId == slotId);
        Assert.Equal(2, updated.AppoinmentStatus);
    }
}
