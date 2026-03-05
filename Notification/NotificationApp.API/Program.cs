using NotificationApp.API.Endpoints;
using NotificationApp.API.Hubs;
using NotificationApp.API.Configuration;
using NotificationApp.Domain.Interfaces;
using NotificationApp.Postgres.Data;
using NotificationApp.Postgres.Repositories;
using NotificationApp.Repository.Interfaces;
using NotificationApp.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

// Configure PostgreSQL Database
builder.Services.ConfigureDatabase(builder.Configuration);

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Configure JWT for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationhub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Configure SignalR
builder.Services.AddSignalR();

// Register Repository Layer
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
builder.Services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
builder.Services.AddScoped<INotificationScheduleRepository, NotificationScheduleRepository>();
builder.Services.AddScoped<INotificationEventRepository, NotificationEventRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Service Layer
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
builder.Services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
builder.Services.AddScoped<INotificationScheduleService, NotificationScheduleService>();
builder.Services.AddScoped<INotificationEventService, NotificationEventService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRealTimeNotifier, NotificationApp.API.Services.SignalRNotifier>();

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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseResponseCompression();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Middleware to allow internal service communication
app.Use(async (context, next) =>
{
    var internalKey = context.Request.Headers["X-Internal-Key"].FirstOrDefault();
    var configuredKey = builder.Configuration["InternalServiceKey"] ?? "internal-dev-key";

    if (internalKey == configuredKey)
    {
        // For internal service requests, create a fake authenticated user
        // This allows the endpoint to proceed without JWT validation
        context.Items["IsInternalService"] = true;
    }
    await next();
});

// Map minimal API endpoints
app.MapNotificationEndpoints();
app.MapPreferenceEndpoints();
app.MapTemplateEndpoints();
app.MapScheduleEndpoints();
app.MapEventEndpoints();

// Map SignalR Hub for real-time notification delivery
app.MapHub<NotificationHub>("/notificationhub");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Notification API", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.Run();
