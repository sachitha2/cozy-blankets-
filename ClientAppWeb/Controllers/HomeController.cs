using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using ClientAppWeb.Models;

namespace ClientAppWeb.Controllers;

public class HomeController : Controller
{
    private const string CartSessionKey = "Cart";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public HomeController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _webHostEnvironment = webHostEnvironment;
    }

    private string ManufacturerServiceUrl => _configuration["Services:ManufacturerServiceUrl"] ?? "http://localhost:5001";
    private string DistributorServiceUrl => _configuration["Services:DistributorServiceUrl"] ?? "http://localhost:5002";
    private string SellerServiceUrl => _configuration["Services:SellerServiceUrl"] ?? "http://localhost:5003";

    private List<CartItemModel> GetCart()
    {
        var bytes = HttpContext.Session.Get(CartSessionKey);
        if (bytes == null || bytes.Length == 0)
            return new List<CartItemModel>();
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<List<CartItemModel>>(json) ?? new List<CartItemModel>();
        }
        catch { return new List<CartItemModel>(); }
    }

    private void SaveCart(List<CartItemModel> cart)
    {
        var json = JsonSerializer.Serialize(cart);
        HttpContext.Session.Set(CartSessionKey, System.Text.Encoding.UTF8.GetBytes(json));
    }

    /// <summary>
    /// Customer-facing storefront: browse blankets, add to cart, checkout like a real website.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var viewModel = new HomeViewModel { Cart = GetCart() };
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (response.IsSuccessStatusCode)
            {
                viewModel.Blankets = await response.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
                viewModel.StatusMessage = viewModel.Blankets.Count > 0
                    ? "Browse our blankets and add to cart to place your order."
                    : "Welcome. Catalog loading.";
            }
            else
            {
                viewModel.StatusMessage = "Welcome to Cozy Comfort. Ensure services are running to browse.";
            }
        }
        catch
        {
            viewModel.StatusMessage = "Welcome. Start services to browse and order.";
        }

        return View(viewModel);
    }

    /// <summary>
    /// Product detail page: full info and image gallery for a blanket.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ProductDetail(int id)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets/{id}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["StatusMessage"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }
        var blanket = await response.Content.ReadFromJsonAsync<BlanketModel>();
        if (blanket == null)
        {
            TempData["StatusMessage"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(blanket);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(int blanketId, int quantity = 1)
    {
        if (quantity < 1) quantity = 1;
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets/{blanketId}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["StatusMessage"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }
        var blanket = await response.Content.ReadFromJsonAsync<BlanketModel>();
        if (blanket == null)
        {
            TempData["StatusMessage"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }
        var cart = GetCart();
        var existing = cart.FirstOrDefault(c => c.BlanketId == blanketId);
        if (existing != null)
            existing.Quantity += quantity;
        else
            cart.Add(new CartItemModel { BlanketId = blanket.Id, ModelName = blanket.ModelName, UnitPrice = blanket.UnitPrice, Quantity = quantity, ImageUrl = blanket.ImageUrl });
        SaveCart(cart);
        TempData["StatusMessage"] = $"Added {quantity} x {blanket.ModelName} to cart.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult RemoveFromCart(int blanketId)
    {
        var cart = GetCart();
        cart.RemoveAll(c => c.BlanketId == blanketId);
        SaveCart(cart);
        TempData["StatusMessage"] = "Item removed from cart.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Checkout()
    {
        var viewModel = new HomeViewModel { Cart = GetCart(), ShowCheckout = true };
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
        if (response.IsSuccessStatusCode)
            viewModel.Blankets = await response.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
        return View("Index", viewModel);
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
        var viewModel = new HomeViewModel { Cart = GetCart() };

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
    [Authorize]
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
                viewModel.StatusMessage = $"Order #{viewModel.OrderResponse?.OrderId} placed successfully!";
                viewModel.ActiveTab = "orders";

                // Keep catalog and load orders so customer can continue shopping or see their order
                var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
                if (blanketsResponse.IsSuccessStatusCode)
                    viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
                var ordersResponse = await httpClient.GetAsync($"{SellerServiceUrl}/api/customerorder");
                if (ordersResponse.IsSuccessStatusCode)
                    viewModel.CustomerOrders = await ordersResponse.Content.ReadFromJsonAsync<List<CustomerOrderModel>>() ?? new();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Order could not be placed: {response.StatusCode}";
            }

            // Keep catalog so customer can try again
            try
            {
                var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
                if (blanketsResponse.IsSuccessStatusCode)
                    viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }
            catch { /* ignore */ }
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            try
            {
                var client = _httpClientFactory.CreateClient();
                var blanketsResponse = await client.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
                if (blanketsResponse.IsSuccessStatusCode)
                    viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }
            catch { /* ignore */ }
        }

        return View("Index", viewModel);
    }

    /// <summary>
    /// Place order from cart (checkout flow). Uses cart from session and customer details from form.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PlaceOrderFromCart(OrderRequestModel request)
    {
        var cart = GetCart();
        if (cart == null || !cart.Any())
        {
            TempData["StatusMessage"] = "Your cart is empty. Add items before checkout.";
            return RedirectToAction(nameof(Index));
        }

        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = new List<CartItemModel>() };

        try
        {
            var order = new
            {
                customerName = request.CustomerName,
                customerEmail = request.CustomerEmail,
                customerPhone = request.CustomerPhone ?? "",
                shippingAddress = request.ShippingAddress,
                items = cart.Select(c => new { blanketId = c.BlanketId, quantity = c.Quantity }).ToArray()
            };

            var response = await httpClient.PostAsJsonAsync($"{SellerServiceUrl}/api/customerorder", order);
            if (response.IsSuccessStatusCode)
            {
                viewModel.OrderResponse = await response.Content.ReadFromJsonAsync<OrderResponseModel>();
                viewModel.StatusMessage = $"Order #{viewModel.OrderResponse?.OrderId} placed successfully!";
                viewModel.ActiveTab = "orders";
                SaveCart(new List<CartItemModel>()); // clear cart
                var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
                if (blanketsResponse.IsSuccessStatusCode)
                    viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
                var ordersResponse = await httpClient.GetAsync($"{SellerServiceUrl}/api/customerorder");
                if (ordersResponse.IsSuccessStatusCode)
                    viewModel.CustomerOrders = await ordersResponse.Content.ReadFromJsonAsync<List<CustomerOrderModel>>() ?? new();
            }
            else
            {
                viewModel.Cart = cart;
                viewModel.ShowCheckout = true;
                viewModel.StatusMessage = $"Order could not be placed: {response.StatusCode}";
                var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
                if (blanketsResponse.IsSuccessStatusCode)
                    viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }
        }
        catch (Exception ex)
        {
            viewModel.Cart = cart;
            viewModel.ShowCheckout = true;
            viewModel.StatusMessage = $"Error: {ex.Message}";
            try
            {
                var res = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
                if (res.IsSuccessStatusCode)
                    viewModel.Blankets = await res.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }
            catch { /* ignore */ }
        }

        return View("Index", viewModel);
    }

    [HttpPost]
    [Authorize]
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
    public async Task<IActionResult> SetBlanketImageUrl(int blanketId, string imageUrl, int? returnToDetail)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PatchAsJsonAsync($"{ManufacturerServiceUrl}/api/blankets/{blanketId}/image", new { imageUrl });
        if (response.IsSuccessStatusCode)
            TempData["StatusMessage"] = "Image URL updated.";
        else
            TempData["StatusMessage"] = "Failed to update image URL.";
        if (returnToDetail.HasValue)
            return RedirectToAction(nameof(ProductDetail), new { id = returnToDetail.Value });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadBlanketImage(int blanketId, IFormFile imageFile, int? returnToDetail)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            TempData["StatusMessage"] = "Please select an image file.";
            return returnToDetail.HasValue ? RedirectToAction(nameof(ProductDetail), new { id = returnToDetail.Value }) : RedirectToAction(nameof(Index));
        }
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
        {
            TempData["StatusMessage"] = "Allowed formats: JPG, PNG, GIF, WebP.";
            return returnToDetail.HasValue ? RedirectToAction(nameof(ProductDetail), new { id = returnToDetail.Value }) : RedirectToAction(nameof(Index));
        }
        var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath ?? "wwwroot", "images", "blankets");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"blanket-{blanketId}-{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
            await imageFile.CopyToAsync(stream);
        var imageUrl = $"/images/blankets/{fileName}";
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PatchAsJsonAsync($"{ManufacturerServiceUrl}/api/blankets/{blanketId}/image", new { imageUrl });
        if (response.IsSuccessStatusCode)
            TempData["StatusMessage"] = "Image uploaded and set for product.";
        else
            TempData["StatusMessage"] = "Image saved but failed to update product.";
        if (returnToDetail.HasValue)
            return RedirectToAction(nameof(ProductDetail), new { id = returnToDetail.Value });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AddAdditionalImageUrl(int blanketId, string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            TempData["StatusMessage"] = "Please enter an image URL.";
            return RedirectToAction(nameof(ProductDetail), new { id = blanketId });
        }
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync($"{ManufacturerServiceUrl}/api/blankets/{blanketId}/images", new { imageUrl = imageUrl.Trim() });
        if (response.IsSuccessStatusCode)
            TempData["StatusMessage"] = "Image added to gallery.";
        else
            TempData["StatusMessage"] = "Failed to add image.";
        return RedirectToAction(nameof(ProductDetail), new { id = blanketId });
    }

    [HttpPost]
    public async Task<IActionResult> UploadAdditionalImage(int blanketId, IFormFile imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            TempData["StatusMessage"] = "Please select an image file.";
            return RedirectToAction(nameof(ProductDetail), new { id = blanketId });
        }
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
        {
            TempData["StatusMessage"] = "Allowed formats: JPG, PNG, GIF, WebP.";
            return RedirectToAction(nameof(ProductDetail), new { id = blanketId });
        }
        var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath ?? "wwwroot", "images", "blankets");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"blanket-{blanketId}-{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
            await imageFile.CopyToAsync(stream);
        var imageUrl = $"/images/blankets/{fileName}";
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync($"{ManufacturerServiceUrl}/api/blankets/{blanketId}/images", new { imageUrl });
        if (response.IsSuccessStatusCode)
            TempData["StatusMessage"] = "Image added to gallery.";
        else
            TempData["StatusMessage"] = "Failed to add image.";
        return RedirectToAction(nameof(ProductDetail), new { id = blanketId });
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
