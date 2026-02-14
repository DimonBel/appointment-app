# Identity Service Integration Guide

## Overview

This guide explains how to integrate the Identity microservice with AppointmentApp and ChatApp using REST API calls.

## Identity Service Endpoints

Base URL: `http://localhost:5000` (update with your actual URL)

### Authentication Endpoints

#### Register New User

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "123123",
  "userName": "username",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890"
}
```

**Response:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "Q3Vyc2l2ZSBhcHByb2Fj...",
  "expiresAt": "2026-02-14T12:00:00Z",
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "userName": "username",
    "firstName": "John",
    "lastName": "Doe",
    "roles": ["User"]
  }
}
```

#### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "123123"
}
```

#### Refresh Token

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "accessToken": "expired_token",
  "refreshToken": "valid_refresh_token"
}
```

#### Validate Token

```http
POST /api/auth/validate
Content-Type: application/json

"your_token_here"
```

### User Management Endpoints

All user management endpoints require authentication (Bearer token).

#### Get User by ID

```http
GET /api/users/{userId}
Authorization: Bearer {access_token}
```

#### Get User by Email

```http
GET /api/users/email/{email}
Authorization: Bearer {access_token}
```

## Integration with AppointmentApp

### Step 1: Add HttpClient for Identity Service

Add to `appsettings.json`:

```json
{
  "IdentityService": {
    "BaseUrl": "http://localhost:5000"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForIdentityService!",
    "Issuer": "IdentityApp",
    "Audience": "IdentityAppClients"
  }
}
```

### Step 2: Create Identity Service Client

Create `Services/IdentityServiceClient.cs`:

```csharp
public interface IIdentityServiceClient
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto model);
    Task<AuthResponseDto?> LoginAsync(LoginDto model);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<bool> ValidateTokenAsync(string token);
}

public class IdentityServiceClient : IIdentityServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public IdentityServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.BaseAddress = new Uri(_configuration["IdentityService:BaseUrl"]!);
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto model)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", model);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto model)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", model);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var response = await _httpClient.GetAsync($"/api/users/{userId}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/validate", token);
        if (!response.IsSuccessStatusCode) return false;
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty("isValid").GetBoolean();
    }
}
```

### Step 3: Register Services in Program.cs

```csharp
// Add HttpClient for Identity Service
builder.Services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>();

// Keep JWT authentication but configure to use Identity Service keys
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

### Step 4: Update Authentication Endpoints

Replace local authentication logic with Identity Service calls:

```csharp
app.MapPost("/api/auth/register", async (RegisterDto model, IIdentityServiceClient identityService) =>
{
    var result = await identityService.RegisterAsync(model);
    return result != null ? Results.Ok(result) : Results.BadRequest();
});

app.MapPost("/api/auth/login", async (LoginDto model, IIdentityServiceClient identityService) =>
{
    var result = await identityService.LoginAsync(model);
    return result != null ? Results.Ok(result) : Results.Unauthorized();
});
```

### Step 5: Remove Local Identity Configuration

You can now remove:

- Local Identity configuration from `Program.cs`
- Local user database tables (keep only business-specific tables)
- Local authentication services

## Integration with ChatApp

Similar steps for ChatApp:

### Step 1: Add Configuration

Update `appsettings.json` with the same Identity Service configuration.

### Step 2: Add Identity Service Client

Copy the `IdentityServiceClient` from AppointmentApp or create a shared library.

### Step 3: Update Program.cs

```csharp
// Remove local Identity configuration
// builder.Services.AddIdentity<AppIdentityUser, AppIdentityRole>...

// Add HttpClient for Identity Service
builder.Services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>();

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

### Step 4: Update Authentication Endpoints

Replace `AuthEndpoints.cs` to use Identity Service:

```csharp
group.MapPost("/register", async (RegisterDto model, IIdentityServiceClient identityService) =>
{
    var result = await identityService.RegisterAsync(model);
    return result != null
        ? Results.Ok(new { message = "Registration successful", result })
        : Results.BadRequest(new { message = "Registration failed" });
});

group.MapPost("/login", async (LoginDto model, IIdentityServiceClient identityService) =>
{
    var result = await identityService.LoginAsync(model);
    return result != null
        ? Results.Ok(new { message = "Login successful", result })
        : Results.Unauthorized();
});
```

## JWT Token Usage

Once you receive a token from Identity Service:

### In API Requests

```http
GET /api/protected-resource
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### In C# HttpClient

```csharp
_httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", accessToken);
```

### In JavaScript (Frontend)

```javascript
fetch("http://localhost:5000/api/users/me", {
  headers: {
    Authorization: `Bearer ${accessToken}`,
  },
});
```

## Important Security Notes

1. **Use HTTPS in Production** - Never send tokens over HTTP
2. **Store tokens securely** - Use HttpOnly cookies or secure storage
3. **Implement token refresh** - Use refresh tokens before access token expires
4. **Validate tokens** - Always validate tokens on protected endpoints
5. **Use same SecretKey** - All services must use the same JWT secret key
6. **CORS Configuration** - Configure CORS properly for frontend apps

## Token Claims

The JWT token includes these claims:

```json
{
  "nameid": "user-guid",
  "name": "username",
  "email": "user@example.com",
  "role": ["User"],
  "FirstName": "John",
  "LastName": "Doe",
  "jti": "token-id",
  "exp": 1644854400,
  "iss": "IdentityApp",
  "aud": "IdentityAppClients"
}
```

Access claims in your code:

```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = User.FindFirst(ClaimTypes.Email)?.Value;
var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
```

## Error Handling

Handle common errors:

### 401 Unauthorized

- Token expired
- Invalid token
- No token provided

**Solution**: Refresh token or redirect to login

### 403 Forbidden

- User doesn't have required role
- User account deactivated

**Solution**: Show appropriate error message

### 400 Bad Request

- Invalid registration data
- User already exists

**Solution**: Show validation errors to user

## Testing

### Using curl

```bash
# Register
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"123123","userName":"testuser"}'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"123123"}'

# Access protected endpoint
curl -X GET http://localhost:5000/api/users/{userId} \
  -H "Authorization: Bearer {your_token}"
```

### Using Postman

1. Create new request
2. Set method and URL
3. Add JSON body for POST requests
4. Add Authorization header with Bearer token
5. Send request

## Troubleshooting

### Token validation fails

- Ensure all services use the same JWT SecretKey
- Check Issuer and Audience match
- Verify token hasn't expired

### Cannot connect to Identity Service

- Check Identity Service is running
- Verify BaseUrl in configuration
- Check firewall/network settings

### Database connection errors

- Verify PostgreSQL is running
- Check connection string
- Ensure database exists

## Next Steps

1. Implement refresh token rotation
2. Add email verification
3. Implement password reset
4. Add two-factor authentication
5. Implement rate limiting
6. Add logging and monitoring
