# 🚀 Quick Start with Docker

## Prerequisites

- Docker Desktop installed and running
- At least 4GB RAM available
- 10GB free disk space

## Start Everything

### Step 1: Navigate to project root

```bash
cd d:\Duma\appointment-app
```

### Step 2: Build and start all services

```bash
docker-compose up -d --build
```

This will start:

- ✅ 3 PostgreSQL databases
- ✅ Identity Service (port 5005)
- ✅ Appointment Service (port 5001)
- ✅ Chat Service (port 5002)
- ✅ Frontend (port 80)

### Step 3: Check status

```bash
docker-compose ps
```

All services should show "Up" status.

### Step 4: View logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f identity-service
docker-compose logs -f appointment-service
docker-compose logs -f chat-service
docker-compose logs -f frontend
```

### Step 5: Access the application

Open your browser: **http://localhost**

## Service URLs

| Service             | URL                   |
| ------------------- | --------------------- |
| **Frontend**        | http://localhost      |
| **Identity API**    | http://localhost:5005 |
| **Appointment API** | http://localhost:5001 |
| **Chat API**        | http://localhost:5002 |
| **Identity DB**     | localhost:5432        |
| **Appointment DB**  | localhost:5433        |
| **Chat DB**         | localhost:5434        |

## Health Checks

```bash
# Check from your host machine
curl http://localhost:5005/health
curl http://localhost:5001/health
curl http://localhost:5002/health
curl http://localhost

# Or using PowerShell on Windows
Invoke-WebRequest http://localhost:5005/health
Invoke-WebRequest http://localhost:5001/health
Invoke-WebRequest http://localhost:5002/health
```

## Stop/Restart

```bash
# Stop all services
docker-compose stop

# Start all services
docker-compose start

# Restart specific service
docker-compose restart identity-service

# Stop and remove containers
docker-compose down

# Stop and remove containers + volumes (DELETES DATABASE DATA!)
docker-compose down -v
```

## Database Migrations

After first start, migrations should apply automatically. If issues occur:

```bash
# Check if migrations need to run
docker-compose exec identity-service dotnet ef database update
docker-compose exec appointment-service dotnet ef database update
docker-compose exec chat-service dotnet ef database update
```

## Troubleshooting

### Port already in use

```bash
# Windows - Find and kill process
netstat -ano | findstr :80
netstat -ano | findstr :5005
taskkill /PID <PID> /F
```

### Can't connect to database

```bash
# Restart database
docker-compose restart identity-db

# Check database logs
docker-compose logs identity-db
```

### Services not responding

```bash
# Check if containers are running
docker-compose ps

# Rebuild specific service
docker-compose up -d --build identity-service

# View service logs for errors
docker-compose logs identity-service
```

### Clear everything and start fresh

```bash
# WARNING: This deletes ALL data
docker-compose down -v
docker system prune -a --volumes
docker-compose up -d --build
```

## Development

### Hot reload (NOT enabled by default in production mode)

For development with hot reload, create `docker-compose.dev.yml`

### View container shell

```bash
docker-compose exec identity-service bash
docker-compose exec appointment-service bash
docker-compose exec chat-service bash
```

### Database access

```bash
# Connect to PostgreSQL
docker-compose exec identity-db psql -U postgres -d IdentityDb
docker-compose exec appointment-db psql -U postgres -d appointmentdb
docker-compose exec chat-db psql -U postgres -d chatappdb
```

## Configuration

Edit `.env.docker` to change:

- Database credentials
- JWT secret keys
- Database names

**Note**: After changing `.env.docker`, restart services:

```bash
docker-compose down
docker-compose up -d
```

## Next Steps

1. ✅ Open http://localhost in browser
2. ✅ Register a new account
3. ✅ Login and test appointments
4. ✅ Test real-time chat

## Full Documentation

See [DOCKER_DEPLOYMENT.md](DOCKER_DEPLOYMENT.md) for complete documentation.

---

**Need help?** Check logs with `docker-compose logs -f`
