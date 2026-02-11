using ChatApp.Domain.Interfaces;
using ChatApp.Domain.Entity;
using ChatApp.Service.Services;
using ChatApp.API.Hubs;
using ChatApp.API.Endpoints;
using ChatApp.Repository.Interfaces;
using ChatApp.Postgres.Data;
using ChatApp.Postgres.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure Authentication with Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.LoginPath = "/api/auth/login";
        options.AccessDeniedPath = "/api/auth/accessdenied";
        options.SlidingExpiration = true;
        // Handle unauthorized API requests with JSON instead of redirect
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
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

// Register Repository Layer
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
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

if (app.Environment.IsDevelopment())
{
    await EnsureDatabaseCreatedAndMigratedAsync(app.Services, app.Configuration);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Map minimal API endpoints
app.MapAuthEndpoints();
app.MapChatEndpoints();

// Map SignalR Hub for real-time WebSocket communication
app.MapHub<ChatHub>("/chathub");

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