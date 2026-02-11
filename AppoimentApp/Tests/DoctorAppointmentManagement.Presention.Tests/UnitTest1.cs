using System.Net;
using DoctorAppointmentManagement.Domain.ServicePort;
using DoctorAppointmentManagement.Domain.ServiceAdaptor;
using DoctorAppointmentManagement.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DoctorAppointmentManagement.Presention.Tests;

public sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}

public sealed class DoctorAppointmentManagementApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ManagementDBContext>));
            services.RemoveAll<ManagementDBContext>();

            services.RemoveAll<IDoctorAppoinmentManagementService>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            services.AddDbContext<ManagementDBContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<RecordingHttpMessageHandler>();
            services.AddSingleton<RecordingHttpMessageHandler>();

            services.AddHttpClient<IDoctorAppoinmentManagementService, DoctorAppoinmentManagementService>(client =>
            {
                client.BaseAddress = new Uri("http://example");
            }).ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<RecordingHttpMessageHandler>());

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ManagementDBContext>();
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

public class DoctorAppointmentManagementEndpointsTests(DoctorAppointmentManagementApiFactory factory) : IClassFixture<DoctorAppointmentManagementApiFactory>
{
    private readonly DoctorAppointmentManagementApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Put_Cancel_CallsAppointmentBookingChangeStatusWithCancel()
    {
        var slotId = Guid.NewGuid();
        var resp = await _client.PutAsync($"/api/DoctorAppoinmentManagement/cancel?SlotId={slotId}", content: null);
        Assert.True(resp.IsSuccessStatusCode);

        var handler = _factory.Services.GetRequiredService<RecordingHttpMessageHandler>();
        Assert.NotNull(handler.LastRequest);
        Assert.Contains("/api/ChangeAppoinmentStatus", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains($"SlotId={slotId}", handler.LastRequest.RequestUri.ToString());
        Assert.Contains("StatusId=2", handler.LastRequest.RequestUri.ToString());
    }

    [Fact]
    public async Task Put_Complete_CallsAppointmentBookingChangeStatusWithComplete()
    {
        var slotId = Guid.NewGuid();
        var resp = await _client.PutAsync($"/api/DoctorAppoinmentManagement/complete?SlotId={slotId}", content: null);
        Assert.True(resp.IsSuccessStatusCode);

        var handler = _factory.Services.GetRequiredService<RecordingHttpMessageHandler>();
        Assert.NotNull(handler.LastRequest);
        Assert.Contains("/api/ChangeAppoinmentStatus", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains($"SlotId={slotId}", handler.LastRequest.RequestUri.ToString());
        Assert.Contains("StatusId=1", handler.LastRequest.RequestUri.ToString());
    }
}
