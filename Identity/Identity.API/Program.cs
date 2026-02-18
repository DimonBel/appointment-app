using Identity.API.Endpoints;
using Identity.Domain.Entity;
using Identity.Domain.Interfaces;
using Identity.Service.Services;
using Identity.Postgres.Data;
using Identity.Postgres.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure PostgreSQL Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityConnection"),
        b => b.MigrationsAssembly("Identity.Postgres"))
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
.AddEntityFrameworkStores<IdentityDbContext>()
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

// Add HttpContextAccessor for authentication
builder.Services.AddHttpContextAccessor();

// Register Service Layer
builder.Services.AddScoped<IAuthService, AuthService>();

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

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Map minimal API endpoints
app.MapAuthEndpoints();

app.Run();

static async Task EnsureDatabaseCreatedAndMigratedAsync(IServiceProvider services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("IdentityConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'IdentityConnection' is missing.");
    }

    var csb = new NpgsqlConnectionStringBuilder(connectionString);
    var databaseName = csb.Database;
    if (string.IsNullOrWhiteSpace(databaseName))
    {
        throw new InvalidOperationException("Database name is missing in 'IdentityConnection'.");
    }

    if (!Regex.IsMatch(databaseName, "^[A-Za-z0-9_]+$"))
    {
        throw new InvalidOperationException($"Unsafe database name '{databaseName}'. Only letters, digits, and '_' are allowed.");
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
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.Database.MigrateAsync();
}
