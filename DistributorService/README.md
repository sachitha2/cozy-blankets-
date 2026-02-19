# DistributorService

## Overview
DistributorService is part of the Cozy Comfort Service-Oriented Architecture (SOA) system. It acts as an intermediary between the Manufacturer and Sellers, managing distributor inventory and processing orders from sellers.

For end-to-end order flow, see [Workflows](../docs/WORKFLOWS.md).

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
- ✅ Inter-service HTTP communication with ManufacturerService
- ✅ Input validation

## API Endpoints

### GET /api/inventory
Retrieves all inventory items held by the distributor.

**Response:**
```json
[
  {
    "id": 1,
    "blanketId": 1,
    "modelName": "Cozy Classic",
    "quantity": 50,
    "reservedQuantity": 5,
    "availableQuantity": 45,
    "unitCost": 35.00,
    "lastUpdated": "2024-01-01T00:00:00Z"
  }
]
```

### POST /api/order
Processes an order from a seller. If distributor stock is unavailable, automatically checks with ManufacturerService.

**Request Body:**
```json
{
  "sellerId": "Seller-001",
  "blanketId": 1,
  "quantity": 20,
  "notes": "Urgent order"
}
```

**Response:**
```json
{
  "orderId": 1,
  "status": "Fulfilled",
  "message": "Order fulfilled from distributor stock. 25 units remaining.",
  "fulfilledFromStock": true,
  "requiresManufacturerOrder": false
}
```

## Inter-Service Communication

DistributorService communicates with ManufacturerService using HTTP clients:
- **GetStockAsync**: Retrieves stock information from ManufacturerService
- **CheckProductionAsync**: Checks production capacity and lead times

## Prerequisites
- .NET 7.0 SDK or higher
- SQLite (included with .NET)
- ManufacturerService must be running (default: http://localhost:5001)

## Setup Instructions

1. **Restore NuGet Packages**
   ```bash
   dotnet restore
   ```

2. **Update Configuration**
   Edit `appsettings.json` and ensure `ManufacturerService:BaseUrl` points to your ManufacturerService instance.

3. **Run the Service**
   ```bash
   dotnet run
   ```

4. **Access Swagger UI**
   Navigate to `http://localhost:5002` (check console output for actual port)

## Database Schema

### Inventory
- Id (PK)
- BlanketId (Unique, references ManufacturerService)
- ModelName
- Quantity
- ReservedQuantity
- UnitCost
- LastUpdated

### Order
- Id (PK)
- SellerId
- BlanketId
- ModelName
- Quantity
- Status (Pending, Fulfilled, Cancelled, PendingManufacturer)
- OrderDate
- FulfilledDate
- Notes

## Seed Data
The service automatically seeds the database with 5 example inventory items matching ManufacturerService blanket IDs.

## Order Processing Flow

1. Seller places order via POST /api/order
2. DistributorService checks its own inventory
3. If sufficient stock → Reserve and fulfill immediately
4. If insufficient stock → Call ManufacturerService API to check production capacity
5. Return appropriate response with status and lead time information

## Logging
Logs are written to:
- Console (for development)
- File: `logs/distributor-service-YYYYMMDD.txt`

## Testing the API

### Using Swagger UI
Navigate to the root URL to access interactive API documentation.

### Using curl
```bash
# Get inventory
curl http://localhost:5002/api/inventory

# Place order
curl -X POST http://localhost:5002/api/order \
  -H "Content-Type: application/json" \
  -d '{
    "sellerId": "Seller-001",
    "blanketId": 1,
    "quantity": 20
  }'
```
