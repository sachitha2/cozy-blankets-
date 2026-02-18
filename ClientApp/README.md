# Cozy Comfort Client Application

## Overview
Console client application that demonstrates consuming all three Cozy Comfort services (ManufacturerService, DistributorService, and SellerService). This application showcases the complete order flow through the service-oriented architecture.

## Features
- ✅ Interactive menu-driven interface
- ✅ Consumes all three services via HTTP APIs
- ✅ Demonstrates complete order flow
- ✅ Service availability checking
- ✅ Error handling

## Prerequisites
- .NET 7.0 SDK or higher
- All three services must be running:
  - ManufacturerService on port 5001
  - DistributorService on port 5002
  - SellerService on port 5003

## Setup Instructions

1. **Ensure all services are running**
   ```bash
   # Terminal 1
   cd ManufacturerService && dotnet run
   
   # Terminal 2
   cd DistributorService && dotnet run
   
   # Terminal 3
   cd SellerService && dotnet run
   ```

2. **Run the client application**
   ```bash
   cd ClientApp
   dotnet restore
   dotnet run
   ```

## Menu Options

1. **View All Blanket Models** - Displays all blankets from ManufacturerService
2. **Check Stock** - Checks stock for a specific model (ManufacturerService)
3. **View Distributor Inventory** - Shows distributor inventory (DistributorService)
4. **Check Product Availability** - Checks availability through SellerService
5. **Place Customer Order** - Places an order through SellerService
6. **Complete Order Flow Demo** - Demonstrates the full order flow
7. **Check Production Capacity** - Checks manufacturer production capacity

## Example Usage

### Complete Order Flow Demo
Option 6 demonstrates the complete flow:
1. View available blankets
2. Check manufacturer stock
3. Check distributor inventory
4. Check availability through seller service
5. Place a sample customer order

### Place a Customer Order
```
1. Select option 5
2. Enter customer details
3. Enter blanket model ID (1-5)
4. Enter quantity
5. View order response with status
```

## Service Communication Flow

The client application demonstrates:
```
ClientApp → SellerService → DistributorService → ManufacturerService
```

## Error Handling

The application includes:
- Service availability checking on startup
- Try-catch blocks for all API calls
- User-friendly error messages
- Validation of user input

## Code Structure

- `Program.cs` - Main application with menu system and API calls
- Uses `HttpClient` for HTTP communication
- Uses dynamic JSON deserialization for flexibility
- Menu-driven interface for easy interaction

## Testing the Services

This client application serves as a test client to:
- Verify all services are working correctly
- Test inter-service communication
- Demonstrate the complete order flow
- Validate API endpoints

## Notes

- The application uses dynamic types for JSON deserialization for simplicity
- All API calls are asynchronous
- The application checks service availability on startup
- Error messages guide users if services are not running
