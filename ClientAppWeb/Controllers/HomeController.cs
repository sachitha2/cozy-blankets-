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
                viewModel.ActiveTab = "blankets";
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
                viewModel.ActiveTab = "stock";
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
                viewModel.ActiveTab = "inventory";
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
                viewModel.ActiveTab = "availability";
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
                
                // Automatically load orders after placing
                var ordersResponse = await httpClient.GetAsync($"{SellerServiceUrl}/api/customerorder");
                if (ordersResponse.IsSuccessStatusCode)
                {
                    viewModel.CustomerOrders = await ordersResponse.Content.ReadFromJsonAsync<List<CustomerOrderModel>>() ?? new();
                    viewModel.ActiveTab = "orders";
                }
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
    public async Task<IActionResult> LoadCustomerOrders()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel();

        try
        {
            var response = await httpClient.GetAsync($"{SellerServiceUrl}/api/customerorder");
            if (response.IsSuccessStatusCode)
            {
                viewModel.CustomerOrders = await response.Content.ReadFromJsonAsync<List<CustomerOrderModel>>() ?? new();
                viewModel.StatusMessage = $"Loaded {viewModel.CustomerOrders.Count} customer orders";
                viewModel.ActiveTab = "orders";
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
    public async Task<IActionResult> ViewOrder(int orderId)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel();

        try
        {
            var response = await httpClient.GetAsync($"{SellerServiceUrl}/api/customerorder/{orderId}");
            if (response.IsSuccessStatusCode)
            {
                viewModel.SelectedOrder = await response.Content.ReadFromJsonAsync<CustomerOrderModel>();
                viewModel.StatusMessage = $"Loaded order {orderId} details";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                viewModel.StatusMessage = $"Order {orderId} not found";
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
}
