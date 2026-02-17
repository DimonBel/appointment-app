using IdentityApp.API.Endpoints;
using IdentityApp.Domain.Entity;
using IdentityApp.Domain.Interfaces;
using IdentityApp.Postgres.Data;
using IdentityApp.Postgres.Repositories;
using IdentityApp.Repository.Interfaces;
using IdentityApp.Service.Services;
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

// Configure PostgreSQL Database
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(dataSource,
        b => b.MigrationsAssembly("IdentityApp.Postgres"))
    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Configure Identity
builder.Services.AddIdentity<AppIdentityUser, AppIdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Disable email confirmation requirement
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<IdentityDbContext>()
.AddDefaultTokenProviders();

// Configure Authentication with JWT
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!")),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireRole("Admin");
    });
});

// Add Memory Cache for performance optimization
builder.Services.AddMemoryCache();

// Register Service Layer
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDoctorProfileService, DoctorProfileService>();
builder.Services.AddScoped<IIdentityEmailService, IdentityEmailService>();

// Register Repository Layer
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IDoctorProfileRepository, DoctorProfileRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register HttpClient for Notification service
builder.Services.AddHttpClient("NotificationService", client =>
{
    var baseUrl = builder.Configuration["NotificationService:BaseUrl"] ?? "http://localhost:5003";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

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
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    dbContext.Database.Migrate();
}

await SeedDefaultAdminAsync(app.Services, builder.Configuration, app.Logger);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection(); // Disabled for Docker - using HTTP only

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapAdminEndpoints();
app.MapDoctorProfileEndpoints();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Identity API", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.Run();

static async Task SeedDefaultAdminAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppIdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppIdentityRole>>();

    var adminEmail = configuration["SeedAdmin:Email"] ?? "admin@appointment-app.com";
    var adminPassword = configuration["SeedAdmin:Password"] ?? "Admin123!";
    var adminUserName = configuration["SeedAdmin:UserName"] ?? "admin";
    var adminFirstName = configuration["SeedAdmin:FirstName"] ?? "System";
    var adminLastName = configuration["SeedAdmin:LastName"] ?? "Admin";
    var resetPasswordOnStartup = bool.TryParse(configuration["SeedAdmin:ResetPasswordOnStartup"], out var parsed) && parsed;

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        var roleResult = await roleManager.CreateAsync(new AppIdentityRole
        {
            Name = "Admin",
            NormalizedName = "ADMIN",
            Description = "Administrator role with full access"
        });

        if (!roleResult.Succeeded)
        {
            logger.LogWarning("Failed to create Admin role during startup seeding: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            return;
        }
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new AppIdentityUser
        {
            Email = adminEmail,
            UserName = adminUserName,
            FirstName = adminFirstName,
            LastName = adminLastName,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("Failed to create default admin user during startup seeding: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }
    }
    else
    {
        var userChanged = false;

        if (!adminUser.EmailConfirmed)
        {
            adminUser.EmailConfirmed = true;
            userChanged = true;
        }

        if (!adminUser.IsActive)
        {
            adminUser.IsActive = true;
            userChanged = true;
        }

        if (string.IsNullOrWhiteSpace(adminUser.UserName))
        {
            adminUser.UserName = adminUserName;
            userChanged = true;
        }

        if (userChanged)
        {
            var updateResult = await userManager.UpdateAsync(adminUser);
            if (!updateResult.Succeeded)
            {
                logger.LogWarning("Failed to update existing admin user during startup seeding: {Errors}", string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            }
        }

        if (resetPasswordOnStartup)
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(adminUser);
            var resetResult = await userManager.ResetPasswordAsync(adminUser, resetToken, adminPassword);
            if (!resetResult.Succeeded)
            {
                logger.LogWarning("Failed to reset admin password during startup seeding: {Errors}", string.Join(", ", resetResult.Errors.Select(e => e.Description)));
            }
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        var assignRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
        if (!assignRoleResult.Succeeded)
        {
            logger.LogWarning("Failed to assign Admin role to seeded user: {Errors}", string.Join(", ", assignRoleResult.Errors.Select(e => e.Description)));
            return;
        }
    }

    logger.LogInformation("Default admin user is ready: {Email}", adminEmail);
}
