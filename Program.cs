using CryptoCloudApi.BackgroundServices;
using CryptoCloudApi.Data;
using CryptoCloudApi.Models.Configuration;
using CryptoCloudApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure CryptoCloud settings
builder.Services.Configure<CryptoCloudSettings>(
    builder.Configuration.GetSection(CryptoCloudSettings.SectionName));

// Configure Database (using SQLite for simplicity)
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=payments.db"));

// Register HttpClient for CryptoCloud API
builder.Services.AddHttpClient<CryptoCloudApiService>();

// Register application services
builder.Services.AddScoped<InvoiceManagementService>();

// Register background service for invoice status monitoring
builder.Services.AddHostedService<InvoiceStatusMonitorService>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Create database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    dbContext.Database.EnsureCreated();
    
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Database initialized successfully");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
