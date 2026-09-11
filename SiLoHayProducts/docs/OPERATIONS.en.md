# SiLoHay Products — Runbook & Deployment Guide (Stripe Checkout)

Operational documentation for the SiLoHay Razor Pages app after Stripe one-time Checkout integration.

---

## 1) Overview

| Item | Details |
|------|---------|
| Stack | ASP.NET Core **Razor Pages** |
| Runtime | **.NET 9** |
| Project | `SiLoHayProducts/SiLoHayProducts.csproj` |
| Solution | `SiLoHayProducts.sln` |
| Database | SQL Server (local or Azure SQL) |
| Payments | Stripe Checkout Session (`mode=payment`) |
| Tax | Manual **11.5%** Tax Rate on `line_items.tax_rates` (**not** Stripe Tax) |
| Subscriptions | Not used |

### Payment flows

1. **Available product** (`IsAvailableNow = true`) → **Full** payment (Card + Klarna).
2. **Unavailable product** (`IsAvailableNow = false`) → **Deposit** from `Prepayment` (Card only) → later **Remaining** = `OurPrice - Prepayment` (Card + Klarna) on the **same order**.
3. Staff generates a Checkout URL + QR and sends it to the customer (WhatsApp/SMS).
4. Customer pays on Stripe-hosted Checkout.
5. Payment confirmation via **webhooks** (source of truth) + optional Success-page sync as backup.

---

## 2) Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (Express / Azure SQL / equivalent)
- Stripe account (Test, then Live)
- Optional for local webhooks: [Stripe CLI](https://stripe.com/docs/stripe-cli)
- Visual Studio 2022, VS Code, or Cursor

---

## 3) Important repository layout

```
SiLoHayProducts/
  Program.cs                          # DI + webhook API endpoint
  appsettings.json                    # Non-secret defaults
  appsettings.Development.json
  Data/
    Product.cs                        # GetProducts / GetById / Add
    OrderModels.cs                    # Order/Payment status constants
    OrderRepository.cs                # Orders, payments, webhook events
  Options/StripeOptions.cs
  Services/
    StripeCheckoutService.cs          # Creates Checkout Sessions
    CheckoutOrchestrator.cs           # Full / Deposit / Remaining
    StripeWebhookProcessor.cs         # Webhook handling + Success sync
  Pages/
    Index.*                           # Catalog + “Pay with Stripe”
    Checkout.*                        # Creates session and redirects
    PaymentSuccess.* / PaymentCancel.*
    StripeWebhook.*                   # Alternate Razor webhook page
    Staff/GeneratePaymentLink.*       # Admin: URL + QR
  Sql/
    LocalSiLoHayFullSchema.sql        # Full local schema
    CreateOrderTables.sql             # Order tables only
    CopyProductsFromAzure.ps1         # Optional product copy script
  docs/
    OPERATIONS.md                     # Arabic runbook
    OPERATIONS.en.md                  # This English runbook
```

### New payment tables

| Table | Purpose |
|-------|---------|
| `SiLoHayOrder` | Order header: prices, deposit/remaining, status |
| `SiLoHayOrderPayment` | Each Stripe payment (Full / Deposit / Remaining) |
| `SiLoHayStripeEvent` | Webhook event idempotency |

### Order statuses

- `PendingPayment`
- `DepositPaid`
- `FullyPaid`
- `PaymentFailed`

### Payment statuses

- `Pending` / `Paid` / `Failed` / `Expired`

---

## 4) Database setup

### A) Local SQL Server

1. Create database `SiLoHay` if needed.
2. Run:

```text
Sql/LocalSiLoHayFullSchema.sql
```

This creates:

- `SiLoHayProduct` + stored procedure `usp_TienditaGetItems`
- `SiLoHayOrder` / `SiLoHayOrderPayment` / `SiLoHayStripeEvent`

3. Optional: copy catalog products from Azure:

```powershell
powershell -ExecutionPolicy Bypass -File Sql/CopyProductsFromAzure.ps1
```

4. Set connection string in `appsettings.json` (example):

```json
"SiLoHayConnString": "Server=YOUR_PC\\SQLEXPRESS;Database=SiLoHay;Integrated Security=True;TrustServerCertificate=True;Pooling=True;"
```

### B) Client Azure / production SQL

1. At minimum, run `Sql/CreateOrderTables.sql` (write permissions required).
2. Ensure existing `SiLoHayProduct` and `usp_TienditaGetItems` remain available (original app).
3. Store the production connection string in **App Settings / secrets**, not in Git.

> Note: a read-only SQL login cannot insert orders/payments. Use a read/write account for the web app.

---

## 5) Stripe configuration

### Keys (do not commit secrets)

Locally, use **User Secrets**:

```powershell
cd SiLoHayProducts
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
dotnet user-secrets set "Stripe:TaxRateId" "txr_..."
dotnet user-secrets set "Stripe:StaffAccessKey" "your-strong-staff-key"
dotnet user-secrets set "Stripe:Currency" "usd"
```

Or environment / Azure App Settings:

```text
Stripe__SecretKey
Stripe__PublishableKey
Stripe__WebhookSecret
Stripe__TaxRateId
Stripe__StaffAccessKey
Stripe__Currency
Stripe__KlarnaMinAmountUsd   (optional)
```

### Manual 11.5% Tax Rate

1. Stripe Dashboard → create a Tax Rate of **11.5%** (inclusive = false).
2. Save the Tax Rate ID (`txr_...`) as `Stripe:TaxRateId`.
3. Use a **separate Live Tax Rate** when switching to production keys.

### Payment method rules (in code)

| Payment type | `payment_method_types` |
|--------------|-------------------------|
| Deposit | `card` only |
| Full / Remaining | `card` + `klarna` (unless below optional `KlarnaMinAmountUsd`) |

- Link is disabled via `wallet_options.link.display = never`.
- **Apple Pay** may still appear as an express wallet when Card is enabled. Disable Apple Pay in the Stripe Dashboard Payment methods settings if you want it hidden.

---

## 6) Running locally

### Start the app

```powershell
cd SiLoHayProducts
dotnet restore
dotnet run --launch-profile http
```

Or use Visual Studio profiles **http** / **https**.

| Profile | URL |
|---------|-----|
| http | `http://localhost:5224` |
| https | `https://localhost:7249` |

### Local webhooks (required for reliable DB updates)

```powershell
stripe listen --forward-to http://localhost:5224/api/stripe/webhook --events checkout.session.completed,checkout.session.async_payment_succeeded,checkout.session.async_payment_failed
```

Copy the printed `whsec_...` into User Secrets (`Stripe:WebhookSecret`), then **restart the app**.

Preferred endpoint: **`POST /api/stripe/webhook`**  
Alternate Razor endpoint: `POST /StripeWebhook`

### Important pages

| Page | Audience | Purpose |
|------|----------|---------|
| `/` or `/Index` | Customer | Catalog + **Pay with Stripe** |
| `/Checkout` | System | Creates Checkout Session and redirects |
| `/PaymentSuccess` | Customer | Thank-you + backup Stripe session sync |
| `/PaymentCancel` | Customer | Cancelled checkout (no charge) |
| `/Staff/GeneratePaymentLink?key=STAFF_KEY` | **Staff/Admin only** | Generate Checkout URL + QR |
| `/AddProduct` | Admin (currently weakly protected) | Add product |
| `/ContactUs` | Customer | Contact form |

Staff key comes from `Stripe:StaffAccessKey` (Development example previously used: `dev-staff-key`).

---

## 7) Quick test scenarios

### A) Full payment

1. Pick a product with `IsAvailableNow = 1`.
2. Click **Pagar ahora con Stripe** → pay with test card `4242 4242 4242 4242`.
3. Expect Success page and DB: Order `FullyPaid`, Payment type `Full` / `Paid`.

### B) Deposit + Remaining

1. Pick unavailable product with valid `Prepayment` (local example: ProductId **32**, deposit `$7.00`, remaining `$17.50`).
2. Pay deposit → Order `DepositPaid`.
3. Open Staff page → select that order → generate Remaining URL/QR → send to customer.
4. Pay remaining → Order `FullyPaid`, Payment type `Remaining` / `Paid`.

### C) Cancel

1. Start Checkout, then use Stripe’s back arrow / leave checkout.
2. App should land on `/PaymentCancel`.
3. Order stays `PendingPayment`; payment stays `Pending`.

### D) Webhook idempotency

1. Resend the same Stripe event.
2. `SiLoHayStripeEvent` should prevent duplicate side effects.

### Stripe test cards

- Success: `4242 4242 4242 4242`
- More cases: [Stripe Testing docs](https://stripe.com/docs/testing)

---

## 8) Deployment

### Recommended: Azure App Service + Azure SQL

1. **Database**
   - Apply `CreateOrderTables.sql` to production.
   - Use a **read/write** connection string.

2. **App Settings (Production)**

```text
ConnectionStrings__SiLoHayConnString = ...
Stripe__SecretKey = sk_live_...
Stripe__PublishableKey = pk_live_...
Stripe__WebhookSecret = whsec_...
Stripe__TaxRateId = txr_...
Stripe__StaffAccessKey = (strong secret)
Stripe__Currency = usd
ASPNETCORE_ENVIRONMENT = Production
```

3. **Publish**

```powershell
dotnet publish -c Release -o .\publish
```

Deploy the `publish` folder (Zip Deploy, GitHub Actions, Visual Studio Publish, etc.).

4. **Stripe webhook (Test and/or Live)**

- Endpoint URL: `https://YOUR-DOMAIN/api/stripe/webhook`
- Events:
  - `checkout.session.completed`
  - `checkout.session.async_payment_succeeded`
  - `checkout.session.async_payment_failed`
- Save the signing secret as `Stripe__WebhookSecret`.

5. **HTTPS** is required in production. Success/Cancel URLs are built from the current request host automatically.

### Go-live checklist

- [ ] Switch to **Live** Stripe keys  
- [ ] Create/use **Live** 11.5% Tax Rate ID  
- [ ] Production webhook returns 200 and inserts into `SiLoHayStripeEvent`  
- [ ] Strong non-empty `StaffAccessKey`  
- [ ] Restrict `/AddProduct` if needed  
- [ ] Decide Apple Pay visibility in Dashboard  
- [ ] No secrets committed to Git  

### IIS alternative

1. `dotnet publish -c Release`
2. Install [.NET 9 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0)
3. Host the published site under IIS with HTTPS
4. Provide the same settings via environment variables / Azure Key Vault / secure config

---

## 9) Who does what?

| Role | Action |
|------|--------|
| **Customer** | Browses `/`, pays via **Pagar ahora**, or opens a staff-sent URL/QR |
| **Staff/Admin** | Uses `/Staff/GeneratePaymentLink` to create Full/Deposit or Remaining links + QR for WhatsApp/SMS |
| **System** | Creates Checkout Sessions, receives webhooks, updates Order/Payment status |

---

## 10) Security notes

- Never commit Live secret keys or webhook secrets.
- Do not treat the Success redirect alone as payment confirmation; webhooks are authoritative.
- Prices are loaded server-side (`GetById`); never trust browser-submitted amounts.
- Query-string `StaffAccessKey` is acceptable for early staging only; prefer real authentication in production.
- Rotate any credentials that were previously stored in source control.

---

## 11) Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| Paid in Stripe, DB still Pending | Webhook failing / CLI stopped | Run `stripe listen`, match `whsec`, restart app; Success page can sync as backup |
| `TaxRateId is not configured` | Missing setting | Set `Stripe:TaxRateId` |
| Deposit checkout rejected | Invalid `Prepayment` | Prepayment must be > 0 and < OurPrice |
| Staff page access denied | Wrong key | Use matching `?key=` value |
| Missing tables | Script not applied | Run `CreateOrderTables.sql` / full schema |
| Cannot insert orders | Read-only SQL user | Switch to read/write login |

### Useful SQL

```sql
SELECT TOP 20 * FROM dbo.SiLoHayOrder ORDER BY OrderId DESC;
SELECT TOP 20 * FROM dbo.SiLoHayOrderPayment ORDER BY PaymentId DESC;
SELECT TOP 20 * FROM dbo.SiLoHayStripeEvent ORDER BY ProcessedAtUtc DESC;
```

---

## 12) Quick command reference

```powershell
# Run
dotnet run --launch-profile http

# Secrets
dotnet user-secrets list

# Local webhook forwarding
stripe listen --forward-to http://localhost:5224/api/stripe/webhook

# Publish
dotnet publish -c Release -o .\publish
```

---

## 13) Delivered functionality (summary)

- One-time Stripe Checkout with manual 11.5% tax.
- Full payment vs Deposit + Remaining on the same order.
- Card-only deposits; Card + Klarna for full/remaining.
- Staff page to generate payment URL + QR code.
- Success and Cancel pages.
- Order/payment status tracking in SQL via webhooks (+ Success sync backup).
- Cookie consent banner wired into the site layout.

---

*Document version aligned with the .NET 9 + Stripe Checkout + SiLoHayOrder implementation.*
