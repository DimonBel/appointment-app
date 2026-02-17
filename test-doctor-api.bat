@echo off
echo Testing Doctor Profile API...
echo.

REM Register a test doctor
echo Step 1: Registering test doctor...
curl -X POST http://localhost:5005/api/auth/register ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"test.doctor@example.com\",\"password\":\"Password123\",\"userName\":\"testdoc\",\"firstName\":\"Test\",\"lastName\":\"Doctor\",\"role\":\"Professional\"}"
echo.
echo.

REM Login and get token
echo Step 2: Logging in...
for /f "tokens=*" %%a in ('curl -s -X POST http://localhost:5005/api/auth/login -H "Content-Type: application/json" -d "{\"email\":\"test.doctor@example.com\",\"password\":\"Password123\"}"') do set LOGIN_RESPONSE=%%a
echo %LOGIN_RESPONSE%
echo.

REM Extract token (this is a simplified version - you might need to parse JSON properly)
echo Step 3: Creating doctor profile...
REM For now, you'll need to manually copy the accessToken from the login response
echo Please copy the accessToken from above and paste it here:
set /p TOKEN=Token: 

curl -X POST http://localhost:5005/api/doctor-profiles ^
  -H "Content-Type: application/json" ^
  -H "Authorization: Bearer %TOKEN%" ^
  -d "{\"specialty\":\"Cardiology\",\"bio\":\"Test bio\",\"qualifications\":\"MD\",\"yearsOfExperience\":5,\"services\":[\"Consultation\"],\"workingHours\":\"Mon-Fri 9-5\",\"consultationFee\":100,\"languages\":[\"English\"],\"address\":\"123 Test St\",\"city\":\"Test City\",\"country\":\"USA\"}"
echo.
echo.

echo Step 4: Getting all profiles...
curl -X GET http://localhost:5005/api/doctor-profiles
echo.
echo.

echo Done!
pause
