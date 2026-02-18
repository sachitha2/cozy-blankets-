# Cozy Comfort Desktop Client Application

## Overview
Cross-platform desktop application built with Avalonia UI that provides a graphical interface for consuming all three Cozy Comfort services (ManufacturerService, DistributorService, and SellerService).

## Features
- ✅ Modern, responsive desktop UI
- ✅ Real-time service status checking
- ✅ View all blanket models from ManufacturerService
- ✅ Check stock levels
- ✅ View distributor inventory
- ✅ Check product availability
- ✅ Place customer orders
- ✅ Complete order flow demonstration
- ✅ Order history tracking
- ✅ Cross-platform (Windows, macOS, Linux)

## Prerequisites
- .NET 7.0 SDK or higher
- All three services must be running:
  - ManufacturerService on port 5001
  - DistributorService on port 5002
  - SellerService on port 5003

## Building and Running

### Build the Application
```bash
cd ClientAppDesktop
dotnet restore
dotnet build
```

### Run the Application
```bash
dotnet run
```

### Build for Release
```bash
dotnet publish -c Release -r osx-x64  # For macOS
dotnet publish -c Release -r win-x64  # For Windows
dotnet publish -c Release -r linux-x64 # For Linux
```

## UI Features

### Main Window Layout
- **Left Sidebar**: Action buttons for all service operations
- **Top Status Bar**: Shows current operation status and loading indicator
- **Order Form**: Quick order placement form
- **Data Tabs**: Three tabs showing Blankets, Inventory, and Orders

### Available Actions
1. **Check Services** - Verifies all three services are running
2. **View Blankets** - Loads and displays all blanket models
3. **Check Stock** - Checks stock for selected blanket model
4. **View Inventory** - Shows distributor inventory
5. **Check Availability** - Checks product availability through SellerService
6. **Place Order** - Places a customer order
7. **Complete Demo** - Runs a complete order flow demonstration

## Architecture
- **MVVM Pattern** - Uses ReactiveUI for MVVM implementation
- **Reactive Commands** - All actions use reactive commands for async operations
- **HTTP Client** - Communicates with services via REST APIs
- **Data Binding** - Uses Avalonia data binding for UI updates

## Technology Stack
- **Avalonia UI** - Cross-platform UI framework
- **ReactiveUI** - MVVM framework with reactive extensions
- **System.Net.Http.Json** - HTTP client for API communication
- **Newtonsoft.Json** - JSON serialization

## Troubleshooting

### Application Won't Start
- Ensure .NET 7.0 SDK is installed
- Run `dotnet restore` to restore packages
- Check that all services are running

### Services Not Connecting
- Verify services are running on correct ports (5001, 5002, 5003)
- Check firewall settings
- Ensure services are accessible via http://localhost

### UI Not Displaying Correctly
- Try rebuilding the application
- Clear bin/obj folders and rebuild
- Check Avalonia UI version compatibility

## Notes
- The application uses reactive programming patterns for async operations
- All API calls are non-blocking and use async/await
- The UI automatically updates when data changes
- Error messages are displayed in the status bar
