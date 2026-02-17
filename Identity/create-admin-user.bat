@echo off
echo Creating default admin user...

REM Identity API URL
set API_URL=http://localhost:5005/api

REM Admin credentials
set ADMIN_EMAIL=admin@appointment-app.com
set ADMIN_PASS=Admin123456
set ADMIN_USERNAME=admin
set ADMIN_FIRSTNAME=System
set ADMIN_LASTNAME=Administrator

echo.
echo Registering admin account...
echo Email: %ADMIN_EMAIL%
echo Username: %ADMIN_USERNAME%
echo Password: %ADMIN_PASS%
echo.

curl -X POST "%API_URL%/auth/register" ^
  -H "Content-Type: application/json" ^
  -d "{\"email\": \"%ADMIN_EMAIL%\", \"password\": \"%ADMIN_PASS%\", \"userName\": \"%ADMIN_USERNAME%\", \"firstName\": \"%ADMIN_FIRSTNAME%\", \"lastName\": \"%ADMIN_LASTNAME%\", \"role\": \"Admin\"}"

echo.
echo Admin user created successfully!
echo.
echo You can now login with:
echo   Email: %ADMIN_EMAIL%
echo   Password: %ADMIN_PASS%
echo.
pause