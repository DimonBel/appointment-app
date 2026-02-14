#!/bin/bash

# Setup script for AppointmentOrderApp database

echo "Setting up AppointmentOrderApp database..."

# Change to the API project directory
cd "$(dirname "$0")/AppointmentApp.API"

# Restore NuGet packages
echo "Restoring NuGet packages..."
dotnet restore

# Add EF Core tools if not already installed
echo "Installing EF Core tools..."
dotnet tool install --global dotnet-ef || echo "EF Core tools already installed"

# Create initial migration
echo "Creating initial migration..."
dotnet ef migrations add InitialCreate --project ../AppointmentApp.Postgres --startup-project .

# Update database
echo "Updating database..."
dotnet ef database update --project ../AppointmentApp.Postgres --startup-project .

echo "Database setup complete!"
echo "You can now run the application with: dotnet run --project ../AppointmentApp.API"