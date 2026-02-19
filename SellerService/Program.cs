using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using SellerService.Data;
using SellerService.Repositories;
using SellerService.Services;
using SellerService.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/seller-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Seller Service API",
        Version = "v1",
        Description = "API for processing customer orders and checking product availability",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Cozy Comfort",
            Email = "support@cozycomfort.com"
        }
    });
});

// Configure Entity Framework Core with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SellerDbContext>(options =>
    options.UseSqlite(connectionString));

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SellerDbContext>("database");

// Register Repositories (Scoped - one per HTTP request)
builder.Services.AddScoped<ICustomerOrderRepository, CustomerOrderRepository>();
builder.Services.AddScoped<ISellerInventoryRepository, SellerInventoryRepository>();

// Register Services (Scoped - one per HTTP request)
builder.Services.AddScoped<ISellerService, SellerService.Services.SellerService>();

// Register HTTP Client for DistributorService communication with Polly retry
builder.Services.AddHttpClient<IDistributorServiceClient, DistributorServiceClient>(client =>
{
    var baseUrl = builder.Configuration["DistributorService:BaseUrl"] ?? "http://localhost:5002";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Add CORS for inter-service communication
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure database is created and seed Seller's own stock (PDF: "Seller checks their own stock")
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SellerDbContext>();
        context.Database.EnsureCreated();
        if (!context.SellerInventories.Any())
        {
            context.SellerInventories.AddRange(
                new SellerService.Models.SellerInventory { BlanketId = 1, ModelName = "Cozy Classic", Quantity = 10, ReservedQuantity = 0, UnitCost = 35.00m },
                new SellerService.Models.SellerInventory { BlanketId = 2, ModelName = "Winter Warmth", Quantity = 5, ReservedQuantity = 0, UnitCost = 45.00m }
            );
            context.SaveChanges();
            Log.Information("Seller inventory seed data added");
        }
        Log.Information("Database ensured/created successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while creating the database");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Seller Service API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("Seller Service starting up");

app.Run();
