# Cozy Comfort - Service-Oriented Architecture System

## Overview
Cozy Comfort is a Service-Oriented Computing (SOC) based system for blanket supply chain management. It replaces manual phone/email ordering processes with service-based API communication.

## 📚 Documentation

Documentation is in the [`docs/`](./docs/) directory:

- **[Workflows](./docs/WORKFLOWS.md)** - Order flow overview and diagram (single source of truth)
- **[Business Logic](./docs/BUSINESS_LOGIC.md)** - End-to-end flow and implementation mapping
- **[Design Diagrams](./docs/DESIGN_DIAGRAMS.md)** - Architecture, sequence, class, and ER diagrams
- **[Architecture: Monolithic vs SOA](./docs/ARCHITECTURE_COMPARISON.md)** - Comparison and justification
- **[Testing](./docs/TESTING_DOCUMENTATION.md)** - Test strategy, cases, and results
- **[Deployment](./docs/DEPLOYMENT.md)** - Docker, Kubernetes, and cloud deployment

## System Architecture

The system consists of three independent ASP.NET Core Web API services:

1. **ManufacturerService** - Manages blanket models, stock levels, and production capacity
2. **DistributorService** - Maintains distributor inventory and processes seller orders
3. **SellerService** - Receives customer orders and coordinates fulfillment

## Architecture Principles

- ✅ **Service-Oriented Architecture (SOA)** - Independent services with clear boundaries
- ✅ **Clean Architecture** - Separation of concerns across layers
- ✅ **Database per Service** - Each service has its own database
- ✅ **Loose Coupling** - Services communicate via HTTP APIs
- ✅ **SOLID Principles** - Maintainable and extensible code
- ✅ **Repository Pattern** - Data access abstraction
- ✅ **DTO Pattern** - API contract separation

## Service Communication Flow

```
Customer → SellerService → DistributorService → ManufacturerService
```

1. **Customer** places order via SellerService
2. **SellerService** checks availability with DistributorService
3. **DistributorService** checks its inventory
4. If unavailable, DistributorService queries ManufacturerService for production capacity
5. Response flows back through the chain

## Project Structure

```
cozy_comfort/
├── ManufacturerService/           # Port 5001
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Models/
│   ├── DTOs/
│   └── Data/
├── DistributorService/            # Port 5002
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Models/
│   ├── DTOs/
│   └── Data/
├── SellerService/                  # Port 5003
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Models/
│   ├── DTOs/
│   └── Data/
├── ClientApp/                      # Console Client Application
├── ClientAppWeb/                   # Web Client Application (Port 5006)
├── ManufacturerService.Tests/      # Unit tests for ManufacturerService
├── DistributorService.Tests/        # Unit tests for DistributorService
├── SellerService.Tests/             # Unit tests for SellerService
├── CozyComfort.IntegrationTests/   # Integration tests
└── docs/                           # Documentation
    ├── DESIGN_DIAGRAMS.md
    ├── ARCHITECTURE_COMPARISON.md
    ├── TESTING_DOCUMENTATION.md
    └── DEPLOYMENT.md
```

## Prerequisites

Before running the system, ensure you have:

- **.NET 7.0 SDK or higher** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **SQLite** - Included with .NET (no separate installation needed)
- **Terminal/Command Prompt** - To run multiple services simultaneously
- **Code Editor** (Optional) - Visual Studio 2022, VS Code, or Rider

### Verify .NET Installation

```bash
dotnet --version
```

You should see version 7.0.x or higher.

## How to Run the System

### 🚀 Quick Start (Single Command)

**macOS/Linux:**
```bash
./start-services.sh
```

**Windows (PowerShell):**
```powershell
.\start-services.ps1
```

**Stop all services:**
```bash
# macOS/Linux
./stop-services.sh

# Windows
.\stop-services.ps1
```

### Single-Line Commands (Alternative)

If you prefer single-line commands without scripts:

**macOS/Linux:**
```bash
cd ManufacturerService && dotnet run > ../logs/manufacturer.log 2>&1 & cd .. && cd DistributorService && dotnet run > ../logs/distributor.log 2>&1 & cd .. && cd SellerService && dotnet run > ../logs/seller.log 2>&1 & cd .. && echo "All services starting in background. Check logs/ directory for output."
```

**Windows PowerShell:**
```powershell
Start-Job -ScriptBlock { cd ManufacturerService; dotnet run } | Out-Null; Start-Sleep -Seconds 2; Start-Job -ScriptBlock { cd DistributorService; dotnet run } | Out-Null; Start-Sleep -Seconds 2; Start-Job -ScriptBlock { cd SellerService; dotnet run } | Out-Null; Write-Host "All services started in background jobs"
```

---

### Manual Method (Step-by-Step)

The system consists of 3 services that must run simultaneously. You'll need **4 terminal windows** (3 for services + 1 for client app).

### Step-by-Step Instructions

#### Step 1: Open Terminal/Command Prompt

Open your terminal (macOS/Linux) or Command Prompt/PowerShell (Windows).

#### Step 2: Navigate to Project Root

```bash
cd /path/to/cozy_comfort
```

Or if you're already in the project directory:
```bash
pwd  # Verify you're in the right location
```

#### Step 3: Restore All Packages (First Time Only)

From the project root, restore packages for all projects:

```bash
dotnet restore
```

Or restore individually:
```bash
dotnet restore ManufacturerService/ManufacturerService.csproj
dotnet restore DistributorService/DistributorService.csproj
dotnet restore SellerService/SellerService.csproj
dotnet restore ClientApp/ClientApp.csproj
```

#### Step 4: Start ManufacturerService (Terminal 1)

Open **Terminal 1** and run:

```bash
cd ManufacturerService
dotnet run
```

**Expected Output:**
```
info: ManufacturerService[0]
      Database seeded successfully
info: ManufacturerService[0]
      Manufacturer Service starting up
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5001
```

**Verify:** Open browser to `http://localhost:5001` - You should see Swagger UI.

**Keep this terminal open!** The service must keep running.

#### Step 5: Start DistributorService (Terminal 2)

Open **Terminal 2** (new terminal window) and run:

```bash
cd DistributorService
dotnet run
```

**Expected Output:**
```
info: DistributorService[0]
      Database seeded successfully
info: DistributorService[0]
      Distributor Service starting up
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5002
```

**Verify:** Open browser to `http://localhost:5002` - You should see Swagger UI.

**Keep this terminal open!**

#### Step 6: Start SellerService (Terminal 3)

Open **Terminal 3** (new terminal window) and run:

```bash
cd SellerService
dotnet run
```

**Expected Output:**
```
info: SellerService[0]
      Seller Service starting up
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5003
```

**Verify:** Open browser to `http://localhost:5003` - You should see Swagger UI.

**Keep this terminal open!**

#### Step 7: Run Client Application (Terminal 4)

Open **Terminal 4** (new terminal window) and run:

```bash
cd ClientApp
dotnet run
```

**Expected Output:**
```
==========================================
  Cozy Comfort - Client Application
  Service-Oriented Architecture Demo
==========================================

Checking service availability...

✓ ManufacturerService is running at http://localhost:5001
✓ DistributorService is running at http://localhost:5002
✓ SellerService is running at http://localhost:5003

--- Main Menu ---
1. View All Blanket Models (ManufacturerService)
2. Check Stock for a Model (ManufacturerService)
3. View Distributor Inventory (DistributorService)
4. Check Product Availability (SellerService)
5. Place Customer Order (SellerService)
6. Complete Order Flow Demo
7. Check Production Capacity (ManufacturerService)
0. Exit

Enter your choice:
```

### Quick Start Script (Alternative Method)

If you prefer, you can use these commands in separate terminals:

**Terminal 1:**
```bash
cd ManufacturerService && dotnet run
```

**Terminal 2:**
```bash
cd DistributorService && dotnet run
```

**Terminal 3:**
```bash
cd SellerService && dotnet run
```

**Terminal 4:**
```bash
cd ClientApp && dotnet run
```

### Using Visual Studio

1. Open `CozyComfort.sln` in Visual Studio
2. Right-click solution → **Properties** → **Startup Project** → **Multiple startup projects**
3. Set all three services to **Start**
4. Set ClientApp to **Start** (optional, or run separately)
5. Press **F5** to run all services

### Using VS Code

1. Open the `cozy_comfort` folder in VS Code
2. Open integrated terminal (Ctrl+` or Cmd+`)
3. Split terminal into 4 panes (Terminal → Split Terminal)
4. Run each service in a separate pane using the commands above

## Verifying Services Are Running

### Method 1: Check Swagger UI

Open these URLs in your browser:
- **ManufacturerService:** http://localhost:5001
- **DistributorService:** http://localhost:5002
- **SellerService:** http://localhost:5003

You should see Swagger documentation for each service.

### Method 2: Use curl Commands

**Test ManufacturerService:**
```bash
curl http://localhost:5001/api/blankets
```

**Test DistributorService:**
```bash
curl http://localhost:5002/api/inventory
```

**Test SellerService:**
```bash
curl http://localhost:5003/api/availability/1
```

### Method 3: Check Logs

Each service logs to console. Look for:
- `Now listening on: http://localhost:XXXX`
- `Database seeded successfully`
- No error messages

## Troubleshooting

### Port Already in Use

**Error:** `Address already in use` or `Port 5001 is already in use`

**Solution:**
1. Find the process using the port:
   ```bash
   # macOS/Linux
   lsof -i :5001
   
   # Windows
   netstat -ano | findstr :5001
   ```
2. Kill the process or change the port in `Properties/launchSettings.json`

### Database Locked Error

**Error:** `database is locked`

**Solution:**
- Stop all services
- Delete `.db` files in each service directory
- Restart services (they will recreate databases)

### Service Not Found

**Error:** `Unable to connect` or `Service unavailable`

**Solution:**
1. Verify all services are running (check all 3 terminals)
2. Check service URLs in `appsettings.json`
3. Ensure services started in correct order:
   - ManufacturerService first
   - Then DistributorService
   - Then SellerService

### Package Restore Failed

**Error:** `NU1301` or package restore errors

**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore again
dotnet restore
```

### Client App Can't Connect

**Error:** Client app shows services as not running

**Solution:**
1. Verify all 3 services are running
2. Check that ports match:
   - ManufacturerService: 5001
   - DistributorService: 5002
   - SellerService: 5003
3. Try accessing Swagger UI in browser first

### Order could not be placed (InternalServerError)

**Error:** Web or client shows "Order could not be placed: InternalServerError" (or another status). The web app now shows the API error detail when available.

**Solution:**
1. **Start services in order:** ManufacturerService → DistributorService → SellerService. If DistributorService is not running, SellerService may fail when processing orders.
2. **SellerService database:** Ensure SQLite is writable. Check `SellerService/appsettings.json` → `ConnectionStrings:DefaultConnection`. The service runs `EnsureCreated()` at startup; if the path is invalid or read-only, order placement will fail.
3. **URLs:** ClientAppWeb: `appsettings.json` → `Services:SellerServiceUrl` (default `http://localhost:5003`). SellerService: `DistributorService:BaseUrl` (default `http://localhost:5002`). Ensure they point to the running instances.

## Testing the Complete System

### Option 1: Use Client Application (Recommended)

1. Run the client app (Terminal 4)
2. Select option **6** for "Complete Order Flow Demo"
3. This demonstrates the full flow automatically

### Option 2: Use Swagger UI

1. Open each service's Swagger UI
2. Test endpoints interactively
3. Use "Try it out" feature

### Option 3: Use curl Commands

See the "Testing the System" section below for curl examples.

## Stopping Services

To stop services:
1. Go to each terminal running a service
2. Press `Ctrl+C` (or `Cmd+C` on Mac)
3. Wait for graceful shutdown

**Note:** Stop services in reverse order:
1. Client App (if running)
2. SellerService
3. DistributorService
4. ManufacturerService

## API Endpoints Summary

### ManufacturerService (Port 5001)
- `GET /health` - Health check
- `GET /api/blankets` - Get all blanket models
- `GET /api/blankets/stock/{modelId}` - Get stock information
- `POST /api/blankets/produce` - Check production capacity

### DistributorService (Port 5002)
- `GET /health` - Health check
- `GET /api/inventory` - Get distributor inventory
- `POST /api/order` - Process seller order

### SellerService (Port 5003)
- `GET /health` - Health check
- `POST /api/customerorder` - Process customer order
- `GET /api/availability/{modelId}` - Check product availability

## Testing the System

### Quick Test: Complete Order Flow

Follow these steps to test the entire system:

#### 1. Check Manufacturer Stock
```bash
curl http://localhost:5001/api/blankets/stock/1
```

**Expected Response:**
```json
{
  "blanketId": 1,
  "modelName": "Cozy Classic",
  "quantity": 150,
  "availableQuantity": 130,
  ...
}
```

#### 2. Check Distributor Inventory
```bash
curl http://localhost:5002/api/inventory
```

**Expected Response:** Array of inventory items

#### 3. Check Product Availability (via SellerService)
```bash
curl http://localhost:5003/api/availability/1
```

**Expected Response:**
```json
{
  "blanketId": 1,
  "isAvailable": true,
  "availableQuantity": 45,
  "message": "45 units available in distributor stock"
}
```

#### 4. Place Customer Order
```bash
curl -X POST http://localhost:5003/api/customerorder \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "John Doe",
    "customerEmail": "john@example.com",
    "customerPhone": "123-456-7890",
    "shippingAddress": "123 Main St, City, State 12345",
    "items": [
      {
        "blanketId": 1,
        "quantity": 2
      }
    ]
  }'
```

**Expected Response:**
```json
{
  "orderId": 1,
  "status": "Fulfilled",
  "message": "Order 1 has been processed",
  "items": [...],
  "totalAmount": 159.98
}
```

### Using the Client Application

The easiest way to test is using the client application:

1. Run the client app: `cd ClientApp && dotnet run`
2. Select option **6** - "Complete Order Flow Demo"
3. This will automatically test all services and show the complete flow

### Testing Individual Services

#### Test ManufacturerService Endpoints

**Get all blankets:**
```bash
curl http://localhost:5001/api/blankets
```

**Get stock for model ID 1:**
```bash
curl http://localhost:5001/api/blankets/stock/1
```

**Check production capacity:**
```bash
curl -X POST http://localhost:5001/api/blankets/produce \
  -H "Content-Type: application/json" \
  -d '{"blanketId": 1, "quantity": 100}'
```

#### Test DistributorService Endpoints

**Get inventory:**
```bash
curl http://localhost:5002/api/inventory
```

**Place order:**
```bash
curl -X POST http://localhost:5002/api/order \
  -H "Content-Type: application/json" \
  -d '{
    "sellerId": "Seller-001",
    "blanketId": 1,
    "quantity": 10
  }'
```

#### Test SellerService Endpoints

**Check availability:**
```bash
curl http://localhost:5003/api/availability/1
```

**Place customer order:**
```bash
curl -X POST http://localhost:5003/api/customerorder \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Jane Smith",
    "customerEmail": "jane@example.com",
    "shippingAddress": "456 Oak Ave",
    "items": [{"blanketId": 1, "quantity": 1}]
  }'
```

## Swagger Documentation

Each service provides Swagger UI at its root URL:
- ManufacturerService: http://localhost:5001
- DistributorService: http://localhost:5002
- SellerService: http://localhost:5003

## Database Files

Each service creates its own SQLite database:
- `ManufacturerServiceDb.db` - ManufacturerService database
- `DistributorServiceDb.db` - DistributorService database
- `SellerServiceDb.db` - SellerService database

## Configuration

### Backend service URLs (APIs)
Update upstream URLs in `appsettings.json` for each service:

**DistributorService:** `ManufacturerService:BaseUrl` (default `http://localhost:5001`)  
**SellerService:** `DistributorService:BaseUrl` (default `http://localhost:5002`)

### Client applications
Backend URLs are configurable so clients work against local or Docker:

- **ClientAppWeb**: `appsettings.json` or environment (`Services:ManufacturerServiceUrl`, etc.). In Docker, set `Services__ManufacturerServiceUrl`, `Services__DistributorServiceUrl`, `Services__SellerServiceUrl`.
- **ClientApp / ClientAppDesktop**: Environment variables `Services__ManufacturerServiceUrl`, `Services__DistributorServiceUrl`, `Services__SellerServiceUrl` (defaults: localhost:5001, 5002, 5003).
- See [.env.example](.env.example) for a list of variables.

## Design Patterns Used

- **Repository Pattern** - Data access abstraction
- **Service Layer Pattern** - Business logic encapsulation
- **DTO Pattern** - API contract separation
- **Dependency Injection** - Loose coupling
- **Middleware Pattern** - Cross-cutting concerns
- **HTTP Client Pattern** - Inter-service communication

## SOLID Principles

- **Single Responsibility** - Each class has one reason to change
- **Open/Closed** - Open for extension, closed for modification
- **Liskov Substitution** - Repository implementations are interchangeable
- **Interface Segregation** - Focused interfaces
- **Dependency Inversion** - Depend on abstractions, not concretions

## Logging

All services use Serilog for structured logging:
- Console output (development)
- File logging: `logs/{service-name}-YYYYMMDD.txt`

## Error Handling

Each service includes:
- Global exception handling middleware
- Proper HTTP status codes
- Error response DTOs
- Structured error logging

## Seed Data

All services automatically seed example data on startup:
- **ManufacturerService**: 5 blanket models with stock and production capacity
- **DistributorService**: 5 inventory items matching manufacturer models
- **SellerService**: No seed data (orders created on demand)

## Development Notes

- All services use SQLite for cross-platform compatibility
- Services can be run independently or together
- Each service has its own Swagger documentation
- CORS is enabled for inter-service communication
- All services follow the same architectural patterns

## Testing

The project includes comprehensive test suites:

- **Unit Tests**: Test projects for each service (xUnit, Moq, FluentAssertions)
- **Integration Tests**: End-to-end service communication tests
- **Test Coverage**: ~67% overall coverage

Run tests:
```bash
dotnet test
```

See [Testing Documentation](./docs/TESTING_DOCUMENTATION.md) for detailed test cases and results.

## Next Steps

- Add authentication/authorization
- Implement API versioning
- Increase test coverage to 80%+
- Implement retry policies for inter-service calls
- Add health check endpoints
- Implement distributed tracing
- Add API rate limiting
- Implement caching strategies

## Quick Reference

### 🎯 Fastest Way to Start

**macOS/Linux:**
```bash
./start-services.sh
```

**Windows:**
```powershell
.\start-services.ps1
```

### Service Ports
- **ManufacturerService:** http://localhost:5001
- **DistributorService:** http://localhost:5002
- **SellerService:** http://localhost:5003

### Start All Services (Manual Commands)

**Terminal 1:**
```bash
cd ManufacturerService && dotnet run
```

**Terminal 2:**
```bash
cd DistributorService && dotnet run
```

**Terminal 3:**
```bash
cd SellerService && dotnet run
```

**Terminal 4 (Client App):**
```bash
cd ClientApp && dotnet run
```

### Database Files Location
- `ManufacturerService/ManufacturerServiceDb.db`
- `DistributorService/DistributorServiceDb.db`
- `SellerService/SellerServiceDb.db`

### Log Files Location
- `ManufacturerService/logs/manufacturer-service-YYYYMMDD.txt`
- `DistributorService/logs/distributor-service-YYYYMMDD.txt`
- `SellerService/logs/seller-service-YYYYMMDD.txt`

### Common Commands

**Restore all packages:**
```bash
dotnet restore
```

**Build all projects:**
```bash
dotnet build
```

**Clean build artifacts:**
```bash
dotnet clean
```

**Run specific service:**
```bash
cd ManufacturerService
dotnet run
```

**Check if port is in use (macOS/Linux):**
```bash
lsof -i :5001
```

**Check if port is in use (Windows):**
```bash
netstat -ano | findstr :5001
```

