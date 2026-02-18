# ManufacturerService

## Overview
ManufacturerService is part of the Cozy Comfort Service-Oriented Architecture (SOA) system. It manages blanket models, stock levels, production capacity, and lead time information for the manufacturer.

## Architecture
This service follows **Clean Architecture** principles with the following layers:

- **Controllers**: API endpoints (Presentation Layer)
- **Services**: Business logic (Application Layer)
- **Repositories**: Data access (Infrastructure Layer)
- **Models**: Domain entities (Domain Layer)
- **DTOs**: Data Transfer Objects for API communication
- **Data**: DbContext and database configuration
- **Middleware**: Cross-cutting concerns (error handling)

## Features
- ✅ RESTful API endpoints
- ✅ Entity Framework Core with SQLite (cross-platform)
- ✅ Repository Pattern
- ✅ Dependency Injection
- ✅ Error Handling Middleware
- ✅ Structured Logging (Serilog)
- ✅ Swagger/OpenAPI documentation
- ✅ Database per Service pattern
- ✅ Input validation
- ✅ Seed data

## API Endpoints

### GET /api/blankets
Retrieves all blanket models.

**Response:**
```json
[
  {
    "id": 1,
    "modelName": "Cozy Classic",
    "material": "100% Cotton",
    "description": "Soft and comfortable classic blanket",
    "unitPrice": 49.99,
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

### GET /api/blankets/stock/{modelId}
Retrieves stock information for a specific blanket model.

**Response:**
```json
{
  "blanketId": 1,
  "modelName": "Cozy Classic",
  "quantity": 150,
  "reservedQuantity": 20,
  "availableQuantity": 130,
  "lastUpdated": "2024-01-01T00:00:00Z"
}
```

### POST /api/blankets/produce
Processes a production request and returns availability and lead time information.

**Request Body:**
```json
{
  "blanketId": 1,
  "quantity": 100,
  "requestedDeliveryDate": "2024-02-01T00:00:00Z"
}
```

**Response:**
```json
{
  "canProduce": true,
  "availableStock": 130,
  "leadTimeDays": 3,
  "estimatedCompletionDate": "2024-02-04T00:00:00Z",
  "message": "Can produce 100 units. 130 available now, 0 need production."
}
```

## Prerequisites
- .NET 7.0 SDK or higher
- SQLite (included with .NET, no separate installation needed)
- Visual Studio 2022, VS Code, or Rider

## Setup Instructions

1. **Restore NuGet Packages**
   ```bash
   dotnet restore
   ```

2. **Database Configuration**
   The service uses SQLite by default (cross-platform). The database file `ManufacturerServiceDb.db` will be created automatically in the project root when you run the application.
   
   To use a different database location, edit `appsettings.json` and update the `ConnectionStrings:DefaultConnection` path.

3. **Run the Service**
   ```bash
   dotnet run
   ```

4. **Access Swagger UI**
   Navigate to `https://localhost:5001` or `http://localhost:5000` (check console output for actual port)

## Database Schema

### Blanket
- Id (PK)
- ModelName (Unique)
- Material
- Description
- UnitPrice
- CreatedAt
- UpdatedAt

### Stock
- Id (PK)
- BlanketId (FK, Unique)
- Quantity
- ReservedQuantity
- LastUpdated

### ProductionCapacity
- Id (PK)
- BlanketId (FK, Unique)
- DailyCapacity
- LeadTimeDays
- IsActive
- LastUpdated

## Seed Data
The service automatically seeds the database with 5 example blanket models on startup:
1. Cozy Classic
2. Warm Winter
3. Luxury Plush
4. Light Breeze
5. Family Size

Each blanket has associated stock and production capacity data.

## Logging
Logs are written to:
- Console (for development)
- File: `logs/manufacturer-service-YYYYMMDD.txt`

## Testing the API

### Using Swagger UI
1. Navigate to the root URL (Swagger UI is configured at root)
2. Use the interactive API documentation to test endpoints

### Using curl
```bash
# Get all blankets
curl https://localhost:5001/api/blankets

# Get stock for model ID 1
curl https://localhost:5001/api/blankets/stock/1

# Process production request
curl -X POST https://localhost:5001/api/blankets/produce \
  -H "Content-Type: application/json" \
  -d '{"blanketId": 1, "quantity": 100}'
```

## Project Structure
```
ManufacturerService/
├── Controllers/
│   └── BlanketsController.cs
├── Services/
│   ├── IBlanketService.cs
│   └── BlanketService.cs
├── Repositories/
│   ├── IBlanketRepository.cs
│   ├── BlanketRepository.cs
│   ├── IStockRepository.cs
│   ├── StockRepository.cs
│   ├── IProductionCapacityRepository.cs
│   └── ProductionCapacityRepository.cs
├── Models/
│   ├── Blanket.cs
│   ├── Stock.cs
│   └── ProductionCapacity.cs
├── DTOs/
│   ├── BlanketDto.cs
│   ├── StockDto.cs
│   ├── ProductionRequestDto.cs
│   └── ProductionResponseDto.cs
├── Data/
│   ├── ManufacturerDbContext.cs
│   └── DatabaseSeeder.cs
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── Program.cs
├── appsettings.json
└── ManufacturerService.csproj
```

## Design Patterns Used
- **Repository Pattern**: Abstracts data access
- **Service Layer Pattern**: Encapsulates business logic
- **DTO Pattern**: Separates API contracts from domain models
- **Dependency Injection**: Loose coupling and testability
- **Middleware Pattern**: Cross-cutting concerns

## SOLID Principles
- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Repository implementations are interchangeable
- **Interface Segregation**: Focused interfaces (IBlanketRepository, IStockRepository, etc.)
- **Dependency Inversion**: Depend on abstractions (interfaces), not concretions

## Next Steps
After ManufacturerService is running, proceed to implement:
1. DistributorService
2. SellerService
3. Inter-service HTTP client communication
