# Identity Microservice

A centralized authentication and authorization microservice for the appointment and chat applications.

## Overview

This Identity microservice provides JWT-based authentication and user management functionality that can be consumed by other microservices (Appointment and ChatApp) via REST API.

## Features

- User registration and authentication
- JWT token generation and validation
- Refresh token mechanism
- Role-based authorization (Admin, User, Professional)
- User profile management
- Centralized user database

## Architecture

The project follows Clean Architecture principles with the following layers:

- **IdentityApp.API** - REST API endpoints and configuration
- **IdentityApp.Service** - Business logic and application services
- **IdentityApp.Domain** - Domain entities, DTOs, and interfaces
- **IdentityApp.Repository** - Repository interfaces
- **IdentityApp.Postgres** - Data access implementation with Entity Framework Core

## Technologies

- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core 9.0
- PostgreSQL
- JWT Bearer Authentication
- ASP.NET Core Identity

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL 13+

### Configuration

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=IdentityDb;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForIdentityService!",
    "Issuer": "IdentityApp",
    "Audience": "IdentityAppClients",
    "AccessTokenExpirationMinutes": "30",
    "RefreshTokenExpirationDays": "7"
  }
}
```

### Database Setup

1. Create the database migrations:

```bash
cd IdentityApp.API
dotnet ef migrations add InitialCreate --project ../IdentityApp.Postgres
```

2. Apply migrations to create the database:

```bash
dotnet ef database update --project ../IdentityApp.Postgres
```

### Running the Service

```bash
cd IdentityApp.API
dotnet run
```

The API will be available at `https://localhost:5001` (or the configured port).

## API Endpoints

### Authentication

- **POST** `/api/auth/register` - Register a new user

  ```json
  {
    "email": "user@example.com",
    "password": "Password123!",
    "userName": "username",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+1234567890"
  }
  ```

- **POST** `/api/auth/login` - Login and get access token

  ```json
  {
    "email": "user@example.com",
    "password": "Password123!"
  }
  ```

- **POST** `/api/auth/refresh` - Refresh access token

  ```json
  {
    "accessToken": "expired_access_token",
    "refreshToken": "valid_refresh_token"
  }
  ```

- **POST** `/api/auth/revoke/{userId}` - Revoke all user tokens (requires authentication)

- **POST** `/api/auth/validate` - Validate a token

### User Management (Requires Authentication)

- **GET** `/api/users/{userId}` - Get user by ID
- **GET** `/api/users/email/{email}` - Get user by email
- **GET** `/api/users/username/{username}` - Get user by username
- **GET** `/api/users` - Get all users
- **PUT** `/api/users/{userId}` - Update user
- **DELETE** `/api/users/{userId}` - Delete user
- **PATCH** `/api/users/{userId}/online-status` - Update user online status

### Health Check

- **GET** `/health` - Service health check

## Integration with Other Services

### AppointmentApp Integration

To integrate the Identity service with AppointmentApp:

1. Update AppointmentApp to call Identity service for authentication instead of using local Identity
2. Modify the authentication to validate JWT tokens issued by Identity service
3. Add HTTP client to communicate with Identity API

Example configuration in AppointmentApp `appsettings.json`:

```json
{
  "IdentityService": {
    "BaseUrl": "https://localhost:5001",
    "ValidateIssuer": true,
    "Issuer": "IdentityApp",
    "Audience": "IdentityAppClients"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForIdentityService!"
  }
}
```

### ChatApp Integration

Similar integration steps for ChatApp:

1. Replace local authentication with Identity service calls
2. Use JWT tokens from Identity service
3. Validate tokens using the same secret key

## Default Roles

The system comes with three predefined roles:

- **Admin** - Full system access
- **User** - Regular user access
- **Professional** - Service provider access (for appointment booking)

## Security Notes

1. **Change the JWT SecretKey** in production - use a strong, random secret key
2. Store sensitive configuration in environment variables or Azure Key Vault
3. Use HTTPS in production
4. Implement rate limiting for authentication endpoints
5. Add refresh token rotation for enhanced security

## Response Format

### Successful Authentication Response

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
    "avatarUrl": null,
    "phoneNumber": "+1234567890",
    "isActive": true,
    "isOnline": true,
    "createdAt": "2026-02-14T10:00:00Z",
    "lastLoginAt": "2026-02-14T11:00:00Z",
    "roles": ["User"]
  }
}
```

## Development

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

## Future Enhancements

- [ ] Add OAuth2/OpenID Connect support
- [ ] Implement two-factor authentication (2FA)
- [ ] Add email verification
- [ ] Implement password reset functionality
- [ ] Add audit logging
- [ ] Implement account lockout policies
- [ ] Add support for external authentication providers (Google, Microsoft, etc.)

## License

This project is part of the microservices architecture for appointment and chat applications.
