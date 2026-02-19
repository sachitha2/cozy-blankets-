using System.Text.Json.Serialization;

namespace ClientAppWeb.Models;

public class HomeViewModel
{
    public string StatusMessage { get; set; } = "Ready";
    public Dictionary<string, string> ServiceStatuses { get; set; } = new();
    public List<BlanketModel> Blankets { get; set; } = new();
    public List<CartItemModel> Cart { get; set; } = new();
    public StockModel? StockInfo { get; set; }
    public List<InventoryModel> Inventory { get; set; } = new();
    public List<InventoryModel> SellerInventory { get; set; } = new();
    public AvailabilityModel? AvailabilityInfo { get; set; }
    public OrderResponseModel? OrderResponse { get; set; }
    public List<CustomerOrderModel> CustomerOrders { get; set; } = new();
    public CustomerOrderModel? SelectedOrder { get; set; }
    public string ActiveTab { get; set; } = "blankets";
    public bool ShowCheckout { get; set; }
}

public class BlanketModel
{
    public int Id { get; set; }
    public string ModelName { get; set; } = "";
    public string Material { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal UnitPrice { get; set; }
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }
    [JsonPropertyName("additionalImageUrls")]
    public List<string> AdditionalImageUrls { get; set; } = new();
}

public class CartItemModel
{
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
}

public class StockModel
{
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = "";
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}

public class InventoryModel
{
    public int Id { get; set; }
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = "";
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public class AvailabilityModel
{
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = "";
    public bool IsAvailable { get; set; }
    public int AvailableQuantity { get; set; }
    public string Message { get; set; } = "";
}

public class OrderRequestModel
{
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string? CustomerPhone { get; set; }
    public string ShippingAddress { get; set; } = "";
    public int BlanketId { get; set; } = 1;
    public int Quantity { get; set; } = 1;
}

public class OrderResponseModel
{
    public int OrderId { get; set; }
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public decimal TotalAmount { get; set; }
}

public class CustomerOrderModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string ShippingAddress { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public DateTime? FulfilledDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemModel> OrderItems { get; set; } = new();
}

public class OrderItemModel
{
    public int Id { get; set; }
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public string Status { get; set; } = "";
}
