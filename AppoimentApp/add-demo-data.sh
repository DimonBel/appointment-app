#!/bin/bash

echo "Quick Demo Data Setup"
echo "====================="

# Test if API is responding
echo "Testing API connection..."
if curl -s http://localhost:5112/api/DoctorSlot/all > /dev/null 2>&1; then
    echo "✓ API is running"
else
    echo "✗ API is not responding. Please make sure the backend is running."
    echo "Run: cd DoctorAvailability.Api && dotnet run"
    exit 1
fi

echo ""
echo "Adding 20 sample appointments..."
echo ""

# Sample appointments with specific dates
curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-13T09:00:00.000Z",
    "doctorId": "11111111-1111-1111-1111-111111111111",
    "doctorName": "Dr. Sarah Johnson",
    "cost": 150,
    "isReserved": false
  }' && echo "✓ Added Dr. Sarah Johnson - Feb 13, 9:00 AM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-13T10:30:00.000Z",
    "doctorId": "22222222-2222-2222-2222-222222222222",
    "doctorName": "Dr. Michael Chen",
    "cost": 125,
    "isReserved": false
  }' && echo "✓ Added Dr. Michael Chen - Feb 13, 10:30 AM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-13T14:00:00.000Z",
    "doctorId": "33333333-3333-3333-3333-333333333333",
    "doctorName": "Dr. Emily Rodriguez",
    "cost": 100,
    "isReserved": false
  }' && echo "✓ Added Dr. Emily Rodriguez - Feb 13, 2:00 PM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-14T09:30:00.000Z",
    "doctorId": "44444444-4444-4444-4444-444444444444",
    "doctorName": "Dr. James Williams",
    "cost": 200,
    "isReserved": false
  }' && echo "✓ Added Dr. James Williams - Feb 14, 9:30 AM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-14T11:00:00.000Z",
    "doctorId": "55555555-5555-5555-5555-555555555555",
    "doctorName": "Dr. Olivia Martinez",
    "cost": 175,
    "isReserved": false
  }' && echo "✓ Added Dr. Olivia Martinez - Feb 14, 11:00 AM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-14T15:30:00.000Z",
    "doctorId": "66666666-6666-6666-6666-666666666666",
    "doctorName": "Dr. David Kim",
    "cost": 150,
    "isReserved": false
  }' && echo "✓ Added Dr. David Kim - Feb 14, 3:30 PM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-15T10:00:00.000Z",
    "doctorId": "77777777-7777-7777-7777-777777777777",
    "doctorName": "Dr. Jessica Taylor",
    "cost": 125,
    "isReserved": false
  }' && echo "✓ Added Dr. Jessica Taylor - Feb 15, 10:00 AM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-15T13:00:00.000Z",
    "doctorId": "88888888-8888-8888-8888-888888888888",
    "doctorName": "Dr. Robert Anderson",
    "cost": 100,
    "isReserved": false
  }' && echo "✓ Added Dr. Robert Anderson - Feb 15, 1:00 PM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-16T09:00:00.000Z",
    "doctorId": "11111111-1111-1111-1111-111111111111",
    "doctorName": "Dr. Sarah Johnson",
    "cost": 150,
    "isReserved": false
  }' && echo "✓ Added Dr. Sarah Johnson - Feb 16, 9:00 AM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-16T14:30:00.000Z",
    "doctorId": "22222222-2222-2222-2222-222222222222",
    "doctorName": "Dr. Michael Chen",
    "cost": 125,
    "isReserved": false
  }' && echo "✓ Added Dr. Michael Chen - Feb 16, 2:30 PM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-17T10:30:00.000Z",
    "doctorId": "33333333-3333-3333-3333-333333333333",
    "doctorName": "Dr. Emily Rodriguez",
    "cost": 100,
    "isReserved": false
  }' && echo "✓ Added Dr. Emily Rodriguez - Feb 17, 10:30 AM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-17T15:00:00.000Z",
    "doctorId": "44444444-4444-4444-4444-444444444444",
    "doctorName": "Dr. James Williams",
    "cost": 200,
    "isReserved": false
  }' && echo "✓ Added Dr. James Williams - Feb 17, 3:00 PM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-18T09:30:00.000Z",
    "doctorId": "55555555-5555-5555-5555-555555555555",
    "doctorName": "Dr. Olivia Martinez",
    "cost": 175,
    "isReserved": false
  }' && echo "✓ Added Dr. Olivia Martinez - Feb 18, 9:30 AM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-18T13:30:00.000Z",
    "doctorId": "66666666-6666-6666-6666-666666666666",
    "doctorName": "Dr. David Kim",
    "cost": 150,
    "isReserved": false
  }' && echo "✓ Added Dr. David Kim - Feb 18, 1:30 PM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-19T11:00:00.000Z",
    "doctorId": "77777777-7777-7777-7777-777777777777",
    "doctorName": "Dr. Jessica Taylor",
    "cost": 125,
    "isReserved": false
  }' && echo "✓ Added Dr. Jessica Taylor - Feb 19, 11:00 AM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-19T16:00:00.000Z",
    "doctorId": "88888888-8888-8888-8888-888888888888",
    "doctorName": "Dr. Robert Anderson",
    "cost": 100,
    "isReserved": false
  }' && echo "✓ Added Dr. Robert Anderson - Feb 19, 4:00 PM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-20T10:00:00.000Z",
    "doctorId": "11111111-1111-1111-1111-111111111111",
    "doctorName": "Dr. Sarah Johnson",
    "cost": 150,
    "isReserved": false
  }' && echo "✓ Added Dr. Sarah Johnson - Feb 20, 10:00 AM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-20T14:00:00.000Z",
    "doctorId": "22222222-2222-2222-2222-222222222222",
    "doctorName": "Dr. Michael Chen",
    "cost": 125,
    "isReserved": false
  }' && echo "✓ Added Dr. Michael Chen - Feb 20, 2:00 PM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-21T09:00:00.000Z",
    "doctorId": "33333333-3333-3333-3333-333333333333",
    "doctorName": "Dr. Emily Rodriguez",
    "cost": 100,
    "isReserved": false
  }' && echo "✓ Added Dr. Emily Rodriguez - Feb 21, 9:00 AM"

curl -X POST http://localhost:5112/api/DoctorSlot \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-02-21T15:30:00.000Z",
    "doctorId": "44444444-4444-4444-4444-444444444444",
    "doctorName": "Dr. James Williams",
    "cost": 200,
    "isReserved": false
  }' && echo "✓ Added Dr. James Williams - Feb 21, 3:30 PM"

echo ""
echo "============================================"
echo "✓ Successfully added 20 demo appointments!"
echo "============================================"
echo ""
echo "You can now:"
echo "  1. View all slots: http://localhost:5112/api/DoctorSlot/all"
echo "  2. View available: http://localhost:5112/api/DoctorSlot/available"
echo "  3. Open Swagger UI: http://localhost:5112/swagger"
echo ""
echo "Frontend will now show these appointments!"
