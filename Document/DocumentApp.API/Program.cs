using DocumentApp.API.Endpoints;
using DocumentApp.Domain.Interfaces;
using DocumentApp.Postgres.Data;
using DocumentApp.Postgres.Repositories;
using DocumentApp.Repository.Interfaces;
using DocumentApp.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<DocumentDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure());
});

// Register repositories
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentAccessRepository, DocumentAccessRepository>();

// Register services
builder.Services.AddSingleton<IMinioDocumentStorageService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new MinioDocumentStorageService(
        config["Minio:Endpoint"] ?? "minio:9000",
        config["Minio:AccessKey"] ?? "minioadmin",
        config["Minio:SecretKey"] ?? "minioadmin",
        bool.Parse(config["Minio:UseSSL"] ?? "false"),
        sp.GetRequiredService<ILogger<MinioDocumentStorageService>>()
    );
});

builder.Services.AddScoped<IDocumentService, DocumentService>();

// Authentication
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "your-very-long-secret-key-here-change-in-production";
var jwtKey = Encoding.UTF8.GetBytes(jwtSecret);

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "appointment-app",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "appointment-app",
        IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
    };
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Management", policy => policy.RequireRole("Management", "Admin"));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configure Kestrel to allow large file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

// Ensure database is created and migrated
await EnsureDatabaseCreatedAndMigratedAsync(app.Services, app.Configuration);

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapDocumentEndpoints();

app.Run();

static async Task EnsureDatabaseCreatedAndMigratedAsync(IServiceProvider services, IConfiguration configuration)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);

        // Check if database exists
        var dbName = builder.Database;
        builder.Database = "postgres";

        using var connection = new Npgsql.NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var checkDbCommand = connection.CreateCommand();
        checkDbCommand.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
        var dbExists = await checkDbCommand.ExecuteScalarAsync() != null;

        if (!dbExists)
        {
            var createDbCommand = connection.CreateCommand();
            createDbCommand.CommandText = $"CREATE DATABASE \"{dbName}\"";
            await createDbCommand.ExecuteNonQueryAsync();
            logger.LogInformation("Created database: {DatabaseName}", dbName);
        }

        await connection.CloseAsync();

        // Apply migrations
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error ensuring database created and migrated");
        throw;
    }
}
