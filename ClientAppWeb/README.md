# Cozy Comfort Web Client Application

## Overview
Customer-facing website where **customers can view the store and place orders**, aligned with the assessment PDF: *"Seller: The point of contact for the end customer. They display blankets for sale (online or in physical stores), take customer orders."*

Built with ASP.NET Core MVC. The site loads the blanket catalog on first view and lets customers place orders through the Seller (SellerService).

## Features
- ✅ **Customer storefront**: Browse blankets, check availability, place orders (PDF-aligned)
- ✅ Catalog loaded on page load so customers can view products immediately
- ✅ Place order form with blanket dropdown (by product name and price)
- ✅ My orders tab to view placed orders
- ✅ Admin/Demo section: service status, manufacturer stock, distributor inventory
- ✅ Modern, responsive web UI

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

## UI Features (customer-first)

### Layout
- **Header**: “Cozy Comfort Blankets” – shop and place your order
- **Status bar**: Message and (when used) service status
- **Sidebar – Shop**: Browse blankets, Check availability, My orders
- **Sidebar – Admin / Demo**: Check services, Manufacturer stock, Distributor inventory
- **Order form**: Your name, Email, Phone (optional), Shipping address, Blanket (dropdown from catalog), Quantity, Place order
- **Tabs**: Our blankets | Availability | My orders | Inventory | Stock

### Customer flow (PDF-aligned)
1. **View the website** – Open the app; catalog loads so customers see blankets for sale.
2. **Browse blankets** – “Our blankets” tab shows product list (or use **Browse blankets** in sidebar).
3. **Check availability** – Optional; use **Check availability** for a product.
4. **Place order** – Fill the form, choose a blanket from the dropdown, submit. Order is sent to SellerService (seller takes customer orders).
5. **My orders** – View order history and status.

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
