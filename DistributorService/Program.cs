using Microsoft.EntityFrameworkCore;
using Serilog;
using DistributorService.Data;
using DistributorService.Repositories;
using DistributorService.Services;
using DistributorService.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/distributor-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Distributor Service API",
        Version = "v1",
        Description = "API for managing distributor inventory and processing seller orders",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Cozy Comfort",
            Email = "support@cozycomfort.com"
        }
    });
});

// Configure Entity Framework Core with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DistributorDbContext>(options =>
    options.UseSqlite(connectionString));

// Register Repositories (Scoped - one per HTTP request)
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Register Services (Scoped - one per HTTP request)
builder.Services.AddScoped<IDistributorService, DistributorService.Services.DistributorService>();

// Register HTTP Client for ManufacturerService communication
builder.Services.AddHttpClient<IManufacturerServiceClient, ManufacturerServiceClient>(client =>
{
        var baseUrl = builder.Configuration["ManufacturerService:BaseUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Distributor Service API v1");
        c.RoutePrefix = string.Empty;
    });
}

// Seed database with example data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DistributorDbContext>();
        DatabaseSeeder.SeedData(context);
        Log.Information("Database seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding the database");
    }
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Add custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();

Log.Information("Distributor Service starting up");

app.Run();
