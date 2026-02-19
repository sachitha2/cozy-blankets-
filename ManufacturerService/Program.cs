using Microsoft.EntityFrameworkCore;
using Serilog;
using ManufacturerService.Data;
using ManufacturerService.Repositories;
using ManufacturerService.Services;
using ManufacturerService.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/manufacturer-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Manufacturer Service API",
        Version = "v1",
        Description = "API for managing blanket manufacturing, stock, and production capacity",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Cozy Comfort",
            Email = "support@cozycomfort.com"
        }
    });
});

// Configure Entity Framework Core with SQLite (cross-platform)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ManufacturerDbContext>(options =>
    options.UseSqlite(connectionString));

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ManufacturerDbContext>("database");

// Register Repositories (Scoped - one per HTTP request)
builder.Services.AddScoped<IBlanketRepository, BlanketRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IProductionCapacityRepository, ProductionCapacityRepository>();

// Register Services (Scoped - one per HTTP request)
builder.Services.AddScoped<IBlanketService, BlanketService>();

// Add CORS if needed for inter-service communication
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Manufacturer Service API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

// Seed database with example data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ManufacturerDbContext>();
        DatabaseSeeder.SeedData(context);
        Log.Information("Database seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding the database");
    }
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("Manufacturer Service starting up");

app.Run();
