# SellerService

## Overview
SellerService is part of the Cozy Comfort Service-Oriented Architecture (SOA) system. It serves as the point of contact for end customers, processing customer orders and coordinating fulfillment through the DistributorService.

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
- ✅ Inter-service HTTP communication with DistributorService
- ✅ Input validation

## API Endpoints

### POST /api/customerorder
Processes a customer order and coordinates fulfillment through DistributorService.

**Request Body:**
```json
{
  "customerName": "John Doe",
  "customerEmail": "john@example.com",
  "customerPhone": "123-456-7890",
  "shippingAddress": "123 Main St, City, State 12345",
  "items": [
    {
      "blanketId": 1,
      "quantity": 2
    },
    {
      "blanketId": 3,
      "quantity": 1
    }
  ]
}
```

**Response:**
```json
{
  "orderId": 1,
  "status": "Fulfilled",
  "message": "Order 1 has been processed",
  "items": [
    {
      "blanketId": 1,
      "modelName": "Cozy Classic",
      "quantity": 2,
      "status": "Fulfilled",
      "message": "Order fulfilled from distributor stock. 43 units remaining."
    }
  ],
  "totalAmount": 159.98
}
```

### GET /api/availability/{modelId}
Checks availability of a blanket model through DistributorService.

**Response:**
```json
{
  "blanketId": 1,
  "modelName": "Cozy Classic",
  "isAvailable": true,
  "availableQuantity": 45,
  "message": "45 units available in distributor stock"
}
```

## Inter-Service Communication

SellerService communicates with DistributorService using HTTP clients:
- **PlaceOrderAsync**: Places order with DistributorService
- **CheckAvailabilityAsync**: Checks product availability

## Prerequisites
- .NET 7.0 SDK or higher
- SQLite (included with .NET)
- DistributorService must be running (default: http://localhost:5002)
- ManufacturerService must be running (default: http://localhost:5001)

## Setup Instructions

1. **Restore NuGet Packages**
   ```bash
   dotnet restore
   ```

2. **Update Configuration**
   Edit `appsettings.json` and ensure `DistributorService:BaseUrl` points to your DistributorService instance.

3. **Run the Service**
   ```bash
   dotnet run
   ```

4. **Access Swagger UI**
   Navigate to `http://localhost:5003` (check console output for actual port)

## Database Schema

### CustomerOrder
- Id (PK)
- CustomerName
- CustomerEmail
- CustomerPhone
- ShippingAddress
- Status (Pending, Processing, Fulfilled, Cancelled)
- OrderDate
- FulfilledDate
- TotalAmount

### OrderItem
- Id (PK)
- CustomerOrderId (FK)
- BlanketId
- ModelName
- Quantity
- UnitPrice
- Status (Pending, Available, Unavailable, Fulfilled)

## Order Processing Flow

1. Customer places order via POST /api/customerorder
2. SellerService creates order record
3. For each item:
   - Check availability with DistributorService
   - If available → Place order with DistributorService
   - Update item status based on DistributorService response
4. Determine overall order status
5. Return response with item-level status information

## Logging
Logs are written to:
- Console (for development)
- File: `logs/seller-service-YYYYMMDD.txt`

## Testing the API

### Using Swagger UI
Navigate to the root URL to access interactive API documentation.

### Using curl
```bash
# Check availability
curl http://localhost:5003/api/availability/1

# Place customer order
curl -X POST http://localhost:5003/api/customerorder \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "John Doe",
    "customerEmail": "john@example.com",
    "shippingAddress": "123 Main St",
    "items": [
      {
        "blanketId": 1,
        "quantity": 2
      }
    ]
  }'
```
