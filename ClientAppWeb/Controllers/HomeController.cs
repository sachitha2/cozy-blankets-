using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using ClientAppWeb.Models;
using ClientAppWeb.Data;
using System.Text.Json.Serialization;

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

    /// <summary>Gets the current user's email from claims (for filtering "my orders").</summary>
    private string? GetCurrentCustomerEmail() => User.FindFirstValue(ClaimTypes.Email);

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
    /// Parses API error response (RFC 7807 detail or controller error) and returns a safe message for the user.
    /// </summary>
    private static string GetOrderErrorMessage(System.Net.HttpStatusCode statusCode, string responseBody)
    {
        const int maxPlainLength = 200;
        if (string.IsNullOrWhiteSpace(responseBody))
            return $"Order could not be placed: {statusCode}";
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("detail", out var detail))
                return $"Order could not be placed: {detail.GetString()?.Trim() ?? statusCode.ToString()}";
            if (root.TryGetProperty("error", out var err))
                return $"Order could not be placed: {err.GetString()?.Trim() ?? statusCode.ToString()}";
        }
        catch { /* not JSON */ }
        var safe = responseBody.Length <= maxPlainLength ? responseBody : responseBody[..maxPlainLength] + "...";
        return $"Order could not be placed: {safe.Trim()}";
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

            // Auto-load seller inventory when Seller logs in
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Seller"))
            {
                try
                {
                    var inventoryResponse = await httpClient.GetAsync($"{SellerServiceUrl}/api/inventory");
                    if (inventoryResponse.IsSuccessStatusCode)
                    {
                        viewModel.SellerInventory = await inventoryResponse.Content.ReadFromJsonAsync<List<InventoryModel>>() ?? new();
                        viewModel.ActiveTab = "inventory";
                        if (viewModel.SellerInventory.Any())
                        {
                            viewModel.StatusMessage = $"Welcome! You have {viewModel.SellerInventory.Count} inventory item(s). Use the sidebar to view blankets, create orders, or manage orders.";
                        }
                        else
                        {
                            viewModel.StatusMessage = "Welcome! Use the sidebar to view blankets, create orders, or manage orders.";
                        }
                    }
                }
                catch
                {
                    // If seller inventory fails to load, continue with normal page
                }
            }
        }
        catch
        {
            viewModel.StatusMessage = "Welcome. Start services to browse and order.";
        }

        return View(viewModel);
    }

    /// <summary>
    /// About Us page: company information and team details.
    /// </summary>
    [HttpGet]
    public IActionResult About()
    {
        return View();
    }

    /// <summary>
    /// Contact Us page: display contact form.
    /// </summary>
    [HttpGet]
    public IActionResult Contact()
    {
        return View(new ContactViewModel());
    }

    /// <summary>
    /// Handle contact form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var submission = new ContactSubmission
            {
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Subject = model.Subject,
                Message = model.Message,
                SubmittedAt = DateTime.UtcNow,
                IsRead = false
            };

            using var scope = HttpContext.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.ContactSubmissions.AddAsync(submission);
            await dbContext.SaveChangesAsync();

            model.StatusMessage = "Thank you for contacting us! We'll get back to you soon.";
            ModelState.Clear();
            return View(new ContactViewModel { StatusMessage = model.StatusMessage });
        }
        catch (Exception ex)
        {
            model.ErrorMessage = "An error occurred while submitting your message. Please try again later.";
            return View(model);
        }
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
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> LoadSellerInventory()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            // Preserve catalog and cart for better UX
            var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (blanketsResponse.IsSuccessStatusCode)
            {
                viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }

            var response = await httpClient.GetAsync($"{SellerServiceUrl}/api/inventory");
            if (response.IsSuccessStatusCode)
            {
                viewModel.SellerInventory = await response.Content.ReadFromJsonAsync<List<InventoryModel>>() ?? new();
                viewModel.StatusMessage = $"Loaded {viewModel.SellerInventory.Count} inventory items";
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
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> LoadSellerBlankets()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var response = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (response.IsSuccessStatusCode)
            {
                viewModel.Blankets = await response.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
                viewModel.StatusMessage = $"Loaded {viewModel.Blankets.Count} blanket(s)";
                viewModel.ActiveTab = "seller-blankets";
            }
            else
            {
                viewModel.StatusMessage = $"Error loading blankets: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
        }

        return View("Index", viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> LoadSellerOrders()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            // Preserve catalog
            var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (blanketsResponse.IsSuccessStatusCode)
            {
                viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }

            var response = await httpClient.GetAsync($"{SellerServiceUrl}/api/customerorder");
            if (response.IsSuccessStatusCode)
            {
                viewModel.CustomerOrders = await response.Content.ReadFromJsonAsync<List<CustomerOrderModel>>() ?? new();
                viewModel.StatusMessage = $"Loaded {viewModel.CustomerOrders.Count} order(s)";
                viewModel.ActiveTab = "seller-orders";
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
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> CreateCustomerOrder(string customerName, string customerEmail, string? customerPhone, string shippingAddress, string orderItemsJson)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            // Preserve catalog
            var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (blanketsResponse.IsSuccessStatusCode)
            {
                viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }

            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerEmail) || string.IsNullOrWhiteSpace(shippingAddress))
            {
                viewModel.StatusMessage = "Customer Name, Email, and Shipping Address are required.";
                viewModel.ActiveTab = "create-order";
                return View("Index", viewModel);
            }

            // Parse order items from JSON
            var items = new List<object>();
            try
            {
                if (!string.IsNullOrWhiteSpace(orderItemsJson))
                {
                    using var doc = JsonDocument.Parse(orderItemsJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in doc.RootElement.EnumerateArray())
                        {
                            if (item.TryGetProperty("blanketId", out var bid) && item.TryGetProperty("quantity", out var qty))
                            {
                                items.Add(new { blanketId = bid.GetInt32(), quantity = qty.GetInt32() });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Invalid JSON, will be caught by empty check below
            }

            if (!items.Any())
            {
                viewModel.StatusMessage = "Order must contain at least one item.";
                viewModel.ActiveTab = "create-order";
                return View("Index", viewModel);
            }

            var order = new
            {
                customerName = customerName.Trim(),
                customerEmail = customerEmail.Trim(),
                customerPhone = customerPhone?.Trim() ?? "",
                shippingAddress = shippingAddress.Trim(),
                items = items
            };

            var response = await httpClient.PostAsJsonAsync($"{SellerServiceUrl}/api/customerorder", order);
            if (response.IsSuccessStatusCode)
            {
                viewModel.OrderResponse = await response.Content.ReadFromJsonAsync<OrderResponseModel>();
                viewModel.StatusMessage = $"Order #{viewModel.OrderResponse?.OrderId} created successfully!";
                viewModel.ActiveTab = "seller-orders";

                // Reload orders
                var ordersResponse = await httpClient.GetAsync($"{SellerServiceUrl}/api/customerorder");
                if (ordersResponse.IsSuccessStatusCode)
                {
                    viewModel.CustomerOrders = await ordersResponse.Content.ReadFromJsonAsync<List<CustomerOrderModel>>() ?? new();
                }
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = GetOrderErrorMessage(response.StatusCode, errorBody);
                viewModel.ActiveTab = "create-order";
            }
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            viewModel.ActiveTab = "create-order";
            try
            {
                var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
                if (blanketsResponse.IsSuccessStatusCode)
                    viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }
            catch { /* ignore */ }
        }

        return View("Index", viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> ViewSellerOrder(int orderId)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            // Preserve catalog
            var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (blanketsResponse.IsSuccessStatusCode)
            {
                viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }

            var response = await httpClient.GetAsync($"{SellerServiceUrl}/api/customerorder/{orderId}");
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<CustomerOrderModel>();
                if (order != null)
                {
                    viewModel.SelectedOrder = order;
                    viewModel.StatusMessage = $"Loaded order {orderId} details";
                    viewModel.ActiveTab = "seller-orders";
                }
                else
                {
                    viewModel.StatusMessage = "Order not found.";
                }
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

    [HttpPost]
    [Authorize(Roles = "Manufacturer")]
    public async Task<IActionResult> LoadManufacturerDashboard()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            // Load blankets
            var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (blanketsResponse.IsSuccessStatusCode)
            {
                viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }

            // Load manufacturer products with stock and capacity
            var products = new List<ManufacturerProductModel>();
            foreach (var blanket in viewModel.Blankets)
            {
                var product = new ManufacturerProductModel
                {
                    Id = blanket.Id,
                    ModelName = blanket.ModelName,
                    Material = blanket.Material,
                    Description = blanket.Description,
                    UnitPrice = blanket.UnitPrice,
                    ImageUrl = blanket.ImageUrl,
                    AdditionalImageUrls = blanket.AdditionalImageUrls ?? new()
                };

                // Get stock
                var stockResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets/stock/{blanket.Id}");
                if (stockResponse.IsSuccessStatusCode)
                {
                    var stock = await stockResponse.Content.ReadFromJsonAsync<StockModel>();
                    if (stock != null)
                    {
                        product.StockQuantity = stock.Quantity;
                        product.ReservedQuantity = stock.ReservedQuantity;
                        product.AvailableQuantity = stock.AvailableQuantity;
                    }
                }

                // Get capacity
                var capacityResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets/{blanket.Id}/capacity");
                if (capacityResponse.IsSuccessStatusCode)
                {
                    var capacityJson = await capacityResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(capacityJson))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(capacityJson);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("dailyCapacity", out var dc))
                                product.DailyCapacity = dc.GetInt32();
                            if (root.TryGetProperty("leadTimeDays", out var ltd))
                                product.LeadTimeDays = ltd.GetInt32();
                            if (root.TryGetProperty("isActive", out var ia))
                                product.CapacityIsActive = ia.GetBoolean();
                        }
                        catch { /* ignore parse errors */ }
                    }
                }

                products.Add(product);
            }

            viewModel.ManufacturerProducts = products;
            viewModel.StatusMessage = $"Loaded {products.Count} product(s)";
            viewModel.ActiveTab = "manufacturer";
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
        }

        return View("Index", viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Manufacturer")]
    public async Task<IActionResult> UpdateManufacturerBlanket(int id, string? modelName, string? material, string? description, decimal? unitPrice)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var updateRequest = new
            {
                modelName = modelName,
                material = material,
                description = description,
                unitPrice = unitPrice
            };

            var response = await httpClient.PutAsJsonAsync($"{ManufacturerServiceUrl}/api/blankets/{id}", updateRequest);
            if (response.IsSuccessStatusCode)
            {
                viewModel.StatusMessage = "Blanket updated successfully";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error updating blanket: {response.StatusCode} - {errorBody}";
            }

            // Reload dashboard
            return await LoadManufacturerDashboard();
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            return View("Index", viewModel);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Manufacturer")]
    public async Task<IActionResult> UpdateManufacturerStock(int blanketId, int quantity)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var stockRequest = new { quantity = quantity };
            var response = await httpClient.PatchAsJsonAsync($"{ManufacturerServiceUrl}/api/blankets/{blanketId}/stock", stockRequest);
            if (response.IsSuccessStatusCode)
            {
                viewModel.StatusMessage = "Stock updated successfully";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error updating stock: {response.StatusCode} - {errorBody}";
            }

            // Reload dashboard
            return await LoadManufacturerDashboard();
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            return View("Index", viewModel);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Manufacturer")]
    public async Task<IActionResult> UpdateManufacturerCapacity(int blanketId, int? dailyCapacity, int? leadTimeDays, bool? isActive)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var capacityRequest = new
            {
                dailyCapacity = dailyCapacity,
                leadTimeDays = leadTimeDays,
                isActive = isActive
            };

            var response = await httpClient.PatchAsJsonAsync($"{ManufacturerServiceUrl}/api/blankets/{blanketId}/capacity", capacityRequest);
            if (response.IsSuccessStatusCode)
            {
                viewModel.StatusMessage = "Production capacity updated successfully";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error updating capacity: {response.StatusCode} - {errorBody}";
            }

            // Reload dashboard
            return await LoadManufacturerDashboard();
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            return View("Index", viewModel);
        }
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
                var email = GetCurrentCustomerEmail();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var ordersUrl = $"{SellerServiceUrl}/api/customerorder/by-customer?customerEmail={Uri.EscapeDataString(email)}";
                    var ordersResponse = await httpClient.GetAsync(ordersUrl);
                    if (ordersResponse.IsSuccessStatusCode)
                        viewModel.CustomerOrders = await ordersResponse.Content.ReadFromJsonAsync<List<CustomerOrderModel>>() ?? new();
                }
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = GetOrderErrorMessage(response.StatusCode, errorBody);
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
            // Use logged-in customer's name and email; only shipping address comes from the form
            var customerName = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "";
            var customerEmail = GetCurrentCustomerEmail() ?? "";
            var order = new
            {
                customerName,
                customerEmail,
                customerPhone = "", // not collected at registration; can be added to profile later
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
                var email = GetCurrentCustomerEmail();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var ordersUrl = $"{SellerServiceUrl}/api/customerorder/by-customer?customerEmail={Uri.EscapeDataString(email)}";
                    var ordersResponse = await httpClient.GetAsync(ordersUrl);
                    if (ordersResponse.IsSuccessStatusCode)
                        viewModel.CustomerOrders = await ordersResponse.Content.ReadFromJsonAsync<List<CustomerOrderModel>>() ?? new();
                }
            }
            else
            {
                viewModel.Cart = cart;
                viewModel.ShowCheckout = true;
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = GetOrderErrorMessage(response.StatusCode, errorBody);
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
        var customerEmail = GetCurrentCustomerEmail();

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            viewModel.StatusMessage = "Cannot load orders: no email associated with your account.";
            viewModel.ActiveTab = "orders";
            return View("Index", viewModel);
        }

        try
        {
            var url = $"{SellerServiceUrl}/api/customerorder/by-customer?customerEmail={Uri.EscapeDataString(customerEmail)}";
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                viewModel.CustomerOrders = await response.Content.ReadFromJsonAsync<List<CustomerOrderModel>>() ?? new();
                viewModel.StatusMessage = $"Loaded {viewModel.CustomerOrders.Count} order(s)";
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
        
        // If user is Manufacturer, reload dashboard
        if (User.IsInRole("Manufacturer"))
            return RedirectToAction(nameof(LoadManufacturerDashboard));
        
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
            // If user is Manufacturer, reload dashboard; otherwise go to product detail
            if (User.IsInRole("Manufacturer"))
                return RedirectToAction(nameof(LoadManufacturerDashboard));
            return RedirectToAction(nameof(ProductDetail), new { id = blanketId });
        }
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync($"{ManufacturerServiceUrl}/api/blankets/{blanketId}/images", new { imageUrl = imageUrl.Trim() });
        if (response.IsSuccessStatusCode)
            TempData["StatusMessage"] = "Image added to gallery.";
        else
            TempData["StatusMessage"] = "Failed to add image.";
        
        // If user is Manufacturer, reload dashboard; otherwise go to product detail
        if (User.IsInRole("Manufacturer"))
            return RedirectToAction(nameof(LoadManufacturerDashboard));
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
    [Authorize]
    public async Task<IActionResult> ViewOrder(int orderId)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel();
        var customerEmail = GetCurrentCustomerEmail();

        try
        {
            var response = await httpClient.GetAsync($"{SellerServiceUrl}/api/customerorder/{orderId}");
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<CustomerOrderModel>();
                // Ensure the order belongs to the current customer
                if (order != null && !string.IsNullOrWhiteSpace(customerEmail) &&
                    string.Equals(order.CustomerEmail?.Trim(), customerEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    viewModel.SelectedOrder = order;
                    viewModel.StatusMessage = $"Loaded order {orderId} details";
                }
                else
                {
                    viewModel.StatusMessage = "Order not found or you do not have access to view it.";
                }
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

    // ========== Distributor Actions ==========

    [HttpGet]
    [HttpPost]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> LoadDistributorInventory()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            // Preserve catalog
            var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (blanketsResponse.IsSuccessStatusCode)
            {
                viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }

            var response = await httpClient.GetAsync($"{DistributorServiceUrl}/api/inventory");
            if (response.IsSuccessStatusCode)
            {
                viewModel.Inventory = await response.Content.ReadFromJsonAsync<List<InventoryModel>>() ?? new();
                viewModel.StatusMessage = $"Loaded {viewModel.Inventory.Count} inventory items";
                viewModel.ActiveTab = "distributor-inventory";
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
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> AddDistributorInventory(int blanketId, string modelName, int quantity, decimal unitCost)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var request = new
            {
                blanketId = blanketId,
                modelName = modelName,
                quantity = quantity,
                unitCost = unitCost
            };

            var response = await httpClient.PostAsJsonAsync($"{DistributorServiceUrl}/api/inventory", request);
            if (response.IsSuccessStatusCode)
            {
                viewModel.StatusMessage = "Inventory item added successfully";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error adding inventory: {response.StatusCode} - {errorBody}";
            }

            return await LoadDistributorInventory();
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            return View("Index", viewModel);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> UpdateDistributorInventory(int id, int blanketId, string modelName, int quantity, decimal unitCost)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var request = new
            {
                id = id,
                blanketId = blanketId,
                modelName = modelName,
                quantity = quantity,
                reservedQuantity = 0,
                availableQuantity = quantity,
                unitCost = unitCost,
                lastUpdated = DateTime.UtcNow
            };

            var response = await httpClient.PutAsJsonAsync($"{DistributorServiceUrl}/api/inventory/{id}", request);
            if (response.IsSuccessStatusCode)
            {
                viewModel.StatusMessage = "Inventory item updated successfully";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error updating inventory: {response.StatusCode} - {errorBody}";
            }

            return await LoadDistributorInventory();
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            return View("Index", viewModel);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> DeleteDistributorInventory(int id)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var response = await httpClient.DeleteAsync($"{DistributorServiceUrl}/api/inventory/{id}");
            if (response.IsSuccessStatusCode)
            {
                viewModel.StatusMessage = "Inventory item deleted successfully";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error deleting inventory: {response.StatusCode} - {errorBody}";
            }

            return await LoadDistributorInventory();
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            return View("Index", viewModel);
        }
    }

    [HttpGet]
    [HttpPost]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> LoadDistributorOrders(string? status = null)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            // Preserve catalog
            var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (blanketsResponse.IsSuccessStatusCode)
            {
                viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }

            var url = string.IsNullOrWhiteSpace(status) 
                ? $"{DistributorServiceUrl}/api/order" 
                : $"{DistributorServiceUrl}/api/order?status={Uri.EscapeDataString(status)}";
            
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                viewModel.DistributorOrders = await response.Content.ReadFromJsonAsync<List<DistributorOrderModel>>() ?? new();
                viewModel.StatusMessage = $"Loaded {viewModel.DistributorOrders.Count} order(s)";
                viewModel.ActiveTab = "distributor-orders";
            }
            else
            {
                viewModel.StatusMessage = $"Error: {response.StatusCode}";
            }

            // Also load delivery types for the dropdown
            try
            {
                var deliveryTypesResponse = await httpClient.GetAsync($"{DistributorServiceUrl}/api/deliverytypes");
                if (deliveryTypesResponse.IsSuccessStatusCode)
                {
                    viewModel.DeliveryTypes = await deliveryTypesResponse.Content.ReadFromJsonAsync<List<DeliveryTypeModel>>() ?? new();
                }
            }
            catch { /* ignore delivery types loading errors */ }
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
        }

        return View("Index", viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> ViewDistributorOrder(int id)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var response = await httpClient.GetAsync($"{DistributorServiceUrl}/api/order/{id}");
            if (response.IsSuccessStatusCode)
            {
                viewModel.SelectedDistributorOrder = await response.Content.ReadFromJsonAsync<DistributorOrderModel>();
                viewModel.StatusMessage = $"Loaded order {id} details";
                viewModel.ActiveTab = "distributor-orders";
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
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> UpdateOrderStatus(int id, string status)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var request = new { status = status };
            var response = await httpClient.PutAsJsonAsync($"{DistributorServiceUrl}/api/order/{id}/status", request);
            if (response.IsSuccessStatusCode)
            {
                viewModel.StatusMessage = "Order status updated successfully";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error updating order status: {response.StatusCode} - {errorBody}";
            }

            return await LoadDistributorOrders();
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            return View("Index", viewModel);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> UpdateOrderDelivery(int id, int? deliveryTypeId, string? deliveryAddress)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var request = new
            {
                deliveryTypeId = deliveryTypeId,
                deliveryAddress = deliveryAddress
            };

            var response = await httpClient.PutAsJsonAsync($"{DistributorServiceUrl}/api/order/{id}/delivery", request);
            if (response.IsSuccessStatusCode)
            {
                viewModel.StatusMessage = "Order delivery information updated successfully";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error updating order delivery: {response.StatusCode} - {errorBody}";
            }

            return await LoadDistributorOrders();
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            return View("Index", viewModel);
        }
    }

    [HttpGet]
    [HttpPost]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> LoadDeliveryTypes()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            // Preserve catalog
            var blanketsResponse = await httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (blanketsResponse.IsSuccessStatusCode)
            {
                viewModel.Blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<BlanketModel>>() ?? new();
            }

            var response = await httpClient.GetAsync($"{DistributorServiceUrl}/api/deliverytypes");
            if (response.IsSuccessStatusCode)
            {
                viewModel.DeliveryTypes = await response.Content.ReadFromJsonAsync<List<DeliveryTypeModel>>() ?? new();
                viewModel.StatusMessage = $"Loaded {viewModel.DeliveryTypes.Count} delivery type(s)";
                viewModel.ActiveTab = "delivery-types";
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
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> AddDeliveryType(string name, string description, decimal cost, int estimatedDays)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var request = new
            {
                name = name,
                description = description,
                cost = cost,
                estimatedDays = estimatedDays
            };

            var response = await httpClient.PostAsJsonAsync($"{DistributorServiceUrl}/api/deliverytypes", request);
            if (response.IsSuccessStatusCode)
            {
                viewModel.StatusMessage = "Delivery type added successfully";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error adding delivery type: {response.StatusCode} - {errorBody}";
            }

            return await LoadDeliveryTypes();
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            return View("Index", viewModel);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> UpdateDeliveryType(int id, string name, string description, decimal cost, int estimatedDays, bool isActive = false)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var request = new
            {
                name = name,
                description = description,
                cost = cost,
                estimatedDays = estimatedDays,
                isActive = isActive
            };

            var response = await httpClient.PutAsJsonAsync($"{DistributorServiceUrl}/api/deliverytypes/{id}", request);
            if (response.IsSuccessStatusCode)
            {
                viewModel.StatusMessage = "Delivery type updated successfully";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error updating delivery type: {response.StatusCode} - {errorBody}";
            }

            return await LoadDeliveryTypes();
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            return View("Index", viewModel);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> DeleteDeliveryType(int id)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var viewModel = new HomeViewModel { Cart = GetCart() };

        try
        {
            var response = await httpClient.DeleteAsync($"{DistributorServiceUrl}/api/deliverytypes/{id}");
            if (response.IsSuccessStatusCode)
            {
                viewModel.StatusMessage = "Delivery type deleted successfully";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                viewModel.StatusMessage = $"Error deleting delivery type: {response.StatusCode} - {errorBody}";
            }

            return await LoadDeliveryTypes();
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Error: {ex.Message}";
            return View("Index", viewModel);
        }
    }
}
