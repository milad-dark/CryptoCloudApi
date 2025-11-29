# Swagger UI Quick Start Guide

## Accessing Swagger UI

### Development Environment
When running in development mode, Swagger UI is available at the root URL:

```
https://localhost:5001/
```

or

```
http://localhost:5000/
```

### Production Environment
In production, Swagger UI is available at:

```
https://yourdomain.com/swagger
```

## Using Swagger UI

### 1. Start the Application

```bash
dotnet run
```

The application will display:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### 2. Open Swagger UI

Navigate to `https://localhost:5001/` in your web browser.

You'll see the Swagger UI interface with two main sections:
- **Payment Management** - Create and manage invoices
- **Webhook / Postback** - Receive payment notifications

### 3. Test API Endpoints

#### Create a Payment Invoice

1. Click on **POST /api/payment/create** to expand it
2. Click **Try it out** button
3. Edit the request body (example provided):

```json
{
  "orderId": "ORDER-12345",
  "amount": 100,
  "currency": "USD",
  "email": "customer@example.com",
  "cryptocurrency": "USDT_TRC20",
  "timeToPayHours": 24
}
```

4. Click **Execute**
5. See the response with invoice details and payment link

#### Get Invoice Details

1. Click on **GET /api/payment/{invoiceUuid}** to expand it
2. Click **Try it out**
3. Enter an invoice UUID (e.g., `INV-XXXXXXXX` or just `XXXXXXXX`)
4. Click **Execute**
5. View complete invoice details including transaction history

#### Get Statistics

1. Click on **GET /api/payment/statistics** to expand it
2. Click **Try it out**
3. Click **Execute**
4. View comprehensive payment statistics

## Swagger Features

### ?? Filter Endpoints
Use the filter box at the top to search for specific endpoints.

### ?? Request Duration
Swagger UI displays how long each request takes to complete.

### ?? Request/Response Examples
Each endpoint includes:
- Request body schema with examples
- Response codes (200, 400, 404, 500, etc.)
- Response body schemas with examples

### ?? Deep Linking
Share direct links to specific endpoints:
```
https://localhost:5001/#/Payment%20Management/PaymentController_CreateInvoice
```

### ?? Download OpenAPI Specification
Download the API specification in JSON or YAML format:
```
https://localhost:5001/swagger/v1/swagger.json
```

## API Endpoint Groups

### Payment Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/payment/create | Create new invoice |
| GET | /api/payment/{invoiceUuid} | Get invoice by UUID |
| GET | /api/payment/order/{orderId} | Get invoice by order ID |
| POST | /api/payment/{invoiceUuid}/refresh | Refresh invoice status |
| GET | /api/payment/statistics | Get payment statistics |

### Webhook / Postback

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/postback/notify | Receive payment notification from CryptoCloud |
| GET | /api/postback/test | Test postback endpoint availability |

## Example Workflow

### Complete Payment Flow Using Swagger UI

1. **Create Invoice**
   - Use POST /api/payment/create
   - Copy the `invoice_uuid` and `payment_link` from response

2. **Get Invoice Status**
   - Use GET /api/payment/{invoiceUuid}
   - Check the `status` field (created, paid, partial, etc.)

3. **Simulate Payment** (in test environment)
   - Open the `payment_link` in browser
   - Complete test payment on CryptoCloud

4. **Check Updated Status**
   - Use POST /api/payment/{invoiceUuid}/refresh
   - Or wait for automatic background service update
   - Or wait for automatic postback notification

5. **View Statistics**
   - Use GET /api/payment/statistics
   - See total payments, success rate, etc.

## Request Examples from Swagger

### Create Simple Invoice

```json
{
  "orderId": "ORDER-001",
  "amount": 50,
  "currency": "USD"
}
```

### Create Invoice with Cryptocurrency Selection

```json
{
  "orderId": "ORDER-002",
  "amount": 100,
  "currency": "USD",
  "email": "customer@example.com",
  "cryptocurrency": "USDT_TRC20"
}
```

### Create Invoice with Multiple Currency Options

```json
{
  "orderId": "ORDER-003",
  "amount": 200,
  "currency": "USD",
  "email": "customer@example.com",
  "availableCurrencies": ["USDT_TRC20", "BTC", "ETH", "USDT_ERC20"],
  "timeToPayHours": 48,
  "metadata": "{\"userId\": \"12345\", \"productId\": \"PROD-001\"}"
}
```

## Response Codes

### Success Codes
- **200 OK** - Request successful
- **201 Created** - Resource created (not used currently)

### Client Error Codes
- **400 Bad Request** - Invalid request parameters
- **404 Not Found** - Resource not found (invoice doesn't exist)

### Server Error Codes
- **500 Internal Server Error** - Server-side error
- **401 Unauthorized** - Invalid authentication (postback JWT token)

## Testing Postback Webhook

The postback endpoint is called automatically by CryptoCloud, but you can test it's working:

1. Click on **GET /api/postback/test**
2. Click **Try it out**
3. Click **Execute**
4. You should see:
```json
{
  "message": "Postback endpoint is working",
  "timestamp": "2024-01-01T12:00:00Z",
  "endpoint": "https://localhost:5001/api/postback/notify",
  "note": "Configure this URL as your postback URL in CryptoCloud dashboard"
}
```

## Exporting API Documentation

### Download OpenAPI Specification

**JSON Format:**
```
https://localhost:5001/swagger/v1/swagger.json
```

**Use with Postman:**
1. Open Postman
2. Click Import
3. Enter URL: `https://localhost:5001/swagger/v1/swagger.json`
4. Click Continue
5. All endpoints imported!

**Use with Insomnia:**
1. Open Insomnia
2. Create ? From URL
3. Enter URL: `https://localhost:5001/swagger/v1/swagger.json`
4. Import collection

## Customizing Swagger UI

To customize the Swagger UI appearance or behavior, edit `Program.cs`:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Version = "v1",
        Title = "Your Custom Title",
        Description = "Your custom description"
    });
    
    // Add more customizations here
});
```

## Troubleshooting

### Issue: Swagger UI not loading

**Solution:**
1. Ensure you're running in Development environment
2. Check the URL is correct: `https://localhost:5001/`
3. Check browser console for errors
4. Try clearing browser cache

### Issue: Endpoints not showing

**Solution:**
1. Ensure controllers have `[ApiController]` attribute
2. Rebuild the project: `dotnet build`
3. Restart the application

### Issue: XML comments not appearing

**Solution:**
1. Check `GenerateDocumentationFile` is set to `true` in .csproj
2. Rebuild the project
3. Check the XML file exists in bin folder

## Additional Resources

- [Swagger UI Documentation](https://swagger.io/tools/swagger-ui/)
- [OpenAPI Specification](https://swagger.io/specification/)
- [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)

---

**Ready to test!** Start your application and navigate to `https://localhost:5001/` to see Swagger UI in action.
