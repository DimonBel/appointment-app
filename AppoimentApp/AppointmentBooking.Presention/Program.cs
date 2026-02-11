using AppointmentBooking.Domain.Service;
using AppointmentBooking.Domain.Repository;
using AppointmentBooking.Application.ApplicationService;
using AppointmentBooking.Infrastructure.Repository;
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

builder.Services.AddDbContext<AppoinmentBookingDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAppointmentBookingRepository, AppointmentBookingRepository>();
builder.Services.AddScoped<IBookAppoimentService, BookAppoimentService>();
builder.Services.AddScoped<IChangeAppoinmentStatusService, ChangeAppoinmentStatusService>();
builder.Services.AddHttpClient<IViewAvaliableSlotService, ViewAvaliableSlotService>(client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:DoctorAvailability"] ?? "http://localhost:5010";
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
