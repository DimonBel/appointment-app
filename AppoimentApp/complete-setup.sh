#!/bin/bash

echo "================================================"
echo "Complete Setup: Databases + Demo Data + Backend"
echo "================================================"
echo ""

cd /home/dumas/Desktop/Final-proj/appointment-app/AppoimentApp

# Step 1: Setup Databases
echo "Step 1: Setting up PostgreSQL databases..."
PGPASSWORD=postgres psql -U postgres -c "DROP DATABASE IF EXISTS doctoravailabilitydb_dev;" 2>/dev/null
PGPASSWORD=postgres psql -U postgres -c "CREATE DATABASE doctoravailabilitydb_dev;" && echo "✓ Database created"

echo ""
echo "Step 2: Running migrations..."
cd DoctorAvailability.Api
dotnet ef database update --no-build 2>/dev/null || dotnet ef database update
cd ..

echo ""
echo "Step 3: Starting backend API..."
cd DoctorAvailability.Api
dotnet build > /dev/null 2>&1
dotnet run > /tmp/doctor-availability.log 2>&1 &
API_PID=$!
cd ..

echo "Waiting for API to start..."
sleep 5

# Test if API is running
if curl -s http://localhost:5112/api/DoctorSlot/all > /dev/null 2>&1; then
    echo "✓ API is running on http://localhost:5112"
else
    echo "✗ API failed to start. Check /tmp/doctor-availability.log"
    exit 1
fi

echo ""
echo "Step 4: Adding demo appointment data..."
echo ""

# Add appointments
appointments=(
    '{"date":"2026-02-13T09:00:00.000Z","doctorId":"11111111-1111-1111-1111-111111111111","doctorName":"Dr. Sarah Johnson","cost":150,"isReserved":false}'
    '{"date":"2026-02-13T10:30:00.000Z","doctorId":"22222222-2222-2222-2222-222222222222","doctorName":"Dr. Michael Chen","cost":125,"isReserved":false}'
    '{"date":"2026-02-13T14:00:00.000Z","doctorId":"33333333-3333-3333-3333-333333333333","doctorName":"Dr. Emily Rodriguez","cost":100,"isReserved":false}'
    '{"date":"2026-02-14T09:30:00.000Z","doctorId":"44444444-4444-4444-4444-444444444444","doctorName":"Dr. James Williams","cost":200,"isReserved":false}'
    '{"date":"2026-02-14T11:00:00.000Z","doctorId":"55555555-5555-5555-5555-555555555555","doctorName":"Dr. Olivia Martinez","cost":175,"isReserved":false}'
    '{"date":"2026-02-14T15:30:00.000Z","doctorId":"66666666-6666-6666-6666-666666666666","doctorName":"Dr. David Kim","cost":150,"isReserved":false}'
    '{"date":"2026-02-15T10:00:00.000Z","doctorId":"77777777-7777-7777-7777-777777777777","doctorName":"Dr. Jessica Taylor","cost":125,"isReserved":false}'
    '{"date":"2026-02-15T13:00:00.000Z","doctorId":"88888888-8888-8888-8888-888888888888","doctorName":"Dr. Robert Anderson","cost":100,"isReserved":false}'
    '{"date":"2026-02-16T09:00:00.000Z","doctorId":"11111111-1111-1111-1111-111111111111","doctorName":"Dr. Sarah Johnson","cost":150,"isReserved":false}'
    '{"date":"2026-02-16T14:30:00.000Z","doctorId":"22222222-2222-2222-2222-222222222222","doctorName":"Dr. Michael Chen","cost":125,"isReserved":false}'
    '{"date":"2026-02-17T10:30:00.000Z","doctorId":"33333333-3333-3333-3333-333333333333","doctorName":"Dr. Emily Rodriguez","cost":100,"isReserved":false}'
    '{"date":"2026-02-17T15:00:00.000Z","doctorId":"44444444-4444-4444-4444-444444444444","doctorName":"Dr. James Williams","cost":200,"isReserved":false}'
    '{"date":"2026-02-18T09:30:00.000Z","doctorId":"55555555-5555-5555-5555-555555555555","doctorName":"Dr. Olivia Martinez","cost":175,"isReserved":false}'
    '{"date":"2026-02-18T13:30:00.000Z","doctorId":"66666666-6666-6666-6666-666666666666","doctorName":"Dr. David Kim","cost":150,"isReserved":false}'
    '{"date":"2026-02-19T11:00:00.000Z","doctorId":"77777777-7777-7777-7777-777777777777","doctorName":"Dr. Jessica Taylor","cost":125,"isReserved":false}'
    '{"date":"2026-02-20T10:00:00.000Z","doctorId":"11111111-1111-1111-1111-111111111111","doctorName":"Dr. Sarah Johnson","cost":150,"isReserved":false}'
)

count=0
for appt in "${appointments[@]}"; do
    if curl -s -X POST http://localhost:5112/api/DoctorSlot \
        -H "Content-Type: application/json" \
        -d "$appt" > /dev/null 2>&1; then
        count=$((count + 1))
        echo "✓ Added appointment $count"
    fi
done

echo ""
echo "================================================"
echo "✓ Setup Complete!"
echo "================================================"
echo ""
echo "Added $count demo appointments"
echo ""
echo "Services running:"
echo "  • Doctor Availability API: http://localhost:5112"
echo "  • Swagger UI: http://localhost:5112/swagger"
echo ""
echo "View appointments:"
echo "  curl http://localhost:5112/api/DoctorSlot/available | jq"
echo ""
echo "Backend API PID: $API_PID"
echo "To stop: kill $API_PID"
echo ""
echo "Now start the frontend:"
echo "  cd appointmentapp-frontend-react"
echo "  npm run dev"
echo ""
