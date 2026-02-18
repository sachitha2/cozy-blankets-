using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using ClientAppWeb.Models;

namespace ClientAppWeb.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string ManufacturerServiceUrl = "http://localhost:5001";
    private const string DistributorServiceUrl = "http://localhost:5002";
    private const string SellerServiceUrl = "http://localhost:5003";

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        return View(new HomeViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> CheckServices()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel();

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
                viewModel.ServiceStatuses.Add(name, response.IsSuccessStatusCode ? "Running" : "Not Running");
            }
            catch
            {
                viewModel.ServiceStatuses.Add(name, "Not Running");
            }
        }

        return View("Index", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> LoadBlankets()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel();

        try
        {
            var response = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (response.IsSuccessStatusCode)
            {
                viewModel.Blankets = await response.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
                viewModel.StatusMessage = $"Loaded {viewModel.Blankets.Count} blanket models";
            }
            else
            {
                viewModel.StatusMessage = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
        }

        return View("Index", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CheckStock(int blanketId)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel();

        try
        {
            var response = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets/stock/{blanketId}");
            if (response.IsSuccessStatusCode)
            {
                viewModel.StockInfo = await response.Content.ReadFromJsonAsync<StockModel>();
                viewModel.StatusMessage = $"Stock checked for model {blanketId}";
            }
            else
            {
                viewModel.StatusMessage = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
        }

        return View("Index", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> LoadInventory()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel();

        try
        {
            var response = await httpClient.GetAsync($"{DistributorServiceUrl}/api/inventory");
            if (response.IsSuccessStatusCode)
            {
                viewModel.Inventory = await response.Content.ReadFromJsonAsync<List<InventoryModel>>() ?? new();
                viewModel.StatusMessage = $"Loaded {viewModel.Inventory.Count} inventory items";
            }
            else
            {
                viewModel.StatusMessage = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
        }

        return View("Index", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CheckAvailability(int blanketId)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel();

        try
        {
            var response = await httpClient.GetAsync($"{SellerServiceUrl}/api/availability/{blanketId}");
            if (response.IsSuccessStatusCode)
            {
                viewModel.AvailabilityInfo = await response.Content.ReadFromJsonAsync<AvailabilityModel>();
                viewModel.StatusMessage = $"Availability checked for model {blanketId}";
            }
            else
            {
                viewModel.StatusMessage = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
        }

        return View("Index", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(OrderRequestModel request)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel();

        try
        {
            var order = new
            {
                customerName = request.CustomerName,
                customerEmail = request.CustomerEmail,
                customerPhone = request.CustomerPhone ?? "123-456-7890",
                shippingAddress = request.ShippingAddress,
                items = new[]
                {
                    new { blanketId = request.BlanketId, quantity = request.Quantity }
                }
            };

            var response = await httpClient.PostAsJsonAsync($"{SellerServiceUrl}/api/customerorder", order);
            if (response.IsSuccessStatusCode)
            {
                viewModel.OrderResponse = await response.Content.ReadFromJsonAsync<OrderResponseModel>();
                viewModel.StatusMessage = $"Order {viewModel.OrderResponse?.OrderId} placed successfully!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error: {response.StatusCode} - {error}";
            }
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
        }

        return View("Index", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CompleteDemo()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel();

        try
        {
            // Step 1: Load blankets
            var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (blanketsResponse.IsSuccessStatusCode)
            {
                viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }

            // Step 2: Check stock
            var stockResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets/stock/1");
            if (stockResponse.IsSuccessStatusCode)
            {
                viewModel.StockInfo = await stockResponse.Content.ReadFromJsonAsync<StockModel>();
            }

            // Step 3: Load inventory
            var inventoryResponse = await httpClient.GetAsync($"{DistributorServiceUrl}/api/inventory");
            if (inventoryResponse.IsSuccessStatusCode)
            {
                viewModel.Inventory = await inventoryResponse.Content.ReadFromJsonAsync<List<InventoryModel>>() ?? new();
            }

            // Step 4: Check availability
            var availabilityResponse = await httpClient.GetAsync($"{SellerServiceUrl}/api/availability/1");
            if (availabilityResponse.IsSuccessStatusCode)
            {
                viewModel.AvailabilityInfo = await availabilityResponse.Content.ReadFromJsonAsync<AvailabilityModel>();
            }

            // Step 5: Place sample order
            var order = new
            {
                customerName = "Demo Customer",
                customerEmail = "demo@example.com",
                customerPhone = "555-1234",
                shippingAddress = "123 Demo Street",
                items = new[] { new { blanketId = 1, quantity = 2 } }
            };

            var orderResponse = await httpClient.PostAsJsonAsync($"{SellerServiceUrl}/api/customerorder", order);
            if (orderResponse.IsSuccessStatusCode)
            {
                viewModel.OrderResponse = await orderResponse.Content.ReadFromJsonAsync<OrderResponseModel>();
            }

            viewModel.StatusMessage = "Complete demo executed successfully!";
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Demo error: {ex.Message}";
        }

        return View("Index", viewModel);
    }
}
