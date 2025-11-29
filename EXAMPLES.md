# CryptoCloud API Usage Examples

## Complete Integration Flow

### Step 1: Configure Your Application

Edit `appsettings.json`:

```json
{
  "CryptoCloud": {
    "ApiKey": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
    "ShopId": "xBAivfPIbskwuEWj",
    "SecretKey": "your-secret-key-from-dashboard",
    "ApiBaseUrl": "https://api.cryptocloud.plus",
    "DefaultCurrency": "USD",
    "TestMode": false,
    "StatusCheckIntervalSeconds": 60,
    "MonitoringPeriodHours": 24
  }
}
```

### Step 2: Start the Application

```bash
dotnet run
```

Application will:
- Start API server on https://localhost:5001
- Initialize SQLite database (payments.db)
- Start background monitoring service
- Begin checking for pending invoices

---

## API Examples

### 1. Create Simple Invoice

**Request:**
```bash
curl -X POST https://localhost:5001/api/payment/create \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORDER-001",
    "amount": 50,
    "currency": "USD"
  }'
```

**Response:**
```json
{
  "success": true,
  "invoice_uuid": "INV-XXXXXXXX",
  "payment_link": "https://pay.cryptocloud.plus/XXXXXXXX",
  "payment_address": null,
  "amount": 50.0,
  "amount_usd": 50.0,
  "currency": "USD",
  "crypto_currency": null,
  "status": "created",
  "expires_at": "2024-01-02T12:00:00Z",
  "created_at": "2024-01-01T12:00:00Z"
}
```

### 2. Create Invoice with Specific Cryptocurrency

**Request:**
```bash
curl -X POST https://localhost:5001/api/payment/create \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORDER-002",
    "amount": 100,
    "currency": "USD",
    "email": "customer@example.com",
    "cryptocurrency": "USDT_TRC20"
  }'
```

**Response:**
```json
{
  "success": true,
  "invoice_uuid": "INV-YYYYYYYY",
  "payment_link": "https://pay.cryptocloud.plus/YYYYYYYY",
  "payment_address": "TXyz...abc",
  "amount": 100.0,
  "amount_usd": 100.0,
  "currency": "USD",
  "crypto_currency": "USDT_TRC20",
  "status": "created",
  "expires_at": "2024-01-02T12:00:00Z",
  "created_at": "2024-01-01T12:00:00Z"
}
```

### 3. Create Invoice with Multiple Currency Options

**Request:**
```bash
curl -X POST https://localhost:5001/api/payment/create \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORDER-003",
    "amount": 200,
    "currency": "USD",
    "email": "customer@example.com",
    "availableCurrencies": ["USDT_TRC20", "BTC", "ETH", "USDT_ERC20"],
    "timeToPayHours": 48,
    "metadata": "{\"userId\": \"12345\", \"productId\": \"PROD-001\"}"
  }'
```

### 4. Get Invoice Details

**Request:**
```bash
curl -X GET https://localhost:5001/api/payment/INV-XXXXXXXX
```

**Response:**
```json
{
  "invoice_uuid": "INV-XXXXXXXX",
  "order_id": "ORDER-001",
  "payment_link": "https://pay.cryptocloud.plus/XXXXXXXX",
  "payment_address": "TXyz...abc",
  "amount": 50.0,
  "amount_usd": 50.0,
  "received_amount": 50.0,
  "currency": "USD",
  "crypto_currency": "USDT_TRC20",
  "status": "paid",
  "customer_email": "customer@example.com",
  "fee": 1.4,
  "service_fee": 0.95,
  "test_mode": false,
  "created_at": "2024-01-01T12:00:00Z",
  "expires_at": "2024-01-02T12:00:00Z",
  "paid_at": "2024-01-01T12:30:00Z",
  "last_status_check": "2024-01-01T12:35:00Z",
  "transactions": [
    {
      "transaction_hash": "0xabc...123",
      "amount": 50.0,
      "currency": "USDT_TRC20",
      "detected_at": "2024-01-01T12:30:00Z",
      "confirmations": 20
    }
  ]
}
```

### 5. Get Invoice by Order ID

**Request:**
```bash
curl -X GET https://localhost:5001/api/payment/order/ORDER-001
```

**Response:**
```json
{
  "invoice_uuid": "INV-XXXXXXXX",
  "order_id": "ORDER-001",
  "status": "paid",
  ...
}
```

### 6. Manually Refresh Invoice Status

**Request:**
```bash
curl -X POST https://localhost:5001/api/payment/INV-XXXXXXXX/refresh
```

**Response:**
```json
{
  "success": true,
  "status": "paid",
  "received_amount": 50.0,
  "paid_at": "2024-01-01T12:30:00Z",
  "last_status_check": "2024-01-01T13:00:00Z"
}
```

### 7. Get Payment Statistics

**Request:**
```bash
curl -X GET https://localhost:5001/api/payment/statistics
```

**Response:**
```json
{
  "total_invoices": 150,
  "paid_invoices": 120,
  "pending_invoices": 25,
  "canceled_invoices": 5,
  "today_invoices": 10,
  "this_month_invoices": 80,
  "total_amount_usd": 15000.50,
  "success_rate": 80.0
}
```

### 8. Test Postback Endpoint

**Request:**
```bash
curl -X GET https://localhost:5001/api/postback/test
```

**Response:**
```json
{
  "message": "Postback endpoint is working",
  "timestamp": "2024-01-01T12:00:00Z",
  "endpoint": "https://localhost:5001/api/postback/notify"
}
```

---

## Postback Webhook

When a payment is completed, CryptoCloud will automatically send a POST request to your configured webhook URL.

### Configure Webhook URL

In your CryptoCloud project settings, set:
```
https://yourdomain.com/api/postback/notify
```

### Example Postback Payload

```json
{
  "status": "success",
  "invoice_id": "INV-XXXXXXXX",
  "amount_crypto": 50.123456,
  "currency": "USDT_TRC20",
  "order_id": "ORDER-001",
  "token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
  "invoice_info": {
    "uuid": "INV-XXXXXXXX",
    "created": "2024-01-01 12:00:00.000000",
    "address": "TXyz...abc",
    "currency": {
      "id": 4,
      "code": "USDT",
      "fullcode": "USDT_TRC20",
      "name": "Tether"
    },
    "date_finished": "2024-01-01 12:30:00.000000",
    "status": "paid",
    "invoice_status": "success",
    "amount": 50.0,
    "amount_usd": 50.0,
    "amount_paid": 50.123456,
    "amount_paid_usd": 50.12,
    "fee": 1.4,
    "fee_usd": 1.4,
    "service_fee": 0.95,
    "service_fee_usd": 0.95,
    "received": 48.05,
    "received_usd": 48.05,
    "tx_list": ["0xabc...123"],
    "test_mode": false
  }
}
```

### Postback Processing

The API automatically:
1. Receives the postback
2. Verifies JWT token signature
3. Updates invoice status in database
4. Saves transaction hashes
5. Logs the event
6. Returns success response

---

## C# Integration Example

### Create Invoice from Your Application

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class PaymentService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "https://localhost:5001";

    public PaymentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreateInvoiceResponse> CreateInvoiceAsync(
        string orderId, 
        decimal amount, 
        string email)
    {
        var request = new
        {
            orderId = orderId,
            amount = amount,
            currency = "USD",
            email = email,
            cryptocurrency = "USDT_TRC20"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_apiBaseUrl}/api/payment/create", 
            content);

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CreateInvoiceResponse>(responseContent);
    }

    public async Task<InvoiceDetails> GetInvoiceAsync(string invoiceUuid)
    {
        var response = await _httpClient.GetAsync(
            $"{_apiBaseUrl}/api/payment/{invoiceUuid}");

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<InvoiceDetails>(content);
    }

    public async Task<bool> CheckPaymentStatusAsync(string invoiceUuid)
    {
        var invoice = await GetInvoiceAsync(invoiceUuid);
        return invoice.Status == "paid" || invoice.Status == "overpaid";
    }
}

// Usage
var paymentService = new PaymentService(new HttpClient());

// Create invoice
var result = await paymentService.CreateInvoiceAsync(
    "ORDER-12345", 
    100m, 
    "customer@example.com");

Console.WriteLine($"Payment link: {result.PaymentLink}");
Console.WriteLine($"Invoice UUID: {result.InvoiceUuid}");

// Redirect customer to payment link
// result.PaymentLink

// Later, check status
var isPaid = await paymentService.CheckPaymentStatusAsync(result.InvoiceUuid);
if (isPaid)
{
    Console.WriteLine("Payment confirmed! Deliver goods/services.");
}
```

---

## JavaScript/Node.js Integration Example

```javascript
const axios = require('axios');

class PaymentService {
  constructor(apiBaseUrl = 'https://localhost:5001') {
    this.apiBaseUrl = apiBaseUrl;
  }

  async createInvoice(orderId, amount, email) {
    try {
      const response = await axios.post(
        `${this.apiBaseUrl}/api/payment/create`,
        {
          orderId: orderId,
          amount: amount,
          currency: 'USD',
          email: email,
          cryptocurrency: 'USDT_TRC20'
        }
      );

      return response.data;
    } catch (error) {
      console.error('Error creating invoice:', error);
      throw error;
    }
  }

  async getInvoice(invoiceUuid) {
    try {
      const response = await axios.get(
        `${this.apiBaseUrl}/api/payment/${invoiceUuid}`
      );

      return response.data;
    } catch (error) {
      console.error('Error getting invoice:', error);
      throw error;
    }
  }

  async checkPaymentStatus(invoiceUuid) {
    const invoice = await this.getInvoice(invoiceUuid);
    return invoice.status === 'paid' || invoice.status === 'overpaid';
  }
}

// Usage
const paymentService = new PaymentService();

(async () => {
  // Create invoice
  const result = await paymentService.createInvoice(
    'ORDER-12345',
    100,
    'customer@example.com'
  );

  console.log('Payment link:', result.payment_link);
  console.log('Invoice UUID:', result.invoice_uuid);

  // Check status after some time
  setTimeout(async () => {
    const isPaid = await paymentService.checkPaymentStatus(result.invoice_uuid);
    if (isPaid) {
      console.log('Payment confirmed!');
    }
  }, 60000); // Check after 1 minute
})();
```

---

## Python Integration Example

```python
import requests
import json

class PaymentService:
    def __init__(self, api_base_url='https://localhost:5001'):
        self.api_base_url = api_base_url

    def create_invoice(self, order_id, amount, email):
        url = f'{self.api_base_url}/api/payment/create'
        payload = {
            'orderId': order_id,
            'amount': amount,
            'currency': 'USD',
            'email': email,
            'cryptocurrency': 'USDT_TRC20'
        }
        
        response = requests.post(url, json=payload)
        response.raise_for_status()
        return response.json()

    def get_invoice(self, invoice_uuid):
        url = f'{self.api_base_url}/api/payment/{invoice_uuid}'
        response = requests.get(url)
        response.raise_for_status()
        return response.json()

    def check_payment_status(self, invoice_uuid):
        invoice = self.get_invoice(invoice_uuid)
        return invoice['status'] in ['paid', 'overpaid']

# Usage
payment_service = PaymentService()

# Create invoice
result = payment_service.create_invoice(
    'ORDER-12345',
    100,
    'customer@example.com'
)

print(f"Payment link: {result['payment_link']}")
print(f"Invoice UUID: {result['invoice_uuid']}")

# Check status
import time
time.sleep(60)  # Wait 1 minute

is_paid = payment_service.check_payment_status(result['invoice_uuid'])
if is_paid:
    print("Payment confirmed!")
```

---

## Testing with ngrok

For local development, use ngrok to expose your local server:

```bash
# Start your API
dotnet run

# In another terminal, start ngrok
ngrok http 5001
```

Copy the ngrok URL (e.g., `https://abc123.ngrok.io`) and configure it in your CryptoCloud project:
```
https://abc123.ngrok.io/api/postback/notify
```

---

## Monitoring Logs

Watch the logs to see the background service in action:

```
[12:00:00 INF] Invoice Status Monitor Service started
[12:00:10 INF] Starting invoice status check cycle
[12:00:10 INF] Found 3 pending invoice(s) to check
[12:00:11 DBG] Checking status for invoice INV-XXXXXXXX
[12:00:12 INF] Invoice INV-XXXXXXXX status changed: created -> paid
[12:00:13 DBG] Checking status for invoice INV-YYYYYYYY
[12:00:14 INF] Invoice status check cycle completed. Checked: 3, Updated: 1, Errors: 0
[12:01:10 INF] Starting invoice status check cycle
...
[12:30:45 INF] Received postback for invoice INV-XXXXXXXX, status success
[12:30:45 INF] Postback processed successfully for invoice INV-XXXXXXXX
```

---

## Database Queries

Query the database directly using SQLite:

```bash
sqlite3 payments.db

# Get all invoices
SELECT * FROM PaymentInvoices;

# Get paid invoices
SELECT * FROM PaymentInvoices WHERE Status = 'paid';

# Get invoices with transactions
SELECT i.*, t.* 
FROM PaymentInvoices i 
LEFT JOIN PaymentTransactions t ON i.Id = t.PaymentInvoiceId 
WHERE i.Status = 'paid';

# Get statistics
SELECT 
  COUNT(*) as total,
  SUM(CASE WHEN Status = 'paid' OR Status = 'overpaid' THEN 1 ELSE 0 END) as paid,
  SUM(CASE WHEN Status = 'created' OR Status = 'partial' THEN 1 ELSE 0 END) as pending,
  SUM(CASE WHEN Status = 'paid' OR Status = 'overpaid' THEN AmountUsd ELSE 0 END) as total_amount
FROM PaymentInvoices;
```

---

## Common Scenarios

### Scenario 1: E-commerce Checkout

```csharp
// 1. User adds items to cart
// 2. User proceeds to checkout
// 3. Create invoice

var invoice = await paymentService.CreateInvoiceAsync(
    orderId: cart.Id.ToString(),
    amount: cart.Total,
    email: user.Email
);

// 4. Redirect user to payment page
Response.Redirect(invoice.PaymentLink);

// 5. Wait for postback or polling
// 6. Postback arrives -> database updated automatically
// 7. Check status before shipping

var currentStatus = await paymentService.GetInvoiceAsync(invoice.InvoiceUuid);
if (currentStatus.Status == "paid")
{
    await ShipOrder(cart.Id);
}
```

### Scenario 2: Subscription Payment

```csharp
// Monthly subscription
var invoice = await paymentService.CreateInvoiceAsync(
    orderId: $"SUB-{userId}-{DateTime.UtcNow:yyyyMM}",
    amount: 29.99m,
    email: user.Email
);

// Send email with payment link
await emailService.SendAsync(
    to: user.Email,
    subject: "Monthly Subscription Payment",
    body: $"Click here to pay: {invoice.PaymentLink}"
);

// Background service monitors
// Postback received -> update subscription status
```

### Scenario 3: Donation Platform

```csharp
// Custom donation amount
var invoice = await paymentService.CreateInvoiceAsync(
    orderId: Guid.NewGuid().ToString(),
    amount: donationAmount,
    email: donor.Email
);

// Allow multiple cryptocurrencies
var invoiceWithOptions = await paymentService.CreateInvoiceAsync(
    orderId: orderId,
    amount: donationAmount,
    email: donor.Email,
    availableCurrencies: new[] { "BTC", "ETH", "USDT_TRC20", "USDT_ERC20" }
);
```

---

## Troubleshooting

### Invoice Not Creating

Check logs for API errors:
```
[ERR] Failed to create invoice. Status: 401, Response: {"detail": "Unauthorized"}
```

**Solution:** Verify your API key in appsettings.json

### Postback Not Received

1. Check postback URL is configured in CryptoCloud dashboard
2. Test endpoint: `curl https://yourdomain.com/api/postback/test`
3. Check firewall allows incoming connections
4. Review logs for incoming requests

### Status Not Updating

1. Check background service is running
2. Verify invoice hasn't expired
3. Check logs for API errors
4. Manually refresh: `POST /api/payment/{uuid}/refresh`

---

**Need Help?** 
- Check README.md
- Review logs in console
- Contact CryptoCloud Support: https://support.cryptocloud.plus/en
