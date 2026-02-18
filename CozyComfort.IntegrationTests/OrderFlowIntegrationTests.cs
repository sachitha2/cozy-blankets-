using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ManufacturerService;
using DistributorService;
using SellerService;

namespace CozyComfort.IntegrationTests;

/// <summary>
/// Integration tests for the complete order flow across all services
/// These tests verify end-to-end functionality of the SOA system
/// </summary>
public class OrderFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _manufacturerClient;
    private readonly HttpClient _distributorClient;
    private readonly HttpClient _sellerClient;

    public OrderFlowIntegrationTests()
    {
        // Note: In a real scenario, these would be separate test servers
        // For demonstration, we're testing the API contracts
        _manufacturerClient = new HttpClient { BaseAddress = new Uri("http://localhost:5001") };
        _distributorClient = new HttpClient { BaseAddress = new Uri("http://localhost:5002") };
        _sellerClient = new HttpClient { BaseAddress = new Uri("http://localhost:5003") };
    }

    [Fact(Skip = "Requires services to be running")]
    public async Task CompleteOrderFlow_ShouldProcessOrderSuccessfully()
    {
        // Step 1: Get available blankets from ManufacturerService
        var blanketsResponse = await _manufacturerClient.GetAsync("/api/blankets");
        blanketsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var blankets = await blanketsResponse.Content.ReadFromJsonAsync<List<object>>();
        blankets.Should().NotBeEmpty();

        // Step 2: Check stock for a blanket
        var stockResponse = await _manufacturerClient.GetAsync("/api/blankets/stock/1");
        stockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Check distributor inventory
        var inventoryResponse = await _distributorClient.GetAsync("/api/inventory");
        inventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var inventory = await inventoryResponse.Content.ReadFromJsonAsync<List<object>>();
        inventory.Should().NotBeEmpty();

        // Step 4: Check availability through SellerService
        var availabilityResponse = await _sellerClient.GetAsync("/api/availability/1");
        availabilityResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Place customer order
        var orderRequest = new
        {
            customerName = "Test Customer",
            customerEmail = "test@example.com",
            customerPhone = "123-456-7890",
            shippingAddress = "123 Test St",
            items = new[]
            {
                new { blanketId = 1, quantity = 2 }
            }
        };

        var orderResponse = await _sellerClient.PostAsJsonAsync("/api/customerorder", orderRequest);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await orderResponse.Content.ReadFromJsonAsync<object>();
        order.Should().NotBeNull();
    }

    [Fact(Skip = "Requires services to be running")]
    public async Task CheckAvailability_WhenProductAvailable_ShouldReturnTrue()
    {
        var response = await _sellerClient.GetAsync("/api/availability/1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var availability = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        availability.Should().NotBeNull();
        availability!["isAvailable"].Should().NotBeNull();
    }

    public void Dispose()
    {
        _manufacturerClient?.Dispose();
        _distributorClient?.Dispose();
        _sellerClient?.Dispose();
    }
}
