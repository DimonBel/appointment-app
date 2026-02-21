# Automation Service

AI-powered assistant service for automated booking and communication using Groq LLM.

## Features

- **Conversational Flow Module**: Guides users through booking with natural language
- **Booking Automation Module**: Creates orders automatically via Appointment Service
- **LLM Integration Module**: Connects to Groq's Llama-3.1-8b-instant model
- **Data Collection Module**: Collects preliminary client information

## Modules

- **AutomationApp.Domain**: Domain entities, enums, and interfaces
- **AutomationApp.Repository**: Repository pattern for data access
- **AutomationApp.Service**: Business logic with Groq LLM integration
- **AutomationApp.Postgres**: PostgreSQL database context and repositories
- **AutomationApp.API**: REST API endpoints and SignalR hub for real-time communication

## API Endpoints

- `POST /api/automation/conversations/start` - Start a new AI conversation
- `GET /api/automation/conversations/active` - Get active conversation
- `GET /api/automation/conversations/{id}/messages` - Get conversation messages
- `POST /api/automation/conversations/send` - Send message to AI
- `GET /api/automation/booking/draft/{conversationId}` - Get booking draft
- `POST /api/automation/booking/submit` - Submit booking
- `POST /api/automation/booking/cancel/{draftId}` - Cancel booking draft

## SignalR Hub

- `/automationhub` - Real-time WebSocket for AI conversation updates

## Environment Variables

- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `Groq__ApiKey` - Groq API key for LLM access
- `IdentityService__BaseUrl` - Identity service URL
- `AppointmentService__BaseUrl` - Appointment service URL
- `Jwt__SecretKey` - JWT secret key
- `Jwt__Issuer` - JWT issuer
- `Jwt__Audience` - JWT audience