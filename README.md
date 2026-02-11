# ChatApp - ASP.NET Core Clean Architecture Chat Application

A secure, real-time chat application built with ASP.NET Core 9.0, following Clean Architecture principles.

## Features

- **User Authentication**: Microsoft Identity with cookie-based authentication
- **Real-time Messaging**: SignalR for instant message delivery
- **User Management**: User registration, login, and online status tracking
- **Clean Architecture**: Separated layers (Domain, Application, Infrastructure, API)
- **PostgreSQL Database**: Entity Framework Core with PostgreSQL provider
- **Security**: Cookie-based authentication with secure password hashing

## Architecture

The solution follows Clean Architecture principles with four main layers:

### 1. Domain Layer (`ChatApp.Domain`)
- Contains core business entities (User, ChatMessage)
- Defines interfaces for repositories and services
- No external dependencies

### 2. Application Layer (`ChatApp.Application`)
- Contains DTOs for data transfer
- Implements business logic services
- Defines service interfaces

### 3. Infrastructure Layer (`ChatApp.Infrastructure`)
- Implements repositories using Entity Framework Core
- Handles database context and migrations
- Integrates with Microsoft Identity

### 4. API Layer (`ChatApp.API`)
- RESTful API controllers
- SignalR Hub for real-time communication
- Authentication and authorization configuration
- Dependency injection setup

## Setup Instructions

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL database server
- Your preferred IDE (Visual Studio, VS Code, etc.)

### Database Configuration

1. Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=chatappdb;Username=postgres;Password=your_password"
  }
}
```

2. Create the database:
```bash
createdb chatappdb
```

3. Apply migrations and create the database schema:
```bash
dotnet ef migrations add InitialCreate --project ChatApp.Infrastructure --startup-project ChatApp.API
dotnet ef database update --project ChatApp.Infrastructure --startup-project ChatApp.API
```

### Running the Application

1. Build the solution:
```bash
dotnet build ChatApp.sln
```

2. Run the API:
```bash
cd ChatApp.API
dotnet run
```

The API will be available at `https://localhost:5001`

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/logout` - Logout user (requires authentication)
- `GET /api/auth/current` - Get current user (requires authentication)

### Chat
- `GET /api/chat/users` - Get all users (requires authentication)
- `GET /api/chat/users/{id}` - Get user by ID (requires authentication)
- `GET /api/chat/messages/{userId}` - Get messages between current user and specified user (requires authentication)
- `POST /api/chat/messages` - Send a message (requires authentication)
- `GET /api/chat/messages/recent` - Get recent messages (requires authentication)

### SignalR Hub
- Hub endpoint: `/chathub`
- Events:
  - `SendMessage(receiverId, message)` - Send message to user
  - `ReceiveMessage(senderId, message)` - Receive message from user
  - `SendTypingNotification(receiverId)` - Send typing notification
  - `UserTyping(userId)` - Receive typing notification
  - `UserOnline(userId)` - User came online
  - `UserOffline(userId)` - User went offline

## Technology Stack

- **.NET 9.0** - Latest .NET framework
- **ASP.NET Core** - Web framework
- **Entity Framework Core 9.0** - ORM
- **Npgsql** - PostgreSQL provider
- **Microsoft Identity** - User authentication and authorization
- **SignalR** - Real-time web functionality
- **Clean Architecture** - Architectural pattern

## Security Features

- Password hashing with ASP.NET Core Identity
- Cookie-based authentication with secure flags
- HTTPS redirection in production
- Authorization policies for protected endpoints
- Password requirements: minimum 6 characters, must include uppercase, lowercase, digit, and special character

## Project Structure

```
ChatApp/
├── ChatApp.Domain/           # Core domain entities and interfaces
├── ChatApp.Application/      # Business logic and DTOs
├── ChatApp.Infrastructure/   # Data access and external services
├── ChatApp.API/             # Web API and SignalR hub
└── ChatApp.sln              # Solution file
```

## Development Notes

- All API endpoints require authentication unless specified
- SignalR connections require authenticated users
- Online status is tracked and updated on connect/disconnect
- Messages are paginated (default 50 per page)
- CORS is configured to allow all origins (configure appropriately for production)