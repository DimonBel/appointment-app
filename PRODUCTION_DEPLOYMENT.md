# Production Deployment Guide

Complete guide for deploying the Appointment App microservices to production.

## 📋 Pre-Deployment Checklist

### Security

- [ ] Change all default passwords in `.env.docker`
- [ ] Generate new JWT secret key (min 32 characters)
- [ ] Configure HTTPS/SSL certificates
- [ ] Set up firewall rules
- [ ] Enable database encryption
- [ ] Configure backup strategy
- [ ] Set up monitoring and alerting
- [ ] Review and update CORS policies
- [ ] Enable rate limiting
- [ ] Configure CSP headers

### Infrastructure

- [ ] Provision production servers/cloud instances
- [ ] Set up load balancer (if needed)
- [ ] Configure DNS records
- [ ] Set up reverse proxy (Nginx/Traefik)
- [ ] Configure CDN for static assets
- [ ] Set up database replication (optional)
- [ ] Configure persistent volumes

### Application

- [ ] Review and update connection strings
- [ ] Configure proper logging levels
- [ ] Set up centralized logging (ELK/Splunk)
- [ ] Configure application monitoring (APM)
- [ ] Set proper environment variables
- [ ] Optimize database indexes
- [ ] Run database migrations

## 🚀 Deployment Methods

### Method 1: Docker Compose (Single Server)

Best for: Small to medium deployments, single server

```bash
# 1. Clone repository to production server
git clone <your-repo> /opt/appointment-app
cd /opt/appointment-app

# 2. Create production environment file
cp .env.docker .env.production

# 3. Edit production configuration
nano .env.production

# Update these values:
# - POSTGRES_PASSWORD=<strong-password>
# - JWT_SECRET_KEY=<32+ character secret>

# 4. Build and start services
docker-compose --env-file .env.production up -d --build

# 5. Verify all services running
docker-compose ps

# 6. Check logs
docker-compose logs -f
```

### Method 2: Docker Swarm (Multiple Servers)

Best for: High availability, multiple servers

```bash
# 1. Initialize Swarm on manager node
docker swarm init

# 2. Join worker nodes
# (Run on each worker - get command from swarm init output)
docker swarm join --token <token> <manager-ip>:2377

# 3. Create overlay network
docker network create --driver overlay appointment-app-network

# 4. Deploy stack
docker stack deploy -c docker-compose.yml appointment-app

# 5. Scale services
docker service scale appointment-app_appointment-service=3
docker service scale appointment-app_chat-service=3

# 6. Monitor services
docker service ls
docker stack ps appointment-app
```

### Method 3: Kubernetes (Enterprise)

Best for: Large scale, enterprise deployments

See [KUBERNETES_DEPLOYMENT.md](KUBERNETES_DEPLOYMENT.md) for detailed guide.

Quick commands:

```bash
# Apply configurations
kubectl apply -f k8s/

# Check deployment status
kubectl get pods
kubectl get services

# Scale deployment
kubectl scale deployment appointment-service --replicas=5
```

## 🔒 Production Configuration

### 1. Environment Variables

Create `.env.production`:

```env
# Database Configuration (USE STRONG PASSWORDS!)
POSTGRES_USER=produser
POSTGRES_PASSWORD=<generate-strong-password>

# Database Names
IDENTITY_DB_NAME=IdentityDb_Production
APPOINTMENT_DB_NAME=AppointmentDb_Production
CHAT_DB_NAME=ChatDb_Production

# JWT Configuration (GENERATE NEW SECRET!)
JWT_SECRET_KEY=<generate-random-64-character-string>
JWT_ISSUER=AppointmentApp
JWT_AUDIENCE=AppointmentAppClients
JWT_ACCESS_EXPIRATION=30
JWT_REFRESH_EXPIRATION=7

# Logging
ASPNETCORE_ENVIRONMENT=Production
LOGGING_LEVEL=Warning
```

Generate strong JWT secret:

```bash
# Linux/Mac
openssl rand -base64 64

# Windows PowerShell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

### 2. Reverse Proxy with Nginx

Create `/etc/nginx/sites-available/appointment-app`:

```nginx
server {
    listen 80;
    server_name yourdomain.com;

    # Redirect HTTP to HTTPS
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com;

    # SSL Configuration
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # Security Headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "no-referrer-when-downgrade" always;

    # Rate Limiting
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;
    limit_req zone=api_limit burst=20 nodelay;

    # Proxy to Frontend
    location / {
        proxy_pass http://localhost:80;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # API Endpoints
    location /api/ {
        proxy_pass http://localhost:80;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # WebSocket Support
    location /chathub {
        proxy_pass http://localhost:80;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_read_timeout 86400;
    }

    location /orderhub {
        proxy_pass http://localhost:80;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_read_timeout 86400;
    }
}
```

Enable site:

```bash
sudo ln -s /etc/nginx/sites-available/appointment-app /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 3. SSL Certificate with Let's Encrypt

```bash
# Install Certbot
sudo apt update
sudo apt install certbot python3-certbot-nginx

# Obtain certificate
sudo certbot --nginx -d yourdomain.com -d www.yourdomain.com

# Auto-renewal is configured automatically
# Test renewal
sudo certbot renew --dry-run
```

### 4. Database Backup

Create backup script `/opt/scripts/backup-databases.sh`:

```bash
#!/bin/bash
BACKUP_DIR="/opt/backups/appointment-app"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# Backup Identity DB
docker-compose exec -T identity-db pg_dump -U postgres IdentityDb_Production > \
    $BACKUP_DIR/identity_${DATE}.sql

# Backup Appointment DB
docker-compose exec -T appointment-db pg_dump -U postgres AppointmentDb_Production > \
    $BACKUP_DIR/appointment_${DATE}.sql

# Backup Chat DB
docker-compose exec -T chat-db pg_dump -U postgres ChatDb_Production > \
    $BACKUP_DIR/chat_${DATE}.sql

# Compress backups
gzip $BACKUP_DIR/*_${DATE}.sql

# Delete backups older than 30 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete

echo "Backup completed: $DATE"
```

Add to crontab:

```bash
# Daily backup at 2 AM
0 2 * * * /opt/scripts/backup-databases.sh >> /var/log/db-backup.log 2>&1
```

## 📊 Monitoring

### 1. Health Check Monitoring

Create `/opt/scripts/health-check.sh`:

```bash
#!/bin/bash

check_service() {
    local service=$1
    local url=$2

    response=$(curl -s -o /dev/null -w "%{http_code}" $url)

    if [ $response -eq 200 ]; then
        echo "✅ $service is healthy"
    else
        echo "❌ $service is down (HTTP $response)"
        # Send alert (email, Slack, etc.)
    fi
}

check_service "Identity" "http://localhost:5005/health"
check_service "Appointment" "http://localhost:5001/health"
check_service "Chat" "http://localhost:5002/health"
check_service "Frontend" "http://localhost/health"
```

Add to crontab:

```bash
# Check every 5 minutes
*/5 * * * * /opt/scripts/health-check.sh
```

### 2. Prometheus + Grafana (Optional)

Add to docker-compose.yml:

```yaml
prometheus:
  image: prom/prometheus:latest
  volumes:
    - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
    - prometheus-data:/prometheus
  ports:
    - "9090:9090"
  networks:
    - app-network

grafana:
  image: grafana/grafana:latest
  ports:
    - "3000:3000"
  environment:
    - GF_SECURITY_ADMIN_PASSWORD=admin
  volumes:
    - grafana-data:/var/lib/grafana
  networks:
    - app-network
```

## 🔄 CI/CD Pipeline

### GitHub Actions

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Production

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Deploy to Server
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.PROD_HOST }}
          username: ${{ secrets.PROD_USER }}
          key: ${{ secrets.PROD_SSH_KEY }}
          script: |
            cd /opt/appointment-app
            git pull origin main
            docker-compose --env-file .env.production up -d --build
            docker-compose ps
```

## 📈 Scaling Strategies

### Horizontal Scaling

```bash
# Scale appointment service to 3 instances
docker-compose up -d --scale appointment-service=3

# With Docker Swarm
docker service scale appointment-app_appointment-service=5
```

### Database Connection Pooling

Update connection strings in production:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Port=5432;Database=AppDb;Username=user;Password=pass;Pooling=true;MinPoolSize=5;MaxPoolSize=100;"
  }
}
```

### Load Balancer

Use Nginx upstream:

```nginx
upstream appointment_backend {
    least_conn;
    server appointment-service-1:5001;
    server appointment-service-2:5001;
    server appointment-service-3:5001;
}

location /api/appointment/ {
    proxy_pass http://appointment_backend;
}
```

## 🛡️ Security Hardening

### 1. Firewall Configuration

```bash
# Allow only necessary ports
sudo ufw allow 22/tcp   # SSH
sudo ufw allow 80/tcp   # HTTP
sudo ufw allow 443/tcp  # HTTPS
sudo ufw enable
```

### 2. Docker Security

Update docker-compose.yml:

```yaml
services:
  identity-service:
    security_opt:
      - no-new-privileges:true
    read_only: true
    tmpfs:
      - /tmp
    deploy:
      resources:
        limits:
          cpus: "1"
          memory: 512M
```

### 3. Environment Secrets

Use Docker Secrets (Swarm mode):

```bash
# Create secrets
echo "super-secret-password" | docker secret create db_password -

# Use in compose
services:
  identity-db:
    secrets:
      - db_password
    environment:
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
```

## 🔧 Performance Optimization

### 1. Database Optimization

```sql
-- Create indexes on frequently queried columns
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_appointments_date ON appointments(appointment_date);
CREATE INDEX idx_messages_chat_id ON messages(chat_id);

-- Vacuum and analyze
VACUUM ANALYZE;
```

### 2. Enable Output Caching (.NET)

Update Program.cs:

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Cache());
    options.AddPolicy("expire-5min", builder => builder.Expire(TimeSpan.FromMinutes(5)));
});

app.UseOutputCache();
```

### 3. Redis Cache (Optional)

Add to docker-compose.yml:

```yaml
redis:
  image: redis:alpine
  ports:
    - "6379:6379"
  volumes:
    - redis-data:/data
  networks:
    - app-network
```

## 📝 Post-Deployment

### Verification Steps

```bash
# 1. Check all services running
docker-compose ps

# 2. Test health endpoints
curl https://yourdomain.com/api/auth/health
curl https://yourdomain.com/api/appointment/health
curl https://yourdomain.com/api/chat/health

# 3. Test user registration
curl -X POST https://yourdomain.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test123"}'

# 4. Test login
curl -X POST https://yourdomain.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test123"}'

# 5. Monitor logs
docker-compose logs -f --tail=100
```

### Load Testing

```bash
# Install Apache Bench
sudo apt install apache2-utils

# Test endpoint
ab -n 1000 -c 10 https://yourdomain.com/api/auth/health

# Or use k6
k6 run load-test.js
```

## 🆘 Rollback Plan

```bash
# 1. Stop current deployment
docker-compose down

# 2. Checkout previous version
git checkout <previous-commit-hash>

# 3. Restart services
docker-compose up -d --build

# 4. Verify
docker-compose ps
```

## 📞 Support Contacts

- **DevOps Team**: devops@company.com
- **DBA Team**: dba@company.com
- **On-Call**: +1-XXX-XXX-XXXX

---

**Last Updated**: February 2026  
**Reviewed By**: DevOps Team  
**Next Review**: March 2026
