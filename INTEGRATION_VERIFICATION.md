# Identity Service Integration - Verification Guide

## Overview

This guide explains how the Identity microservice has been integrated with AppointmentApp and ChatApp, and how to verify that both applications are successfully using the centralized authentication service.

## Architecture

```
┌─────────────────────┐
│  Identity Service   │
│  (Port 5005)        │
│  - Registration     │
│  - Login            │
│  - JWT Tokens       │
│  - User Management  │
└──────────▲──────────┘
           │
           │ REST API (HTTP + JSON)
           │
    ┌──────┴──────────┐
    │                 │
┌───▼───────────┐ ┌──▼──────────────┐
│ AppointmentApp│ │    ChatApp      │
│ (Port: TBD)   │ │  (Port: TBD)    │
│               │ │                 │
│ - Auth Proxy  │ │  - Auth Proxy   │
│ - JWT Bearer  │ │  - JWT Bearer   │
│ - HttpClient  │ │  - HttpClient   │
└───────────────┘ └─────────────────┘
```

## What Was Changed

### 1. Identity Service (localhost:5005) - Already Running ✅

- **Database**: IdentityDb (PostgreSQL)
- **Authentication**: JWT with Refresh Tokens
- **Endpoints**:
  - `POST /api/auth/register` - User registration
  - `POST /api/auth/login` - User login
  - `POST /api/auth/refresh` - Refresh access token
  - `POST /api/auth/revoke` - Revoke refresh token
  - `POST /api/auth/validate` - Validate access token
  - `GET /api/users/{id}` - Get user by ID
  - `PUT /api/users/{id}` - Update user
  - `DELETE /api/users/{id}` - Delete user

### 2. AppointmentApp Integration ✅

#### Files Modified/Created:

1. **appsettings.json & appsettings.Development.json**
   - Added `IdentityService.BaseUrl`: "http://localhost:5005"
   - Added `Jwt` configuration matching Identity service

2. **DTOs/Identity/IdentityDtos.cs** (NEW)
   - Shared DTOs for communication with Identity service
   - `RegisterDto`, `LoginDto`, `RefreshTokenDto`, `IdentityUserDto`, `AuthResponseDto`

3. **Services/IdentityServiceClient.cs** (NEW)
   - HttpClient wrapper for calling Identity service REST API
   - Methods: `RegisterAsync`, `LoginAsync`, `RefreshTokenAsync`, `GetUserByIdAsync`, etc.

4. **Program.cs**
   - Added `using AppointmentApp.API.Services`
   - Registered `HttpClient<IIdentityServiceClient>` with Identity service base URL
   - Updated JWT authentication to use same settings as Identity service

5. **Endpoints/AuthEndpoints.cs** (NEW)
   - Created proxy endpoints that forward authentication requests to Identity service
   - All endpoints use `IIdentityServiceClient` instead of local authentication

### 3. ChatApp Integration ✅

#### Files Modified/Created:

1. **appsettings.json & appsettings.Development.json**
   - Added `IdentityService.BaseUrl`: "http://localhost:5005"
   - Added `Jwt` configuration matching Identity service

2. **DTOs/Identity/IdentityDtos.cs** (NEW)
   - Same shared DTOs as AppointmentApp

3. **Services/IdentityServiceClient.cs** (NEW)
   - Identical HttpClient wrapper for Identity service communication

4. **Program.cs**
   - Added `using ChatApp.API.Services`
   - **CHANGED**: Authentication from Cookie to JWT Bearer
   - Registered `HttpClient<IIdentityServiceClient>`
   - Configured JWT validation with same settings as Identity service
   - Added SignalR JWT support (via query string `access_token`)

5. **Endpoints/AuthEndpoints.cs** (UPDATED)
   - **CHANGED**: From using local `IAuthService` to `IIdentityServiceClient`
   - All endpoints now proxy to Identity service

## JWT Configuration (Synchronized Across All Services)

```json
{
  "Jwt": {
    "Issuer": "IdentityApp",
    "Audience": "IdentityAppClients",
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForIdentityService!"
  }
}
```

**Important**: All three services must use the SAME secret key for token validation to work.

## How to Verify Integration

### Step 1: Start All Services

1. **Start Identity Service** (Port 5005)

   ```bash
   cd d:\Duma\appointment-app\Identity\IdentityApp.API
   dotnet run
   ```

   Should display: `Now listening on: http://localhost:5005`

2. **Start AppointmentApp**

   ```bash
   cd d:\Duma\appointment-app\Appointment\AppointmentApp.API
   dotnet run
   ```

   Note the port (e.g., http://localhost:5xxx)

3. **Start ChatApp**
   ```bash
   cd d:\Duma\appointment-app\ChatApp\ChatApp.API
   dotnet run
   ```
   Note the port (e.g., http://localhost:5yyy)

### Step 2: Test Registration Flow

#### Test AppointmentApp Registration:

```bash
curl -X POST http://localhost:<appointment-port>/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

**Expected Response** (from Identity service):

```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123...",
  "expiresAt": "2024-01-15T12:00:00Z",
  "user": {
    "userId": "guid-here",
    "email": "test@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "avatarUrl": null,
    "isOnline": false,
    "roles": ["User"]
  }
}
```

#### Test ChatApp Registration:

```bash
curl -X POST http://localhost:<chat-port>/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "chat@example.com",
    "password": "Test123",
    "userName": "ChatUser"
  }'
```

**Important**: Both registrations should create users in the **IdentityDb** database, NOT in local databases.

### Step 3: Test Login Flow

#### Login via AppointmentApp:

```bash
curl -X POST http://localhost:<appointment-port>/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123"
  }'
```

#### Login via ChatApp:

```bash
curl -X POST http://localhost:<chat-port>/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "chat@example.com",
    "password": "Test123"
  }'
```

**Expected**: Both should return JWT tokens from Identity service.

### Step 4: Verify Token Validation

Use the access token from login response:

#### Test Protected Endpoint in AppointmentApp:

```bash
curl -X GET http://localhost:<appointment-port>/api/auth/me \
  -H "Authorization: Bearer <access-token>"
```

#### Test Protected Endpoint in ChatApp:

```bash
curl -X GET http://localhost:<chat-port>/api/auth/me \
  -H "Authorization: Bearer <access-token>"
```

**Expected**: Should return user information.

### Step 5: Verify Database

Check PostgreSQL `IdentityDb` database:

```sql
-- Should show users registered from both apps
SELECT "Id", "Email", "FirstName", "LastName", "IsOnline"
FROM "AspNetUsers";

-- Should show refresh tokens
SELECT * FROM "RefreshTokens";
```

## Verification Checklist

- [ ] Identity service starts on port 5005
- [ ] AppointmentApp starts without errors
- [ ] ChatApp starts without errors
- [ ] Can register user via AppointmentApp → user created in IdentityDb
- [ ] Can register user via ChatApp → user created in IdentityDb
- [ ] Can login via AppointmentApp → JWT token returned
- [ ] Can login via ChatApp → JWT token returned
- [ ] JWT token from AppointmentApp works on protected endpoints
- [ ] JWT token from ChatApp works on protected endpoints
- [ ] Can refresh token via both apps
- [ ] Can logout (revoke token) via both apps

## Common Issues & Solutions

### Issue 1: "Connection refused" when calling Identity service

**Cause**: Identity service is not running  
**Solution**: Start Identity service on port 5005

### Issue 2: "401 Unauthorized" on protected endpoints

**Cause**: JWT token validation failing  
**Solution**: Verify all services use the same `Jwt:SecretKey` in appsettings.json

### Issue 3: User created in local database instead of IdentityDb

**Cause**: Old authentication code still active  
**Solution**: Verify `AuthEndpoints.cs` uses `IIdentityServiceClient`, not `IAuthService`

### Issue 4: CORS errors in browser

**Cause**: CORS policy not allowing requests  
**Solution**: All services have "AllowAll" CORS policy in development

## Testing with Swagger/OpenAPI

All three services expose OpenAPI documentation:

- **Identity Service**: http://localhost:5005/swagger (if Swagger enabled)
- **AppointmentApp**: http://localhost:<port>/swagger
- **ChatApp**: http://localhost:<port>/swagger

Use Swagger UI to test endpoints interactively.

## Next Steps

After verifying integration:

1. **Frontend Integration**:
   - Update AppointmentApp frontend to use new auth endpoints
   - Update ChatApp frontend to use JWT instead of cookies

2. **Security Enhancements**:
   - Use HTTPS in production
   - Store JWT secret in environment variables/Azure Key Vault
   - Implement token rotation

3. **Additional Features**:
   - Email verification
   - Password reset
   - Two-factor authentication
   - User roles management UI

## Architecture Benefits

✅ **Single Source of Truth**: All users managed in one database  
✅ **Centralized Authentication**: Consistent auth logic across services  
✅ **JWT Tokens**: Stateless, scalable authentication  
✅ **Service Independence**: Each service can run independently  
✅ **REST API Communication**: Standard HTTP/JSON protocol  
✅ **Easy to Extend**: Add new services easily

## Support

If you encounter any issues:

1. Check logs in all three service consoles
2. Verify database connections (PostgreSQL on localhost:5432)
3. Ensure all services use consistent JWT configuration
4. Check that Identity service is running before starting other services

---

**Integration Date**: 2024  
**Services Integrated**: Identity Service ↔ AppointmentApp ↔ ChatApp  
**Communication Protocol**: REST API (HTTP + JSON)  
**Authentication**: JWT Bearer Tokens with Refresh Tokens
