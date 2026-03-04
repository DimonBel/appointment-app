using AppointmentApp.API.Endpoints;
using AppointmentApp.API.Hubs;
using AppointmentApp.API.Services;
using AppointmentApp.API.Configuration;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Postgres.Data;
using AppointmentApp.Postgres.Repositories;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure PostgreSQL Database with JSON support
builder.Services.ConfigureDatabase(builder.Configuration);

// Configure Identity (keep for existing database structure, but auth will use Identity Service)
builder.Services.AddIdentity<AppIdentityUser, AppIdentityRole>(options =>
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

// Add HttpClient for Identity Service
builder.Services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["IdentityService:BaseUrl"] ?? "http://localhost:5005");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add HttpClient for Notification Service
builder.Services.AddHttpClient("NotificationService", client =>
{
    var baseUrl = builder.Configuration["NotificationService:BaseUrl"] ?? "http://localhost:5003";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient("DocumentService", client =>
{
    var baseUrl = builder.Configuration["DocumentService:BaseUrl"] ?? "http://localhost:5004";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Configure Authentication with JWT (validate tokens from Identity Service)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLongForIdentityService!")),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add Memory Cache for performance optimization
builder.Services.AddMemoryCache();

// Configure SignalR
builder.Services.AddSignalR();

// Register Service Layer
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderApprovalService, OrderApprovalService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IProfessionalService, ProfessionalService>();
builder.Services.AddScoped<IDomainConfigurationService, DomainConfigurationService>();
builder.Services.AddScoped<IPreOrderDataService, PreOrderDataService>();

// Register Repository Layer
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
builder.Services.AddScoped<IProfessionalRepository, ProfessionalRepository>();
builder.Services.AddScoped<IDomainConfigurationRepository, DomainConfigurationRepository>();
builder.Services.AddScoped<IPreOrderDataRepository, PreOrderDataRepository>();
builder.Services.AddScoped<IOrderHistoryRepository, OrderHistoryRepository>();
builder.Services.AddScoped<IAvailabilitySlotRepository, AvailabilitySlotRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Apply database migrations automatically
await DatabaseConfiguration.EnsureDatabaseCreatedAndMigratedAsync(app.Services, app.Configuration);

// Seed demo data only in Development
if (app.Environment.IsDevelopment())
{
    await DataSeeder.SeedDemoDataAsync(app.Services);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Map minimal API endpoints
app.MapAuthEndpoints();
app.MapOrderEndpoints();
app.MapProfessionalEndpoints();
app.MapAvailabilityEndpoints();
app.MapDomainConfigurationEndpoints();
app.MapPreOrderDataEndpoints();

// Map SignalR Hub for real-time order notifications
app.MapHub<OrderHub>("/orderhub");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Appointment API", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.Run();