#!/bin/bash

# Initialize demo data for AppointmentOrderApp

echo "Initializing demo data..."

# Build the solution
echo "Building solution..."
cd "$(dirname "$0")"
dotnet build

# Create a simple console app to seed data
cat > SeedDemoData.cs << 'EOF'
using AppointmentApp.Domain.Entity;
using AppointmentApp.Postgres.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppIdentityUser, AppIdentityRole>()
    .AddEntityFrameworkStores<AppointmentDbContext>()
    .AddDefaultTokenProviders();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppIdentityUser>>();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppIdentityRole>>();

await SeedData.SeedAsync(context, userManager, roleManager);

Console.WriteLine("Demo data initialized successfully!");
EOF

# Run the seeding
echo "Running seed data..."
dotnet run --project ../AppointmentApp.API --launch-profile "SeedDemoData" -- SeedDemoData.cs

# Cleanup
rm SeedDemoData.cs

echo "Demo data initialization complete!"