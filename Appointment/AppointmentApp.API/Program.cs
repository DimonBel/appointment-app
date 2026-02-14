using AppointmentApp.API.Endpoints;
using AppointmentApp.API.Hubs;
using AppointmentApp.API.Services;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Postgres.Data;
using AppointmentApp.Postgres.Repositories;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure PostgreSQL Database with JSON support
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseNpgsql(dataSource,
        b => b.MigrationsAssembly("AppointmentApp.Postgres"))
    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

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

// Apply database migrations automatically (changed from Development-only to always run)
await EnsureDatabaseCreatedAndMigratedAsync(app.Services, app.Configuration);
// Seed demo data only in Development
if (app.Environment.IsDevelopment())
{
    await SeedDemoDataAsync(app.Services);
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

// Map SignalR Hub for real-time order notifications
app.MapHub<OrderHub>("/orderhub");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Appointment API", timestamp = DateTime.UtcNow }))
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
    var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
    await dbContext.Database.MigrateAsync();
}

static async Task SeedDemoDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppIdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppIdentityRole>>();

    try
    {
        await SeedData.SeedAsync(context, userManager, roleManager);
        Console.WriteLine("Demo data seeded successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding demo data: {ex.Message}");
    }
}