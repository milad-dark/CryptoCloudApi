using CryptoCloudApi.BackgroundServices;
using CryptoCloudApi.Data;
using CryptoCloudApi.Models.Configuration;
using CryptoCloudApi.Services;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Version = "v1",
        Title = "CryptoCloud Payment API",
        Description = "Complete .NET API integration for CryptoCloud cryptocurrency payment gateway with automatic transaction monitoring and postback handling"
    });

    // Add XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Group endpoints by tags
    options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Unknown" });
    options.DocInclusionPredicate((name, api) => true);
});

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

WebApplication app = builder.Build();

// Create database on startup
using (IServiceScope? scope = app.Services.CreateScope())
{
    PaymentDbContext? dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    dbContext.Database.EnsureCreated();
    
    ILogger<Program>? logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Database initialized successfully");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CryptoCloud Payment API v1");
        options.RoutePrefix = "swagger"; 
        options.DocumentTitle = "CryptoCloud Payment API";
        options.DisplayRequestDuration();
        options.EnableFilter();
        options.EnableDeepLinking();
    });
    
    app.UseCors();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
