# Stripe Payment Integration Setup Guide

## Overview
Complete Stripe payment integration for PeerPay with payment intents, confirmations, refunds, and webhooks.

---

## 1. Install Stripe NuGet Package

Run this command in the Infrastructure project:

```bash
dotnet add package Stripe.net --version 43.0.0
```

---

## 2. Add Stripe Configuration to appsettings.json

Add to `PeerPayBackend/appsettings.json`:

```json
{
  "Stripe": {
    "PublishableKey": "pk_test_YOUR_PUBLISHABLE_KEY",
    "SecretKey": "sk_test_YOUR_SECRET_KEY",
    "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET"
  }
}
```

**Get your keys from**: https://dashboard.stripe.com/test/apikeys

---

## 3. Register Services in Program.cs

Add to `PeerPayBackend/Program.cs`:

```csharp
// Payment Services
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IStripeService, StripeService>();
```

---

## 4. API Endpoints

### PaymentController (`/api/payment`)

1. **POST /api/payment/create**
   - Create payment intent
   - Body: `CreatePaymentDto`
   - Returns: `PaymentIntentDto` with clientSecret for Stripe Elements

2. **POST /api/payment/confirm**
   - Confirm payment after Stripe processing
   - Body: `ConfirmPaymentDto`
   - Returns: `PaymentDto`

3. **POST /api/payment/refund**
   - Refund completed payment
   - Body: `RefundPaymentDto`
   - Returns: Success confirmation

4. **POST /api/payment/{id}/cancel**
   - Cancel pending payment
   - Returns: Success confirmation

5. **GET /api/payment/{id}**
   - Get payment by ID
   - Returns: `PaymentDto`

6. **GET /api/payment/job/{jobId}**
   - Get payment for specific job
   - Returns: `PaymentDto`

7. **GET /api/payment/employer/{employerId}**
   - Get all employer payments
   - Returns: `PaymentListDto`

8. **GET /api/payment/student/{studentId}**
   - Get all student payments
   - Returns: `PaymentListDto`

9. **GET /api/payment/status/{status}**
   - Get payments by status
   - Returns: List of `PaymentDto`

10. **GET /api/payment/intent/{paymentIntentId}/status**
    - Get Stripe payment intent status
    - Returns: Payment intent details

### StripeWebhookController (`/api/stripewebhook`)

1. **POST /api/stripewebhook**
   - Handle Stripe webhook events
   - Automatically updates payment status

---

## 5. Frontend Integration Guide

### Step 1: Create Payment Intent

```javascript
const response = await fetch('/api/payment/create', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    jobId: 'job-123',
    employerId: 'employer-123',
    studentId: 'student-123',
    amount: 100.00,
    currency: 'usd',
    notes: 'Payment for web development job'
  })
});

const { clientSecret, paymentIntentId } = await response.json();
```

### Step 2: Load Stripe Elements

```html
<script src="https://js.stripe.com/v3/"></script>
```

```javascript
const stripe = Stripe('pk_test_YOUR_PUBLISHABLE_KEY');

const elements = stripe.elements();
const cardElement = elements.create('card');
cardElement.mount('#card-element');
```

### Step 3: Confirm Payment

```javascript
const { error, paymentIntent } = await stripe.confirmCardPayment(clientSecret, {
  payment_method: {
    card: cardElement,
    billing_details: {
      name: 'Customer Name'
    }
  }
});

if (error) {
  console.error(error.message);
} else if (paymentIntent.status === 'succeeded') {
  // Notify backend
  await fetch('/api/payment/confirm', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      paymentId: 'payment-123',
      paymentIntentId: paymentIntent.id
    })
  });
}
```

---

## 6. Webhook Setup

### Local Testing with Stripe CLI

1. Install Stripe CLI: https://stripe.com/docs/stripe-cli

2. Login to Stripe:
```bash
stripe login
```

3. Forward webhooks to local:
```bash
stripe listen --forward-to https://localhost:7000/api/stripewebhook
```

4. Copy the webhook signing secret and add to appsettings.json

### Production Webhook

1. Go to: https://dashboard.stripe.com/test/webhooks
2. Add endpoint: `https://yourdomain.com/api/stripewebhook`
3. Select events:
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
   - `charge.refunded`
4. Copy webhook signing secret to production appsettings

---

## 7. Payment Flow

### Create Payment
1. Employer initiates payment for completed job
2. Backend creates Stripe PaymentIntent
3. Backend stores payment record with status `Pending`
4. Returns `clientSecret` to frontend

### Process Payment
1. Frontend collects card details using Stripe Elements
2. Frontend confirms payment with Stripe
3. Stripe processes payment
4. Frontend notifies backend of success
5. Backend confirms with Stripe and updates status to `Completed`
6. Notifications sent to both parties

### Webhook Updates
1. Stripe sends webhook for `payment_intent.succeeded`
2. Backend updates payment status
3. Sends notifications if not already sent

---

## 8. Test Cards (Stripe Test Mode)

**Successful Payment:**
- Card: `4242 4242 4242 4242`
- Any future expiry date
- Any 3-digit CVC

**Payment Declined:**
- Card: `4000 0000 0000 0002`

**Requires Authentication (3D Secure):**
- Card: `4000 0025 0000 3155`

**More test cards**: https://stripe.com/docs/testing

---

## 9. Features Included

✅ **Payment Intents**: Secure server-side payment creation
✅ **Automatic Payment Methods**: Supports cards, wallets, etc.
✅ **Payment Confirmation**: Verify successful payments
✅ **Refunds**: Full or partial refund support
✅ **Payment Cancellation**: Cancel pending payments
✅ **Webhooks**: Automatic status updates from Stripe
✅ **Payment History**: Track all payments by employer/student
✅ **Duplicate Prevention**: One payment per job
✅ **Auto-Notifications**: Notify users on payment events
✅ **Metadata Tracking**: Store job and user info in Stripe

---

## 10. Security Best Practices

1. **Never expose Secret Key**: Keep in appsettings.json, use environment variables in production
2. **Validate Webhooks**: Always verify webhook signatures
3. **Use HTTPS**: Required for production webhooks
4. **Client-side validation**: Stripe Elements handles card validation
5. **Server-side verification**: Always confirm payment status with Stripe
6. **Amount handling**: Convert to cents (multiply by 100) for Stripe

---

## 11. Currency Support

Currently set to USD. To support multiple currencies:

1. Update `CreatePaymentDto.Currency` default
2. Ensure amounts are in smallest currency unit (cents for USD, pence for GBP, etc.)
3. Supported currencies: https://stripe.com/docs/currencies

---

## 12. Error Handling

Common errors:
- **Payment requires authentication**: Use 3D Secure flow
- **Card declined**: Show user-friendly error message
- **Insufficient funds**: Request alternative payment method
- **Webhook signature verification failed**: Check webhook secret

---

## 13. Production Checklist

- [ ] Switch to live API keys (pk_live and sk_live)
- [ ] Configure production webhook endpoint
- [ ] Set up proper error logging
- [ ] Implement retry logic for failed webhooks
- [ ] Add rate limiting on payment endpoints
- [ ] Enable Stripe Radar for fraud detection
- [ ] Set up email notifications for payments
- [ ] Configure proper SSL/TLS certificates
- [ ] Test refund flow
- [ ] Document dispute handling process

---

## Notes
- Payments are tied to jobs (one payment per job)
- Payment amounts are in the currency's base unit (e.g., dollars, not cents in the API)
- Stripe handles amounts in cents internally
- All timestamps use UTC
- Payment status flows: Pending → Completed/Failed/Refunded
