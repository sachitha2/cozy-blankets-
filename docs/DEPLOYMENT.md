# Cozy Comfort - Deployment Documentation

This document describes various deployment techniques suitable for the Cozy Comfort Service-Oriented Architecture system.

## Table of Contents
1. [Deployment Overview](#deployment-overview)
2. [Traditional Server Deployment](#traditional-server-deployment)
3. [Docker Deployment](#docker-deployment)
4. [Kubernetes Deployment](#kubernetes-deployment)
5. [Cloud Platform Deployment](#cloud-platform-deployment)
6. [Comparison of Deployment Methods](#comparison-of-deployment-methods)

---

## Deployment Overview

The Cozy Comfort system consists of:
- **3 Microservices**: ManufacturerService, DistributorService, SellerService
- **1 Web Client**: ClientAppWeb
- **3 Databases**: SQLite (can be migrated to PostgreSQL/SQL Server)
- **Inter-service Communication**: HTTP REST APIs

Each service can be deployed independently, allowing for flexible scaling and maintenance.

---

## Traditional Server Deployment

### Prerequisites
- Windows Server 2019+ or Linux (Ubuntu 20.04+)
- .NET 7.0 Runtime installed
- IIS (Windows) or Nginx (Linux) for reverse proxy
- SQLite (or PostgreSQL/SQL Server for production)

### Windows Server Deployment

#### Step 1: Install .NET Runtime
```powershell
# Download and install .NET 7.0 Runtime
# https://dotnet.microsoft.com/download/dotnet/7.0
```

#### Step 2: Publish Applications
```powershell
# Publish each service
cd ManufacturerService
dotnet publish -c Release -o C:\Services\ManufacturerService

cd ..\DistributorService
dotnet publish -c Release -o C:\Services\DistributorService

cd ..\SellerService
dotnet publish -c Release -o C:\Services\SellerService

cd ..\ClientAppWeb
dotnet publish -c Release -o C:\Services\ClientAppWeb
```

#### Step 3: Configure IIS
1. Install IIS with ASP.NET Core Module
2. Create Application Pools for each service
3. Create websites pointing to published folders
4. Configure URL Rewrite rules for reverse proxy

#### Step 4: Configure Windows Services (Alternative)
Use NSSM (Non-Sucking Service Manager) to run as Windows Services:
```powershell
nssm install ManufacturerService "C:\Program Files\dotnet\dotnet.exe" "C:\Services\ManufacturerService\ManufacturerService.dll"
nssm set ManufacturerService AppDirectory "C:\Services\ManufacturerService"
nssm start ManufacturerService
```

### Linux Server Deployment

#### Step 1: Install .NET Runtime
```bash
# Ubuntu/Debian
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 7.0.0

# Add to PATH
export PATH=$PATH:$HOME/.dotnet
```

#### Step 2: Publish Applications
```bash
cd ManufacturerService
dotnet publish -c Release -o /opt/services/ManufacturerService

cd ../DistributorService
dotnet publish -c Release -o /opt/services/DistributorService

cd ../SellerService
dotnet publish -c Release -o /opt/services/SellerService

cd ../ClientAppWeb
dotnet publish -c Release -o /opt/services/ClientAppWeb
```

#### Step 3: Configure Systemd Services
Create service files for each service:

**/etc/systemd/system/manufacturer-service.service**
```ini
[Unit]
Description=Cozy Comfort Manufacturer Service
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /opt/services/ManufacturerService/ManufacturerService.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5001

[Install]
WantedBy=multi-user.target
```

Enable and start services:
```bash
sudo systemctl enable manufacturer-service
sudo systemctl start manufacturer-service
sudo systemctl status manufacturer-service
```

#### Step 4: Configure Nginx Reverse Proxy
**/etc/nginx/sites-available/cozy-comfort**
```nginx
upstream manufacturer {
    server localhost:5001;
}

upstream distributor {
    server localhost:5002;
}

upstream seller {
    server localhost:5003;
}

upstream webclient {
    server localhost:5006;
}

server {
    listen 80;
    server_name api.cozycomfort.com;

    location /manufacturer/ {
        proxy_pass http://manufacturer/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }

    location /distributor/ {
        proxy_pass http://distributor/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }

    location /seller/ {
        proxy_pass http://seller/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}

server {
    listen 80;
    server_name cozycomfort.com;

    location / {
        proxy_pass http://webclient;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

---

## Docker Deployment

### Prerequisites
- Docker Engine 20.10+
- Docker Compose 2.0+

### Dockerfile for Services

**ManufacturerService/Dockerfile**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ManufacturerService/ManufacturerService.csproj", "ManufacturerService/"]
RUN dotnet restore "ManufacturerService/ManufacturerService.csproj"
COPY . .
WORKDIR "/src/ManufacturerService"
RUN dotnet build "ManufacturerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ManufacturerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ManufacturerService.dll"]
```

### Docker Compose Configuration

**docker-compose.yml**
```yaml
version: '3.8'

services:
  manufacturer-service:
    build:
      context: .
      dockerfile: ManufacturerService/Dockerfile
    container_name: manufacturer-service
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5001
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/ManufacturerServiceDb.db
    volumes:
      - manufacturer-data:/app/data
    networks:
      - cozy-comfort-network
    restart: unless-stopped

  distributor-service:
    build:
      context: .
      dockerfile: DistributorService/Dockerfile
    container_name: distributor-service
    ports:
      - "5002:5002"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5002
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/DistributorServiceDb.db
      - ManufacturerService__BaseUrl=http://manufacturer-service:5001
    volumes:
      - distributor-data:/app/data
    networks:
      - cozy-comfort-network
    depends_on:
      - manufacturer-service
    restart: unless-stopped

  seller-service:
    build:
      context: .
      dockerfile: SellerService/Dockerfile
    container_name: seller-service
    ports:
      - "5003:5003"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5003
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/SellerServiceDb.db
      - DistributorService__BaseUrl=http://distributor-service:5002
    volumes:
      - seller-data:/app/data
    networks:
      - cozy-comfort-network
    depends_on:
      - distributor-service
    restart: unless-stopped

  client-app-web:
    build:
      context: .
      dockerfile: ClientAppWeb/Dockerfile
    container_name: client-app-web
    ports:
      - "5006:5006"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5006
    networks:
      - cozy-comfort-network
    depends_on:
      - seller-service
    restart: unless-stopped

volumes:
  manufacturer-data:
  distributor-data:
  seller-data:

networks:
  cozy-comfort-network:
    driver: bridge
```

### Deploy with Docker Compose

```bash
# Build and start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

---

## Kubernetes Deployment

### Prerequisites
- Kubernetes cluster (1.24+)
- kubectl configured
- Docker images pushed to container registry

### Kubernetes Manifests

**manufacturer-service-deployment.yaml**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: manufacturer-service
  labels:
    app: manufacturer-service
spec:
  replicas: 2
  selector:
    matchLabels:
      app: manufacturer-service
  template:
    metadata:
      labels:
        app: manufacturer-service
    spec:
      containers:
      - name: manufacturer-service
        image: cozycomfort/manufacturer-service:latest
        ports:
        - containerPort: 5001
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:5001"
        volumeMounts:
        - name: data
          mountPath: /app/data
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: manufacturer-data-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: manufacturer-service
spec:
  selector:
    app: manufacturer-service
  ports:
  - port: 5001
    targetPort: 5001
  type: ClusterIP
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: manufacturer-data-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 1Gi
```

**distributor-service-deployment.yaml**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: distributor-service
  labels:
    app: distributor-service
spec:
  replicas: 2
  selector:
    matchLabels:
      app: distributor-service
  template:
    metadata:
      labels:
        app: distributor-service
    spec:
      containers:
      - name: distributor-service
        image: cozycomfort/distributor-service:latest
        ports:
        - containerPort: 5002
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:5002"
        - name: ManufacturerService__BaseUrl
          value: "http://manufacturer-service:5001"
        volumeMounts:
        - name: data
          mountPath: /app/data
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: distributor-data-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: distributor-service
spec:
  selector:
    app: distributor-service
  ports:
  - port: 5002
    targetPort: 5002
  type: ClusterIP
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: distributor-data-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 1Gi
```

**seller-service-deployment.yaml**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: seller-service
  labels:
    app: seller-service
spec:
  replicas: 2
  selector:
    matchLabels:
      app: seller-service
  template:
    metadata:
      labels:
        app: seller-service
    spec:
      containers:
      - name: seller-service
        image: cozycomfort/seller-service:latest
        ports:
        - containerPort: 5003
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:5003"
        - name: DistributorService__BaseUrl
          value: "http://distributor-service:5002"
        volumeMounts:
        - name: data
          mountPath: /app/data
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: seller-data-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: seller-service
spec:
  selector:
    app: seller-service
  ports:
  - port: 5003
    targetPort: 5003
  type: ClusterIP
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: seller-data-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 1Gi
```

**ingress.yaml** (for external access)
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: cozy-comfort-ingress
spec:
  rules:
  - host: api.cozycomfort.com
    http:
      paths:
      - path: /manufacturer
        pathType: Prefix
        backend:
          service:
            name: manufacturer-service
            port:
              number: 5001
      - path: /distributor
        pathType: Prefix
        backend:
          service:
            name: distributor-service
            port:
              number: 5002
      - path: /seller
        pathType: Prefix
        backend:
          service:
            name: seller-service
            port:
              number: 5003
  - host: cozycomfort.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: client-app-web
            port:
              number: 5006
```

### Deploy to Kubernetes

```bash
# Apply all deployments
kubectl apply -f manufacturer-service-deployment.yaml
kubectl apply -f distributor-service-deployment.yaml
kubectl apply -f seller-service-deployment.yaml
kubectl apply -f ingress.yaml

# Check status
kubectl get pods
kubectl get services
kubectl get ingress

# View logs
kubectl logs -f deployment/manufacturer-service
```

---

## Cloud Platform Deployment

### Azure App Service

1. **Create App Service Plans**
   - One for each service tier
   - Choose appropriate SKU (Basic/Standard)

2. **Deploy Services**
   ```bash
   az webapp create --resource-group cozy-comfort-rg \
     --plan cozy-comfort-plan \
     --name cozy-comfort-manufacturer \
     --runtime "DOTNET|7.0"
   
   az webapp deployment source config-zip \
     --resource-group cozy-comfort-rg \
     --name cozy-comfort-manufacturer \
     --src manufacturer-service.zip
   ```

3. **Configure Application Settings**
   - Connection strings
   - Inter-service URLs
   - Environment variables

### AWS Elastic Beanstalk

1. **Create Application**
   ```bash
   eb init -p "64bit Amazon Linux 2 v2.0.0 running .NET Core" cozy-comfort
   ```

2. **Deploy Each Service**
   ```bash
   cd ManufacturerService
   eb create manufacturer-service-env
   eb deploy
   ```

### Google Cloud Run

1. **Build and Push Images**
   ```bash
   gcloud builds submit --tag gcr.io/PROJECT_ID/manufacturer-service
   ```

2. **Deploy Services**
   ```bash
   gcloud run deploy manufacturer-service \
     --image gcr.io/PROJECT_ID/manufacturer-service \
     --platform managed \
     --region us-central1 \
     --allow-unauthenticated
   ```

---

## Comparison of Deployment Methods

| Method | Pros | Cons | Best For |
|--------|------|------|----------|
| **Traditional Server** | Full control, predictable costs | Manual setup, scaling challenges | Small to medium deployments |
| **Docker** | Consistent environments, easy local dev | Requires Docker knowledge | Development, small production |
| **Kubernetes** | Auto-scaling, high availability | Complex setup, learning curve | Large scale, production |
| **Cloud Platforms** | Managed services, auto-scaling | Vendor lock-in, costs | Enterprise, rapid deployment |

---

## Recommendations

1. **Development**: Use Docker Compose for local development
2. **Small Production**: Traditional server deployment with systemd/IIS
3. **Medium Production**: Docker Swarm or Docker Compose on VPS
4. **Large Production**: Kubernetes on cloud platform (AKS, EKS, GKE)
5. **Enterprise**: Cloud-native services (Azure App Service, AWS ECS, GCP Cloud Run)

---

## Monitoring and Maintenance

### Health Checks
- Implement `/health` endpoints for each service
- Configure Kubernetes liveness and readiness probes
- Set up monitoring dashboards (Prometheus + Grafana)

### Logging
- Centralized logging (ELK Stack, Azure Log Analytics)
- Structured logging with Serilog
- Log aggregation and analysis

### Backup Strategy
- Regular database backups
- Persistent volume snapshots
- Disaster recovery plan

---

## Security Considerations

1. **HTTPS/TLS**: Enable SSL certificates for all endpoints
2. **Authentication**: Implement API keys or OAuth2
3. **Network Security**: Use private networks, firewalls
4. **Secrets Management**: Use Kubernetes Secrets or Azure Key Vault
5. **Input Validation**: Already implemented in services
6. **Rate Limiting**: Implement API rate limiting

---

## Scaling Strategies

1. **Horizontal Scaling**: Add more service instances
2. **Vertical Scaling**: Increase resource allocation
3. **Database Scaling**: Migrate to PostgreSQL with read replicas
4. **Caching**: Implement Redis for frequently accessed data
5. **Load Balancing**: Use load balancers for traffic distribution
