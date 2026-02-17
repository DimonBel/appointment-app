# Notification Service

Centralized notification service for the Medical Appointment Platform.

## Architecture Modules

| Module                       | Description                                                | Status |
| ---------------------------- | ---------------------------------------------------------- | ------ |
| 2.1 Notification Preferences | Users configure which notifications to receive per channel | ✅     |
| 2.2 Notification Delivery    | Sends notifications triggered by system events             | ✅     |
| 2.3 Notification Schedule    | Custom notification timing (reminders, delays)             | ✅     |
| 2.4 Template Management      | Manages notification templates with dynamic placeholders   | ✅     |
| 2.5 Event Listener           | Listens to events from other microservices                 | ✅     |

## API Endpoints

### Notifications

- `GET /api/notifications?userId={userId}&page=1&pageSize=20` — Get user notifications
- `GET /api/notifications/{id}` — Get notification by ID
- `GET /api/notifications/unread-count?userId={userId}` — Get unread count
- `POST /api/notifications` — Create/send notification
- `PUT /api/notifications/{id}/read` — Mark as read
- `PUT /api/notifications/read-all?userId={userId}` — Mark all as read
- `DELETE /api/notifications/{id}` — Delete notification

### Preferences

- `GET /api/notifications/preferences?userId={userId}` — Get preferences
- `PUT /api/notifications/preferences?userId={userId}` — Update preference
- `POST /api/notifications/preferences/defaults?userId={userId}` — Set defaults

### Templates

- `GET /api/notifications/templates` — List all templates
- `GET /api/notifications/templates/{id}` — Get template
- `POST /api/notifications/templates` — Create template
- `PUT /api/notifications/templates/{id}` — Update template
- `DELETE /api/notifications/templates/{id}` — Delete template

### Schedules

- `GET /api/notifications/schedules?userId={userId}` — Get schedules
- `POST /api/notifications/schedules` — Create schedule
- `POST /api/notifications/schedules/reminder` — Schedule appointment reminder
- `PUT /api/notifications/schedules/{id}/cancel` — Cancel schedule
- `POST /api/notifications/schedules/process` — Process pending schedules

### Events (Inter-service communication)

- `POST /api/notifications/events` — Receive event from microservice
- `GET /api/notifications/events/unprocessed` — Get unprocessed events
- `GET /api/notifications/events/failed` — Get failed events
- `POST /api/notifications/events/{id}/retry` — Retry failed event

## Running Locally

```bash
cd Notification
dotnet run --project NotificationApp.API
```

Port: **5003**

## SignalR Hub

Connect to `/notificationhub` for real-time notification delivery.

## Health Check

`GET /health`
