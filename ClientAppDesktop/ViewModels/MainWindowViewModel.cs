using ReactiveUI;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Reactive;
using System.Text.Json;

namespace ClientAppDesktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    private static string ManufacturerServiceUrl => Environment.GetEnvironmentVariable("Services__ManufacturerServiceUrl") ?? "http://localhost:5001";
    private static string DistributorServiceUrl => Environment.GetEnvironmentVariable("Services__DistributorServiceUrl") ?? "http://localhost:5002";
    private static string SellerServiceUrl => Environment.GetEnvironmentVariable("Services__SellerServiceUrl") ?? "http://localhost:5003";

    private string _statusMessage = "Ready";
    private bool _isLoading;
    private ObservableCollection<BlanketModel> _blankets = new();
    private ObservableCollection<InventoryItem> _inventory = new();
    private ObservableCollection<OrderModel> _orders = new();
    private BlanketModel? _selectedBlanket;
    private InventoryItem? _selectedInventory;
    private int _selectedBlanketId = 1;
    private int _orderQuantity = 1;
    private string _customerName = "";
    private string _customerEmail = "";
    private string _shippingAddress = "";

    public MainWindowViewModel()
    {
        CheckServicesCommand = ReactiveCommand.CreateFromTask(CheckServicesAsync);
        LoadBlanketsCommand = ReactiveCommand.CreateFromTask(LoadBlanketsAsync);
        CheckStockCommand = ReactiveCommand.CreateFromTask(CheckStockAsync);
        LoadInventoryCommand = ReactiveCommand.CreateFromTask(LoadInventoryAsync);
        CheckAvailabilityCommand = ReactiveCommand.CreateFromTask(CheckAvailabilityAsync);
        PlaceOrderCommand = ReactiveCommand.CreateFromTask(PlaceOrderAsync);
        CompleteDemoCommand = ReactiveCommand.CreateFromTask(CompleteDemoAsync);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ObservableCollection<BlanketModel> Blankets
    {
        get => _blankets;
        set => this.RaiseAndSetIfChanged(ref _blankets, value);
    }

    public ObservableCollection<InventoryItem> Inventory
    {
        get => _inventory;
        set => this.RaiseAndSetIfChanged(ref _inventory, value);
    }

    public ObservableCollection<OrderModel> Orders
    {
        get => _orders;
        set => this.RaiseAndSetIfChanged(ref _orders, value);
    }

    public BlanketModel? SelectedBlanket
    {
        get => _selectedBlanket;
        set => this.RaiseAndSetIfChanged(ref _selectedBlanket, value);
    }

    public InventoryItem? SelectedInventory
    {
        get => _selectedInventory;
        set => this.RaiseAndSetIfChanged(ref _selectedInventory, value);
    }

    public int SelectedBlanketId
    {
        get => _selectedBlanketId;
        set => this.RaiseAndSetIfChanged(ref _selectedBlanketId, value);
    }

    public int OrderQuantity
    {
        get => _orderQuantity;
        set => this.RaiseAndSetIfChanged(ref _orderQuantity, value);
    }

    public string CustomerName
    {
        get => _customerName;
        set => this.RaiseAndSetIfChanged(ref _customerName, value);
    }

    public string CustomerEmail
    {
        get => _customerEmail;
        set => this.RaiseAndSetIfChanged(ref _customerEmail, value);
    }

    public string ShippingAddress
    {
        get => _shippingAddress;
        set => this.RaiseAndSetIfChanged(ref _shippingAddress, value);
    }

    public ReactiveCommand<Unit, Unit> CheckServicesCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadBlanketsCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckStockCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadInventoryCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckAvailabilityCommand { get; }
    public ReactiveCommand<Unit, Unit> PlaceOrderCommand { get; }
    public ReactiveCommand<Unit, Unit> CompleteDemoCommand { get; }

    private async Task CheckServicesAsync()
    {
        IsLoading = true;
        StatusMessage = "Checking services...";

        try
        {
            var services = new[]
            {
                ("ManufacturerService", ManufacturerServiceUrl),
                ("DistributorService", DistributorServiceUrl),
                ("SellerService", SellerServiceUrl)
            };

            var results = new List<string>();
            foreach (var (name, url) in services)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    results.Add($"✓ {name} is running");
                }
                catch
                {
                    results.Add($"✗ {name} is NOT running");
                }
            }

            StatusMessage = string.Join(" | ", results);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadBlanketsAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading blankets...";

        try
        {
            var response = await _httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets");
            if (response.IsSuccessStatusCode)
            {
                var blankets = await response.Content.ReadFromJsonAsync<List<BlanketModel>>();
                Blankets = new ObservableCollection<BlanketModel>(blankets ?? new List<BlanketModel>());
                StatusMessage = $"Loaded {Blankets.Count} blanket models";
            }
            else
            {
                StatusMessage = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CheckStockAsync()
    {
        IsLoading = true;
        StatusMessage = $"Checking stock for model {SelectedBlanketId}...";

        try
        {
            var response = await _httpClient.GetAsync($"{ManufacturerServiceUrl}/api/blankets/stock/{SelectedBlanketId}");
            if (response.IsSuccessStatusCode)
            {
                var stock = await response.Content.ReadFromJsonAsync<StockModel>();
                StatusMessage = $"Stock: {stock?.AvailableQuantity} available (Total: {stock?.Quantity}, Reserved: {stock?.ReservedQuantity})";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                StatusMessage = $"Model ID {SelectedBlanketId} not found";
            }
            else
            {
                StatusMessage = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadInventoryAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading distributor inventory...";

        try
        {
            var response = await _httpClient.GetAsync($"{DistributorServiceUrl}/api/inventory");
            if (response.IsSuccessStatusCode)
            {
                var inventory = await response.Content.ReadFromJsonAsync<List<InventoryItem>>();
                Inventory = new ObservableCollection<InventoryItem>(inventory ?? new List<InventoryItem>());
                StatusMessage = $"Loaded {Inventory.Count} inventory items";
            }
            else
            {
                StatusMessage = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CheckAvailabilityAsync()
    {
        IsLoading = true;
        StatusMessage = $"Checking availability for model {SelectedBlanketId}...";

        try
        {
            var response = await _httpClient.GetAsync($"{SellerServiceUrl}/api/availability/{SelectedBlanketId}");
            if (response.IsSuccessStatusCode)
            {
                var availability = await response.Content.ReadFromJsonAsync<AvailabilityModel>();
                StatusMessage = availability?.IsAvailable == true
                    ? $"Available: {availability.AvailableQuantity} units - {availability.Message}"
                    : $"Not Available - {availability?.Message}";
            }
            else
            {
                StatusMessage = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PlaceOrderAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(CustomerEmail) || string.IsNullOrWhiteSpace(ShippingAddress))
        {
            StatusMessage = "Please fill in all customer details";
            return;
        }

        IsLoading = true;
        StatusMessage = "Placing order...";

        try
        {
            var order = new
            {
                customerName = CustomerName,
                customerEmail = CustomerEmail,
                customerPhone = "123-456-7890",
                shippingAddress = ShippingAddress,
                items = new[]
                {
                    new { blanketId = SelectedBlanketId, quantity = OrderQuantity }
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"{SellerServiceUrl}/api/customerorder", order);
            if (response.IsSuccessStatusCode)
            {
                var orderResponse = await response.Content.ReadFromJsonAsync<OrderResponseModel>();
                StatusMessage = $"Order {orderResponse?.OrderId} placed! Status: {orderResponse?.Status} - {orderResponse?.Message}";
                
                // Add to orders list
                Orders.Insert(0, new OrderModel
                {
                    OrderId = orderResponse?.OrderId ?? 0,
                    Status = orderResponse?.Status ?? "Unknown",
                    CustomerName = CustomerName,
                    TotalAmount = orderResponse?.TotalAmount ?? 0
                });
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                StatusMessage = $"Error: {response.StatusCode} - {error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CompleteDemoAsync()
    {
        IsLoading = true;
        StatusMessage = "Running complete order flow demo...";

        try
        {
            // Step 1: Load blankets
            await LoadBlanketsAsync();
            await Task.Delay(500);

            // Step 2: Check stock
            SelectedBlanketId = 1;
            await CheckStockAsync();
            await Task.Delay(500);

            // Step 3: Load inventory
            await LoadInventoryAsync();
            await Task.Delay(500);

            // Step 4: Check availability
            await CheckAvailabilityAsync();
            await Task.Delay(500);

            // Step 5: Place sample order
            CustomerName = "Demo Customer";
            CustomerEmail = "demo@example.com";
            ShippingAddress = "123 Demo Street";
            SelectedBlanketId = 1;
            OrderQuantity = 2;
            await PlaceOrderAsync();

            StatusMessage = "Demo completed successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Demo error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

// Model classes
public class BlanketModel
{
    public int Id { get; set; }
    public string ModelName { get; set; } = "";
    public string Material { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal UnitPrice { get; set; }
}

public class StockModel
{
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = "";
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}

public class InventoryItem
{
    public int Id { get; set; }
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = "";
    public int Quantity { get; set; }
    public int AvailableQuantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class AvailabilityModel
{
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = "";
    public bool IsAvailable { get; set; }
    public int AvailableQuantity { get; set; }
    public string Message { get; set; } = "";
}

public class OrderResponseModel
{
    public int OrderId { get; set; }
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public decimal TotalAmount { get; set; }
}

public class OrderModel
{
    public int OrderId { get; set; }
    public string Status { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public decimal TotalAmount { get; set; }
}
