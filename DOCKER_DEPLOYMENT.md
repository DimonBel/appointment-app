# 🐳 Docker Deployment Guide

Complete Docker containerization for the Appointment App microservices architecture.

## 📋 Overview

This Docker Compose setup includes:

- **Frontend** - React application served via Nginx
- **Identity Service** - Authentication & Authorization (.NET 9)
- **Appointment Service** - Appointment management (.NET 9)
- **Chat Service** - Real-time messaging (.NET 9)
- **3 PostgreSQL Databases** - Separate databases for each service

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         Frontend (Nginx)                    │
│                     http://localhost:80                     │
└──────────────┬──────────────┬──────────────┬───────────────┘
               │              │              │
       ┌───────▼──────┐ ┌────▼─────┐ ┌──────▼────────┐
       │  Identity    │ │Appointment│ │     Chat      │
       │   Service    │ │  Service  │ │   Service     │
       │  :5005       │ │   :5001   │ │    :5002      │
       └───────┬──────┘ └────┬─────┘ └──────┬────────┘
               │              │              │
       ┌───────▼──────┐ ┌────▼─────┐ ┌──────▼────────┐
       │ Identity DB  │ │Appointment│ │   Chat DB     │
       │  :5432       │ │    DB     │ │    :5434      │
       │              │ │  :5433    │ │               │
       └──────────────┘ └──────────┘ └───────────────┘
```

## 🚀 Quick Start

### Prerequisites

- Docker Desktop 4.0+ or Docker Engine 20.10+
- Docker Compose 2.0+
- At least 4GB free RAM
- 10GB free disk space

### 1. Build and Start All Services

```bash
# Navigate to project root
cd d:\Duma\appointment-app

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f identity-service
docker-compose logs -f appointment-service
docker-compose logs -f chat-service
docker-compose logs -f frontend
```

### 2. Initialize Databases

The databases will be created automatically, but you need to apply migrations:

```bash
# Run migrations for Identity Service
docker-compose exec identity-service dotnet ef database update

# Run migrations for Appointment Service
docker-compose exec appointment-service dotnet ef database update

# Run migrations for Chat Service
docker-compose exec chat-service dotnet ef database update
```

### 3. Access the Application

- **Frontend**: http://localhost:80
- **Identity API**: http://localhost:5005
- **Appointment API**: http://localhost:5001
- **Chat API**: http://localhost:5002

## 📦 Services Details

### Frontend

- **Image**: Built from `Frontend/Dockerfile`
- **Base**: Node 20 → Nginx Alpine
- **Port**: 80
- **Features**:
  - Multi-stage build (optimized size)
  - Nginx with API proxying
  - SignalR WebSocket support
  - Static asset caching

### Identity Service

- **Image**: Built from `Identity/Dockerfile`
- **Base**: .NET 9 SDK → .NET 9 Runtime
- **Port**: 5005
- **Database**: PostgreSQL (identity-db:5432)
- **Features**:
  - JWT authentication
  - User registration & login
  - Token refresh mechanism

### Appointment Service

- **Image**: Built from `Appointment/Dockerfile`
- **Base**: .NET 9 SDK → .NET 9 Runtime
- **Port**: 5001
- **Database**: PostgreSQL (appointment-db:5433)
- **Features**:
  - Appointment management
  - SignalR for real-time updates
  - Integration with Identity Service

### Chat Service

- **Image**: Built from `ChatApp/Dockerfile`
- **Base**: .NET 9 SDK → .NET 9 Runtime
- **Port**: 5002
- **Database**: PostgreSQL (chat-db:5434)
- **Features**:
  - Real-time messaging with SignalR
  - Chat history
  - Integration with Identity Service

## 🗄️ Database Management

### Access PostgreSQL Databases

```bash
# Identity Database
docker-compose exec identity-db psql -U postgres -d IdentityDb

# Appointment Database
docker-compose exec appointment-db psql -U postgres -d appointmentdb

# Chat Database
docker-compose exec chat-db psql -U postgres -d chatappdb
```

### Backup Databases

```bash
# Backup Identity DB
docker-compose exec identity-db pg_dump -U postgres IdentityDb > identity_backup.sql

# Backup Appointment DB
docker-compose exec appointment-db pg_dump -U postgres appointmentdb > appointment_backup.sql

# Backup Chat DB
docker-compose exec chat-db pg_dump -U postgres chatappdb > chat_backup.sql
```

### Restore Databases

```bash
# Restore Identity DB
docker-compose exec -T identity-db psql -U postgres IdentityDb < identity_backup.sql

# Restore Appointment DB
docker-compose exec -T appointment-db psql -U postgres appointmentdb < appointment_backup.sql

# Restore Chat DB
docker-compose exec -T chat-db psql -U postgres chatappdb < chat_backup.sql
```

## 🔧 Configuration

### Environment Variables

Edit `.env.docker` file to customize configuration:

```env
# Database credentials
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your-secure-password

# JWT settings
JWT_SECRET_KEY=your-super-secret-key-at-least-32-characters
JWT_ISSUER=IdentityApp
JWT_AUDIENCE=IdentityAppClients
```

### Using Custom Environment File

```bash
# Use custom env file
docker-compose --env-file .env.production up -d
```

## 🔍 Monitoring & Health Checks

### Check Service Health

```bash
# Check all services status
docker-compose ps

# Check specific service health (from inside container)
docker-compose exec identity-service wget -q -O- http://localhost:5005/health
docker-compose exec appointment-service wget -q -O- http://localhost:5001/health
docker-compose exec chat-service wget -q -O- http://localhost:5002/health

# Or check from your host machine (if you have curl installed)
curl http://localhost:5005/health
curl http://localhost:5001/health
curl http://localhost:5002/health
```

### View Resource Usage

```bash
# CPU and Memory usage
docker stats

# Specific containers
docker stats identity-service appointment-service chat-service
```

## 🛠️ Common Commands

### Build & Deploy

```bash
# Build without cache
docker-compose build --no-cache

# Build specific service
docker-compose build identity-service

# Rebuild and restart
docker-compose up -d --build

# Scale services (if needed)
docker-compose up -d --scale appointment-service=2
```

### Manage Services

```bash
# Stop all services
docker-compose stop

# Start all services
docker-compose start

# Restart specific service
docker-compose restart identity-service

# Remove containers (keep volumes)
docker-compose down

# Remove containers and volumes
docker-compose down -v

# Remove everything including images
docker-compose down -v --rmi all
```

### Debugging

```bash
# Enter container shell
docker-compose exec identity-service bash
docker-compose exec appointment-service bash
docker-compose exec chat-service bash

# View real-time logs
docker-compose logs -f --tail=100

# Export logs to file
docker-compose logs > docker-logs.txt
```

## 🔄 Development Workflow

### Hot Reload Development

For development with hot reload, use development compose file:

```bash
# Create docker-compose.dev.yml
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
```

### Volume Mounting for Development

Add volume mounts in docker-compose.dev.yml:

```yaml
services:
  frontend:
    volumes:
      - ./Frontend/src:/app/src
  identity-service:
    volumes:
      - ./Identity:/src
```

## 📊 Performance Optimization

### Multi-Stage Builds

All Dockerfiles use multi-stage builds to minimize final image size:

- **Build stage**: Full SDK with all tools
- **Runtime stage**: Minimal runtime only

### Image Sizes (Approximate)

- Frontend: ~50MB (Nginx Alpine)
- .NET Services: ~220MB each (ASP.NET Runtime)
- PostgreSQL: ~240MB (Alpine)

### Reduce Build Time

```bash
# Use BuildKit for faster builds
DOCKER_BUILDKIT=1 docker-compose build

# Parallel builds
docker-compose build --parallel
```

## 🔒 Security Best Practices

### Production Recommendations

1. **Change default passwords** in `.env.docker`
2. **Use secrets management** instead of plain env variables
3. **Enable HTTPS** with reverse proxy (Nginx/Traefik)
4. **Limit exposed ports** - only expose frontend (80/443)
5. **Use read-only filesystems** where possible
6. **Scan images** for vulnerabilities:

```bash
# Scan images with Trivy
trivy image identity-service
trivy image appointment-service
trivy image chat-service
```

### Network Isolation

All services communicate via internal `app-network`. Only frontend port 80 should be exposed to public.

## 🚨 Troubleshooting

### Common Issues

**1. Database connection errors**

```bash
# Check if database is ready
docker-compose logs identity-db | grep "ready to accept connections"

# Restart database
docker-compose restart identity-db
```

**2. Port already in use**

```bash
# Find process using port
netstat -ano | findstr :80
netstat -ano | findstr :5005

# Kill process or change port in docker-compose.yml
```

**3. Build failures**

```bash
# Clean Docker cache
docker system prune -a --volumes

# Rebuild from scratch
docker-compose build --no-cache
```

**4. Frontend can't connect to backend**

```bash
# Check nginx logs
docker-compose logs frontend

# Verify backend services are running
docker-compose ps
```

**5. SignalR WebSocket errors**

```bash
# Ensure nginx.conf has WebSocket upgrade headers
# Check proxy_set_header Upgrade and Connection settings
```

## 📈 Scaling

### Horizontal Scaling

```bash
# Scale appointment service
docker-compose up -d --scale appointment-service=3

# With load balancer
# Add nginx upstream configuration in docker-compose.yml
```

### Database Replication

For production, consider PostgreSQL replication:

- Master-Slave setup
- Connection pooling (PgBouncer)
- Separate read/write connections

## 🔄 CI/CD Integration

### GitHub Actions Example

```yaml
name: Deploy with Docker

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Build and Deploy
        run: |
          docker-compose build
          docker-compose up -d
```

## 📝 Maintenance

### Regular Tasks

```bash
# Update images
docker-compose pull

# Clean unused resources
docker system prune -a --volumes

# Check disk usage
docker system df

# Backup volumes
docker run --rm -v identity-db-data:/data -v $(pwd):/backup alpine tar czf /backup/identity-db-backup.tar.gz /data
```

## 🎯 Next Steps

1. ✅ All services containerized
2. 🔄 Setup CI/CD pipeline
3. 🔒 Add HTTPS with Let's Encrypt
4. 📊 Add monitoring (Prometheus + Grafana)
5. 📝 Add centralized logging (ELK stack)
6. 🔄 Setup orchestration (Kubernetes/Docker Swarm)

## 📚 Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [PostgreSQL Docker Hub](https://hub.docker.com/_/postgres)
- [Nginx Documentation](https://nginx.org/en/docs/)

## 💡 Support

For issues or questions:

1. Check logs: `docker-compose logs`
2. Verify health: `docker-compose ps`
3. Review this documentation
4. Check individual service READMEs

---

**Created**: February 2026  
**Docker Compose Version**: 3.8  
**Docker Engine**: 20.10+
