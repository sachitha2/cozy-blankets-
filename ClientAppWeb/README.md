# Cozy Comfort Web Client Application

## Overview
Web-based client application built with ASP.NET Core MVC that provides a beautiful, modern UI for consuming all three Cozy Comfort services. This runs in a web browser and provides a desktop-like experience.

## Features
- ✅ Modern, responsive web UI
- ✅ Real-time service status checking
- ✅ View all blanket models from ManufacturerService
- ✅ Check stock levels
- ✅ View distributor inventory
- ✅ Check product availability
- ✅ Place customer orders
- ✅ Complete order flow demonstration
- ✅ Tabbed interface for organized data display
- ✅ Beautiful gradient design

## Prerequisites
- .NET 7.0 SDK or higher
- All three services must be running:
  - ManufacturerService on port 5001
  - DistributorService on port 5002
  - SellerService on port 5003

## Building and Running

### Build the Application
```bash
cd ClientAppWeb
dotnet restore
dotnet build
```

### Run the Application
```bash
dotnet run
```

The application will start on **http://localhost:5006**

Open your browser and navigate to: **http://localhost:5006**

## UI Features

### Layout
- **Header**: Title and branding with gradient background
- **Status Bar**: Shows current operation status and service statuses
- **Left Sidebar**: Action buttons organized by service
- **Order Form**: Quick order placement form
- **Tabbed Content**: Four tabs showing Blankets, Inventory, Stock Info, and Availability

### Available Actions
1. **Check Services** - Verifies all three services are running
2. **View Blankets** - Loads and displays all blanket models
3. **Check Stock** - Checks stock for blanket model ID 1
4. **View Inventory** - Shows distributor inventory
5. **Check Availability** - Checks product availability through SellerService
6. **Place Order** - Places a customer order via form
7. **Complete Demo** - Runs a complete order flow demonstration

## Architecture
- **MVC Pattern** - Model-View-Controller architecture
- **HTTP Client Factory** - For API communication
- **Razor Views** - Server-side rendering with modern CSS
- **Form-based Actions** - POST requests for all operations

## Technology Stack
- **ASP.NET Core MVC** - Web framework
- **Razor Pages** - View engine
- **System.Net.Http.Json** - HTTP client for API communication
- **Modern CSS** - Gradient design with responsive layout

## Troubleshooting

### Application Won't Start
- Ensure .NET 7.0 SDK is installed
- Run `dotnet restore` to restore packages
- Check that port 5006 is available

### Services Not Connecting
- Verify services are running on correct ports (5001, 5002, 5003)
- Check firewall settings
- Ensure services are accessible via http://localhost

### Browser Issues
- Use a modern browser (Chrome, Firefox, Safari, Edge)
- Clear browser cache if UI doesn't load correctly
- Check browser console for JavaScript errors

## Notes
- The application uses server-side rendering for simplicity
- All API calls are made server-side
- The UI updates after each action
- Error messages are displayed in the status bar
