using DocumentApp.API.Endpoints;
using DocumentApp.API.Configuration;
using DocumentApp.Domain.Interfaces;
using DocumentApp.Postgres.Data;
using DocumentApp.Postgres.Repositories;
using DocumentApp.Repository.Interfaces;
using DocumentApp.Service;
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
builder.Services.ConfigureDatabase(builder.Configuration);

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
builder.Services.AddScoped<IBookingDocumentService, BookingDocumentService>();

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
await DatabaseConfiguration.EnsureDatabaseCreatedAndMigratedAsync(app.Services, app.Configuration, app.Logger);

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
app.MapBookingDocumentEndpoints();

app.Run();
