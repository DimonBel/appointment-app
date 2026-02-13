#!/bin/bash

echo "========================================="
echo "Initializing Demo Data for Appointment App"
echo "========================================="

# Wait for APIs to be ready
echo "Waiting for APIs to start..."
sleep 5

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Doctor names array
DOCTORS=("Dr. Sarah Johnson" "Dr. Michael Chen" "Dr. Emily Rodriguez" "Dr. James Williams" "Dr. Olivia Martinez" "Dr. David Kim" "Dr. Jessica Taylor" "Dr. Robert Anderson")

# Costs array
COSTS=(75 100 125 150 200)

echo -e "${BLUE}Adding appointment slots...${NC}"

# Generate slots for next 30 days
for day in {0..29}; do
    # Random number of slots per day (3-5)
    slots_per_day=$((3 + RANDOM % 3))
    
    for ((i=0; i<slots_per_day; i++)); do
        # Random doctor
        doctor_index=$((RANDOM % ${#DOCTORS[@]}))
        doctor_name="${DOCTORS[$doctor_index]}"
        
        # Random cost
        cost_index=$((RANDOM % ${#COSTS[@]}))
        cost="${COSTS[$cost_index]}"
        
        # Random hour (9-17) and minute (0 or 30)
        hour=$((9 + RANDOM % 9))
        minute=$((RANDOM % 2 * 30))
        
        # Calculate date
        if [[ "$OSTYPE" == "darwin"* ]]; then
            # macOS
            slot_date=$(date -u -v+${day}d -v${hour}H -v${minute}M -v0S +"%Y-%m-%dT%H:%M:%S.000Z")
        else
            # Linux
            slot_date=$(date -u -d "+${day} days ${hour}:${minute}:00" +"%Y-%m-%dT%H:%M:%S.000Z")
        fi
        
        # Generate random GUID
        doctor_id=$(cat /proc/sys/kernel/random/uuid)
        
        # Create JSON payload
        json_payload=$(cat <<EOF
{
    "date": "${slot_date}",
    "doctorId": "${doctor_id}",
    "doctorName": "${doctor_name}",
    "cost": ${cost},
    "isReserved": false
}
EOF
)
        
        # Send POST request
        response=$(curl -s -w "\n%{http_code}" -X POST \
            "http://localhost:5112/api/DoctorSlot" \
            -H "Content-Type: application/json" \
            -d "$json_payload")
        
        http_code=$(echo "$response" | tail -n1)
        
        if [ "$http_code" == "200" ] || [ "$http_code" == "201" ]; then
            echo -e "${GREEN}✓${NC} Added slot: ${doctor_name} on ${slot_date:0:10} at ${slot_date:11:5}"
        else
            echo -e "${RED}✗${NC} Failed to add slot (HTTP $http_code)"
        fi
        
        # Small delay to avoid overwhelming the API
        sleep 0.1
    done
done

echo ""
echo -e "${GREEN}=========================================${NC}"
echo -e "${GREEN}Demo data initialization complete!${NC}"
echo -e "${GREEN}=========================================${NC}"
echo ""
echo "API Endpoints:"
echo "  - Doctor Availability: http://localhost:5112/swagger"
echo "  - Appointment Booking: http://localhost:5167/swagger"
echo "  - Management: http://localhost:5113/swagger"
echo ""
echo "You can now access the frontend and see the demo appointments!"
