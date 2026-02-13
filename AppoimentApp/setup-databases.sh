#!/bin/bash

echo "Setting up Databases and Migrations"
echo "===================================="

# Create databases
echo "Creating PostgreSQL databases..."

PGPASSWORD=postgres psql -U postgres -c "DROP DATABASE IF EXISTS doctoravailabilitydb_dev;" 2>/dev/null
PGPASSWORD=postgres psql -U postgres -c "CREATE DATABASE doctoravailabilitydb_dev;" && echo "✓ Created doctoravailabilitydb_dev"

PGPASSWORD=postgres psql -U postgres -c "DROP DATABASE IF EXISTS appointmentbookingdb_dev;" 2>/dev/null
PGPASSWORD=postgres psql -U postgres -c "CREATE DATABASE appointmentbookingdb_dev;" && echo "✓ Created appointmentbookingdb_dev"

PGPASSWORD=postgres psql -U postgres -c "DROP DATABASE IF EXISTS doctorappointmentmanagementdb_dev;" 2>/dev/null
PGPASSWORD=postgres psql -U postgres -c "CREATE DATABASE doctorappointmentmanagementdb_dev;" && echo "✓ Created doctorappointmentmanagementdb_dev"

echo ""
echo "Running migrations..."

# Run migrations for Doctor Availability
cd DoctorAvailability.Api
echo "Migrating DoctorAvailability..."
dotnet ef database update
cd ..

# Run migrations for Appointment Booking
cd AppointmentBooking.Presention
echo "Migrating AppointmentBooking..."
dotnet ef database update
cd ..

# Run migrations for Management
cd DoctorAppointmentManagement.Presention
echo "Migrating DoctorAppointmentManagement..."
dotnet ef database update
cd ..

echo ""
echo "✓ Database setup complete!"
echo ""
