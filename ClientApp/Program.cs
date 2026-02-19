using System.Net.Http.Json;
using System.Text.Json;

namespace ClientApp;

/// <summary>
/// Console client application to demonstrate consuming Cozy Comfort services
/// This application demonstrates the complete order flow through all services
/// </summary>
class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static string ManufacturerServiceUrl => Environment.GetEnvironmentVariable("Services__ManufacturerServiceUrl") ?? "http://localhost:5001";
    private static string DistributorServiceUrl => Environment.GetEnvironmentVariable("Services__DistributorServiceUrl") ?? "http://localhost:5002";
    private static string SellerServiceUrl => Environment.GetEnvironmentVariable("Services__SellerServiceUrl") ?? "http://localhost:5003";

    static async Task Main(string[] args)
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("  Cozy Comfort - Client Application");
        Console.WriteLine("  Service-Oriented Architecture Demo");
        Console.WriteLine("==========================================\n");

        try
        {
            // Check if services are running
            await CheckServicesAvailability();

            // Display menu
            while (true)
            {
                DisplayMenu();
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await DisplayAllBlankets();
                        break;
                    case "2":
                        await CheckStock();
                        break;
                    case "3":
                        await CheckDistributorInventory();
                        break;
                    case "4":
                        await CheckAvailability();
                        break;
                    case "5":
                        await PlaceCustomerOrder();
                        break;
                    case "6":
                        await CompleteOrderFlowDemo();
                        break;
                    case "7":
                        await CheckProductionCapacity();
                        break;
                    case "0":
                        Console.WriteLine("\nThank you for using Cozy Comfort Client App!");
                        return;
                    default:
                        Console.WriteLine("\nInvalid choice. Please try again.\n");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine("Make sure all services are running:\n");
            Console.WriteLine("1. ManufacturerService on port 5001");
            Console.WriteLine("2. DistributorService on port 5002");
            Console.WriteLine("3. SellerService on port 5003");
        }
    }

    static void DisplayMenu()
    {
        Console.WriteLine("\n--- Main Menu ---");
        Console.WriteLine("1. View All Blanket Models (ManufacturerService)");
        Console.WriteLine("2. Check Stock for a Model (ManufacturerService)");
        Console.WriteLine("3. View Distributor Inventory (DistributorService)");
        Console.WriteLine("4. Check Product Availability (SellerService)");
        Console.WriteLine("5. Place Customer Order (SellerService)");
        Console.WriteLine("6. Complete Order Flow Demo");
        Console.WriteLine("7. Check Production Capacity (ManufacturerService)");
        Console.WriteLine("0. Exit");
        Console.Write("\nEnter your choice: ");
    }

    static async Task CheckServicesAvailability()
    {
        Console.WriteLine("Checking service availability...\n");
        
        var services = new[]
        {
            ("ManufacturerService", ManufacturerServiceUrl),
            ("DistributorService", DistributorServiceUrl),
            ("SellerService", SellerServiceUrl)
        };

        foreach (var (name, url) in services)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                Console.WriteLine($"✓ {name} is running at {url}");
            }
            catch
            {
                Console.WriteLine($"✗ {name} is NOT running at {url}");
            }
        }
        Console.WriteLine();
    }

    static async Task DisplayAllBlankets()
    {
        Console.WriteLine("\n--- All Blanket Models ---");
        try
        {
            var response = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (response.IsSuccessStatusCode)
            {
                var blankets = await response.Content.ReadFromJsonAsync<List<dynamic>>();
                if (blankets != null && blankets.Any())
                {
                    Console.WriteLine($"\nFound {blankets.Count} blanket models:\n");
                    foreach (var blanket in blankets)
                    {
                        Console.WriteLine($"  ID: {blanket.Id}");
                        Console.WriteLine($"  Model: {blanket.ModelName}");
                        Console.WriteLine($"  Material: {blanket.Material}");
                        Console.WriteLine($"  Price: ${blanket.UnitPrice}");
                        Console.WriteLine($"  Description: {blanket.Description}");
                        Console.WriteLine();
                    }
                }
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task CheckStock()
    {
        Console.Write("\nEnter Blanket Model ID: ");
        if (int.TryParse(Console.ReadLine(), out int modelId))
        {
            try
            {
                var response = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets/stock/{modelId}");
                if (response.IsSuccessStatusCode)
                {
                    var stock = await response.Content.ReadFromJsonAsync<dynamic>();
                    Console.WriteLine("\n--- Stock Information ---");
                    Console.WriteLine($"Model: {stock?.ModelName}");
                    Console.WriteLine($"Total Quantity: {stock?.Quantity}");
                    Console.WriteLine($"Reserved: {stock?.ReservedQuantity}");
                    Console.WriteLine($"Available: {stock?.AvailableQuantity}");
                    Console.WriteLine($"Last Updated: {stock?.LastUpdated}");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"Model ID {modelId} not found.");
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Invalid model ID.");
        }
    }

    static async Task CheckDistributorInventory()
    {
        Console.WriteLine("\n--- Distributor Inventory ---");
        try
        {
            var response = await httpClient.GetAsync($"{DistributorServiceUrl}/api/inventory");
            if (response.IsSuccessStatusCode)
            {
                var inventory = await response.Content.ReadFromJsonAsync<List<dynamic>>();
                if (inventory != null && inventory.Any())
                {
                    Console.WriteLine($"\nFound {inventory.Count} inventory items:\n");
                    foreach (var item in inventory)
                    {
                        Console.WriteLine($"  Model: {item.ModelName} (ID: {item.BlanketId})");
                        Console.WriteLine($"  Quantity: {item.Quantity}");
                        Console.WriteLine($"  Available: {item.AvailableQuantity}");
                        Console.WriteLine($"  Unit Cost: ${item.UnitCost}");
                        Console.WriteLine();
                    }
                }
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task CheckAvailability()
    {
        Console.Write("\nEnter Blanket Model ID: ");
        if (int.TryParse(Console.ReadLine(), out int modelId))
        {
            try
            {
                var response = await httpClient.GetAsync($"{SellerServiceUrl}/api/availability/{modelId}");
                if (response.IsSuccessStatusCode)
                {
                    var availability = await response.Content.ReadFromJsonAsync<dynamic>();
                    Console.WriteLine("\n--- Availability Information ---");
                    Console.WriteLine($"Model: {availability?.ModelName}");
                    Console.WriteLine($"Available: {(availability?.IsAvailable == true ? "Yes" : "No")}");
                    Console.WriteLine($"Available Quantity: {availability?.AvailableQuantity}");
                    Console.WriteLine($"Message: {availability?.Message}");
                    if (availability?.EstimatedDeliveryDays != null)
                    {
                        Console.WriteLine($"Estimated Delivery: {availability.EstimatedDeliveryDays} days");
                    }
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Invalid model ID.");
        }
    }

    static async Task PlaceCustomerOrder()
    {
        Console.WriteLine("\n--- Place Customer Order ---");
        Console.Write("Customer Name: ");
        var customerName = Console.ReadLine();
        Console.Write("Customer Email: ");
        var customerEmail = Console.ReadLine();
        Console.Write("Shipping Address: ");
        var shippingAddress = Console.ReadLine();
        Console.Write("Blanket Model ID: ");
        if (int.TryParse(Console.ReadLine(), out int blanketId))
        {
            Console.Write("Quantity: ");
            if (int.TryParse(Console.ReadLine(), out int quantity))
            {
                var order = new
                {
                    customerName = customerName ?? "Test Customer",
                    customerEmail = customerEmail ?? "test@example.com",
                    customerPhone = "123-456-7890",
                    shippingAddress = shippingAddress ?? "123 Main St",
                    items = new[]
                    {
                        new { blanketId = blanketId, quantity = quantity }
                    }
                };

                try
                {
                    var response = await httpClient.PostAsJsonAsync(
                        $"{SellerServiceUrl}/api/customerorder", order);

                    if (response.IsSuccessStatusCode)
                    {
                        var orderResponse = await response.Content.ReadFromJsonAsync<dynamic>();
                        Console.WriteLine("\n--- Order Response ---");
                        Console.WriteLine($"Order ID: {orderResponse?.OrderId}");
                        Console.WriteLine($"Status: {orderResponse?.Status}");
                        Console.WriteLine($"Message: {orderResponse?.Message}");
                        Console.WriteLine($"Total Amount: ${orderResponse?.TotalAmount}");
                        Console.WriteLine("\nItem Status:");
                        if (orderResponse?.Items != null)
                            foreach (var item in orderResponse.Items)
                            {
                                Console.WriteLine($"  - {item?.ModelName}: {item?.Status} - {item?.Message}");
                            }
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error: {response.StatusCode} - {error}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid quantity.");
            }
        }
        else
        {
            Console.WriteLine("Invalid blanket ID.");
        }
    }

    static async Task CompleteOrderFlowDemo()
    {
        Console.WriteLine("\n==========================================");
        Console.WriteLine("  Complete Order Flow Demonstration");
        Console.WriteLine("==========================================\n");

        // Step 1: Display available blankets
        Console.WriteLine("Step 1: Viewing available blanket models...");
        await DisplayAllBlankets();
        await Task.Delay(1000);

        // Step 2: Check manufacturer stock
        Console.WriteLine("\nStep 2: Checking manufacturer stock for Model ID 1...");
        try
        {
            var stockResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets/stock/1");
            if (stockResponse.IsSuccessStatusCode)
            {
                var stock = await stockResponse.Content.ReadFromJsonAsync<dynamic>();
                Console.WriteLine($"  Available: {stock?.AvailableQuantity} units");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
        await Task.Delay(1000);

        // Step 3: Check distributor inventory
        Console.WriteLine("\nStep 3: Checking distributor inventory...");
        await CheckDistributorInventory();
        await Task.Delay(1000);

        // Step 4: Check availability through seller service
        Console.WriteLine("\nStep 4: Checking product availability (SellerService)...");
        try
        {
            var availabilityResponse = await httpClient.GetAsync($"{SellerServiceUrl}/api/availability/1");
            if (availabilityResponse.IsSuccessStatusCode)
            {
                var availability = await availabilityResponse.Content.ReadFromJsonAsync<dynamic>();
                Console.WriteLine($"  Available: {availability?.IsAvailable}");
                Console.WriteLine($"  Available Quantity: {availability?.AvailableQuantity}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
        await Task.Delay(1000);

        // Step 5: Place a sample order
        Console.WriteLine("\nStep 5: Placing a sample customer order...");
        var sampleOrder = new
        {
            customerName = "Demo Customer",
            customerEmail = "demo@example.com",
            customerPhone = "555-1234",
            shippingAddress = "123 Demo Street, Demo City",
            items = new[]
            {
                new { blanketId = 1, quantity = 2 }
            }
        };

        try
        {
            var orderResponse = await httpClient.PostAsJsonAsync(
                $"{SellerServiceUrl}/api/customerorder", sampleOrder);

            if (orderResponse.IsSuccessStatusCode)
            {
                var result = await orderResponse.Content.ReadFromJsonAsync<dynamic>();
                Console.WriteLine($"  Order ID: {result?.OrderId}");
                Console.WriteLine($"  Status: {result?.Status}");
                Console.WriteLine($"  Message: {result?.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }

        Console.WriteLine("\n==========================================");
        Console.WriteLine("  Demo Complete!");
        Console.WriteLine("==========================================\n");
    }

    static async Task CheckProductionCapacity()
    {
        Console.Write("\nEnter Blanket Model ID: ");
        if (int.TryParse(Console.ReadLine(), out int blanketId))
        {
            Console.Write("Enter Quantity Needed: ");
            if (int.TryParse(Console.ReadLine(), out int quantity))
            {
                var request = new
                {
                    blanketId = blanketId,
                    quantity = quantity
                };

                try
                {
                    var response = await httpClient.PostAsJsonAsync(
                        $"{ManufacturerServiceUrl}/api/blankets/produce", request);

                    if (response.IsSuccessStatusCode)
                    {
                        var production = await response.Content.ReadFromJsonAsync<dynamic>();
                        Console.WriteLine("\n--- Production Capacity Check ---");
                        Console.WriteLine($"Can Produce: {(production?.CanProduce == true ? "Yes" : "No")}");
                        Console.WriteLine($"Available Stock: {production?.AvailableStock}");
                        Console.WriteLine($"Lead Time: {production?.LeadTimeDays} days");
                        if (production?.EstimatedCompletionDate != null)
                        {
                            Console.WriteLine($"Estimated Completion: {production.EstimatedCompletionDate}");
                        }
                        Console.WriteLine($"Message: {production?.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid quantity.");
            }
        }
        else
        {
            Console.WriteLine("Invalid model ID.");
        }
    }
}
