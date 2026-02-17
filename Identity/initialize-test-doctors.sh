#!/bin/bash

echo "Initializing test doctor users and profiles..."

# Wait for API to be ready
sleep 5

# Identity API URL
API_URL="http://localhost:5005/api"

# Create test doctor accounts
echo "Creating test doctor accounts..."

# Doctor 1 - Cardiologist
DOCTOR1_EMAIL="john.cardio@example.com"
DOCTOR1_PASS="Password123"

curl -X POST "${API_URL}/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"${DOCTOR1_EMAIL}\",
    \"password\": \"${DOCTOR1_PASS}\",
    \"userName\": \"john_cardio\",
    \"firstName\": \"John\",
    \"lastName\": \"Smith\",
    \"role\": \"Professional\"
  }"

echo "Doctor 1 registered"

# Login to get token
DOCTOR1_TOKEN=$(curl -X POST "${API_URL}/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\": \"${DOCTOR1_EMAIL}\", \"password\": \"${DOCTOR1_PASS}\"}" \
  | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

echo "Doctor 1 token obtained"

# Create profile for Doctor 1
curl -X POST "${API_URL}/doctor-profiles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ${DOCTOR1_TOKEN}" \
  -d '{
    "specialty": "Cardiology",
    "bio": "Experienced cardiologist with over 15 years of practice. Specialized in heart disease prevention and treatment.",
    "qualifications": "MD, FACC, Board Certified in Cardiovascular Disease",
    "yearsOfExperience": 15,
    "services": ["Heart Disease Treatment", "Preventive Cardiology", "ECG Testing", "Stress Testing"],
    "workingHours": "Mon-Fri 09:00-17:00",
    "consultationFee": 150.00,
    "languages": ["English", "Spanish"],
    "address": "123 Medical Center Drive",
    "city": "New York",
    "country": "USA"
  }'

echo "Doctor 1 profile created"

# Doctor 2 - Pediatrician
DOCTOR2_EMAIL="sarah.pediatrics@example.com"
DOCTOR2_PASS="Password123"

curl -X POST "${API_URL}/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"${DOCTOR2_EMAIL}\",
    \"password\": \"${DOCTOR2_PASS}\",
    \"userName\": \"sarah_pediatrics\",
    \"firstName\": \"Sarah\",
    \"lastName\": \"Johnson\",
    \"role\": \"Professional\"
  }"

echo "Doctor 2 registered"

DOCTOR2_TOKEN=$(curl -X POST "${API_URL}/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\": \"${DOCTOR2_EMAIL}\", \"password\": \"${DOCTOR2_PASS}\"}" \
  | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

echo "Doctor 2 token obtained"

curl -X POST "${API_URL}/doctor-profiles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ${DOCTOR2_TOKEN}" \
  -d '{
    "specialty": "Pediatrics",
    "bio": "Caring pediatrician dedicated to children'\''s health and wellbeing. Experienced in childhood diseases and vaccinations.",
    "qualifications": "MD, FAAP, Board Certified in Pediatrics",
    "yearsOfExperience": 10,
    "services": ["Well-Child Visits", "Vaccinations", "Sick Visits", "Development Screening"],
    "workingHours": "Mon-Fri 08:00-16:00, Sat 09:00-13:00",
    "consultationFee": 120.00,
    "languages": ["English", "French"],
    "address": "456 Children'\''s Hospital Lane",
    "city": "Los Angeles",
    "country": "USA"
  }'

echo "Doctor 2 profile created"

# Doctor 3 - Dermatologist
DOCTOR3_EMAIL="emily.derma@example.com"
DOCTOR3_PASS="Password123"

curl -X POST "${API_URL}/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"${DOCTOR3_EMAIL}\",
    \"password\": \"${DOCTOR3_PASS}\",
    \"userName\": \"emily_derma\",
    \"firstName\": \"Emily\",
    \"lastName\": \"Davis\",
    \"role\": \"Professional\"
  }"

echo "Doctor 3 registered"

DOCTOR3_TOKEN=$(curl -X POST "${API_URL}/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\": \"${DOCTOR3_EMAIL}\", \"password\": \"${DOCTOR3_PASS}\"}" \
  | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

echo "Doctor 3 token obtained"

curl -X POST "${API_URL}/doctor-profiles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ${DOCTOR3_TOKEN}" \
  -d '{
    "specialty": "Dermatology",
    "bio": "Board-certified dermatologist specializing in medical and cosmetic dermatology. Expert in skin cancer detection and treatment.",
    "qualifications": "MD, FAAD, Board Certified in Dermatology",
    "yearsOfExperience": 8,
    "services": ["Skin Cancer Screening", "Acne Treatment", "Cosmetic Procedures", "Laser Treatments"],
    "workingHours": "Mon-Thu 10:00-18:00, Fri 10:00-16:00",
    "consultationFee": 130.00,
    "languages": ["English", "German"],
    "address": "789 Dermatology Plaza",
    "city": "Chicago",
    "country": "USA"
  }'

echo "Doctor 3 profile created"

echo "Test doctors initialization complete!"
