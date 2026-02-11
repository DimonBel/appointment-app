using DoctorAppointmentManagement.Domain.RepositoryPort;
using DoctorAppointmentManagement.Domain.ServiceAdaptor;
using DoctorAppointmentManagement.Domain.ServicePort;
using DoctorAppointmentManagement.Infrastructure;
using DoctorAppointmentManagement.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddDbContext<ManagementDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFN_GetUpcomingAppoinmentRepository, FN_GetUpcomingAppoinmentRepository>();
builder.Services.AddHttpClient<IDoctorAppoinmentManagementService, DoctorAppoinmentManagementService>(client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:AppointmentBooking"] ?? "http://localhost:5011";
    client.BaseAddress = new Uri(baseUrl);
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("DevCors");

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
