using AutomationApp.API.Endpoints;
using AutomationApp.API.Hubs;
using AutomationApp.API.Services;
using AutomationApp.Domain.Interfaces;
using AutomationApp.Postgres.Data;
using AutomationApp.Postgres.Repositories;
using AutomationApp.Repository.Interfaces;
using AutomationApp.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure PostgreSQL Database with dynamic JSON support
NpgsqlDataSourceBuilder dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AutomationDbContext>(options =>
    options.UseNpgsql(dataSource,
        b => b.MigrationsAssembly("AutomationApp.Postgres"))
    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Configure Identity
builder.Services.AddIdentity<AutomationApp.Domain.Entity.AppIdentityUser, AutomationApp.Domain.Entity.AppIdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AutomationDbContext>()
.AddDefaultTokenProviders();

// Register HttpClient for Identity Service
builder.Services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>(client =>
{
    var identityServiceUrl = builder.Configuration["IdentityService:BaseUrl"]
        ?? throw new InvalidOperationException("IdentityService:BaseUrl is not configured.");
    client.BaseAddress = new Uri(identityServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register HttpClient for Appointment Service
builder.Services.AddHttpClient("AppointmentService", client =>
{
    var appointmentServiceUrl = builder.Configuration["AppointmentService:BaseUrl"] ?? "http://appointment-service:5001";
    client.BaseAddress = new Uri(appointmentServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

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

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/automationhub"))
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

// Register Service Layer
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<ILLMService, OllamaLLMService>();
builder.Services.AddScoped<IBookingAutomationService, BookingAutomationService>();
builder.Services.AddScoped<IDataCollectionService, DataCollectionService>();

// Register Repository Layer
builder.Services.AddScoped<IConversationRepository, AutomationApp.Postgres.Repositories.ConversationRepository>();
builder.Services.AddScoped<IConversationMessageRepository, AutomationApp.Postgres.Repositories.ConversationMessageRepository>();
builder.Services.AddScoped<IBookingDraftRepository, AutomationApp.Postgres.Repositories.BookingDraftRepository>();
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
app.MapAutomationEndpoints();

// Map SignalR Hub for real-time AI conversation
app.MapHub<AutomationHub>("/automationhub");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Automation API", timestamp = DateTime.UtcNow }))
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
    var dbContext = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
    await dbContext.Database.MigrateAsync();
}