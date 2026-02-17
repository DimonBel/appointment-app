using NotificationApp.API.Endpoints;
using NotificationApp.API.Hubs;
using NotificationApp.Domain.Interfaces;
using NotificationApp.Postgres.Data;
using NotificationApp.Postgres.Repositories;
using NotificationApp.Repository.Interfaces;
using NotificationApp.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure PostgreSQL Database
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("NotificationApp.Postgres"))
    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

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
builder.Services.AddScoped<INotificationService, NotificationApp.Service.Services.NotificationService>();
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
await EnsureDatabaseCreatedAndMigratedAsync(app.Services, app.Configuration);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

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

static async Task EnsureDatabaseCreatedAndMigratedAsync(IServiceProvider services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
    }

    var csb = new NpgsqlConnectionStringBuilder(connectionString);
    var databaseName = csb.Database;
    if (string.IsNullOrWhiteSpace(databaseName))
    {
        throw new InvalidOperationException("Database name is missing in 'DefaultConnection'.");
    }

    if (!Regex.IsMatch(databaseName, "^[A-Za-z0-9_]+$"))
    {
        throw new InvalidOperationException(
            $"Unsafe database name '{databaseName}'. Only letters, digits, and '_' are allowed.");
    }

    var adminCsb = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Database = "postgres"
    };

    await using (var adminConnection = new NpgsqlConnection(adminCsb.ConnectionString))
    {
        await adminConnection.OpenAsync();

        await using (var existsCmd = new NpgsqlCommand(
                         "SELECT 1 FROM pg_database WHERE datname = @name;",
                         adminConnection))
        {
            existsCmd.Parameters.AddWithValue("name", databaseName);
            var exists = await existsCmd.ExecuteScalarAsync() != null;
            if (!exists)
            {
                await using var createCmd =
                    new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\";", adminConnection);
                await createCmd.ExecuteNonQueryAsync();
            }
        }
    }

    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await dbContext.Database.MigrateAsync();

    await EnsureSchemaTablesExistAsync(dbContext);
}

static async Task EnsureSchemaTablesExistAsync(NotificationDbContext dbContext)
{
    await using var connection = dbContext.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='Notifications';";
    var tableCount = Convert.ToInt32(await command.ExecuteScalarAsync());

    if (tableCount > 0)
    {
        return;
    }

    var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
    await databaseCreator.CreateTablesAsync();
}
