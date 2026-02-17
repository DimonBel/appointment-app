using ChatApp.Domain.Interfaces;
using ChatApp.Domain.Entity;
using ChatApp.Service.Services;
using ChatApp.API.Hubs;
using ChatApp.API.Endpoints;
using ChatApp.API.Services;
using ChatApp.Repository.Interfaces;
using ChatApp.Postgres.Data;
using ChatApp.Postgres.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure PostgreSQL Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("ChatApp.Postgres"))
    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Configure Identity
builder.Services.AddIdentity<AppIdentityUser, AppIdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Register HttpClient for Identity Service
builder.Services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>(client =>
{
    var identityServiceUrl = builder.Configuration["IdentityService:BaseUrl"]
        ?? throw new InvalidOperationException("IdentityService:BaseUrl is not configured.");
    client.BaseAddress = new Uri(identityServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register HttpClient for Notification Service
builder.Services.AddHttpClient("NotificationService", client =>
{
    var notificationServiceUrl = builder.Configuration["NotificationService:BaseUrl"] ?? "http://localhost:5003";
    client.BaseAddress = new Uri(notificationServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
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

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Add Memory Cache for performance optimization
builder.Services.AddMemoryCache();

// Configure SignalR
builder.Services.AddSignalR();

// Register Service Layer
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();

// Register Repository Layer
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
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

// Apply database migrations automatically (changed from Development-only to always run)
await EnsureDatabaseCreatedAndMigratedAsync(app.Services, app.Configuration);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// if (app.Environment.IsDevelopment())
// {
//     app.UseHttpsRedirection(); // Disabled for Docker - using HTTP only
// }

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Map minimal API endpoints
app.MapAuthEndpoints();
app.MapChatEndpoints();
app.MapFriendshipEndpoints();

// Map SignalR Hub for real-time WebSocket communication
app.MapHub<ChatHub>("/chathub");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Chat API", timestamp = DateTime.UtcNow }))
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
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}