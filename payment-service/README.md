# StartupIMS Payment Service (Java / Spring Boot + Razorpay)

Phase 2 microservice: handles funding disbursement via **Razorpay, in Test
Mode**, once an Admin approves a funding request in the main .NET API. Kept
as a separate service with its own database (`startupims_payments`) - the
real polyglot microservice extraction the rest of StartupIMS was designed
to allow.

**Not built or run in this environment** - Maven isn't installed here and
Maven Central isn't reachable from this sandbox, and Razorpay obviously
can't be exercised without a real (test-mode) account. The Razorpay Java
SDK usage here was verified against Razorpay's own documentation and
GitHub source (`RazorpayClient`, `paymentLink.create`,
`Utils.verifyWebhookSignature`) rather than guessed, but it has not been
compiled or run - budget real debugging time for your first build.

## The flow

1. Admin approves a funding request in the React app → `PUT /api/Funding/{id}/approval` on the .NET API.
2. .NET's `FundingController` calls this service's `POST /api/payments/initiate`, authenticated with a shared `X-Service-Key` header (service-to-service, not a JWT).
3. This service creates a **Razorpay Payment Link** (via the Razorpay Java SDK), storing it as a `PENDING` `PaymentTransaction` row, and returns the link's `short_url` to .NET.
4. Someone (you, manually, for now - see below) sends that URL to the founder. They open it and pay using Razorpay's **test card/UPI details** (real bank/card details are never used or accepted in test mode).
5. Razorpay calls this service's `POST /api/payments/webhook` with the `payment_link.paid` event, signed with your webhook secret.
6. This service verifies the signature, looks up the `PaymentTransaction` by the `reference_id` Razorpay echoes back (which is *our* generated session id), marks it `SUCCESS`, and calls back into .NET's `POST /api/Funding/payment-webhook` to mark the `FundingRequest` as `Disbursed`.

## Setting up a Razorpay test account

1. Sign up at https://dashboard.razorpay.com/signup (no business verification needed to use Test Mode).
2. In the dashboard, confirm the **Test Mode** toggle (top right) is ON.
3. Go to **Settings → API Keys → Generate Test Key**. Copy the `key_id` (starts with `rzp_test_`) and `key_secret` - the secret is only shown once.
4. Go to **Settings → Webhooks → Add New Webhook**:
   - URL: needs to be a publicly reachable address pointing at this service's `/api/payments/webhook` - Razorpay cannot call `localhost` directly. Use a tunnel like `ngrok http 8081` during development, and set the webhook URL to the `https://xxxx.ngrok.io/api/payments/webhook` address ngrok gives you.
   - Active events: check **`payment_link.paid`** at minimum.
   - Set a **Secret** here - this is your `webhook-secret`, a *different* value from `key_secret`.

## Two different shared secrets (unrelated to Razorpay - service-to-service)

| Direction | .NET config key | Java config key | Must match |
|---|---|---|---|
| .NET → Java (`/payments/initiate`) | `PaymentService:OutboundApiKey` | `startupims.inbound-api-key` | Yes, same value |
| Java → .NET (`/Funding/payment-webhook`) | `PaymentService:InboundApiKey` | `startupims.api.service-key` | Yes, same value |

These have nothing to do with your Razorpay keys - they're a separate,
simple mechanism protecting the two API calls between your own two
services. Never commit real values for any of these five secrets.

## Build and run locally

```bash
mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS startupims_payments;"

export DB_PASSWORD=your_mysql_password
export INBOUND_API_KEY=some-long-random-string-a
export OUTBOUND_API_KEY=some-long-random-string-b
export STARTUPIMS_API_BASE_URL=https://localhost:61901
export RAZORPAY_KEY_ID=rzp_test_xxxxxxxxxxxx
export RAZORPAY_KEY_SECRET=your_test_key_secret
export RAZORPAY_WEBHOOK_SECRET=your_webhook_secret_from_dashboard

mvn spring-boot:run
```

Set the matching values in the .NET side's User Secrets:
```
dotnet user-secrets set "PaymentService:OutboundApiKey" "some-long-random-string-a" --project src/StartupIMS.API
dotnet user-secrets set "PaymentService:InboundApiKey" "some-long-random-string-b" --project src/StartupIMS.API
dotnet user-secrets set "PaymentService:BaseUrl" "http://localhost:8081" --project src/StartupIMS.API
```

## Testing the full flow with real Razorpay test mode

1. Start ngrok (`ngrok http 8081`), update the webhook URL in the Razorpay dashboard to match the current tunnel address (ngrok's free tier gives you a new URL each restart - update the dashboard each time, or use a paid/static ngrok domain).
2. Start both services, MySQL with `startupims_payments` created.
3. In the React app: log in as Admin, approve a funding request.
4. Check the `payment_transactions` table for a new `PENDING` row, and check this service's logs for "Initiated payment session..." along with a `short_url`.
5. Open that `short_url` in a browser. Razorpay's test checkout page appears.
6. Pay using Razorpay's published test details, e.g.:
   - **Test card**: `4111 1111 1111 1111`, any future expiry, any CVV
   - **Test UPI**: `success@razorpay` (always succeeds) or `failure@razorpay` (always fails)
   - Full current list: https://razorpay.com/docs/payments/payments/test-card-upi-details/
7. On successful test payment, Razorpay calls your webhook. Check `payment_transactions.status` is now `SUCCESS`.
8. Check the .NET side: `GET /api/Funding/{id}` should show `approvalStatus: "Disbursed"`, and the founder should receive the email.

## What's still a placeholder / next steps

- **The founder never automatically receives the payment link** - right now it's only logged/returned to .NET, which doesn't yet forward it into the approval email. Wire `paymentUrl` into the existing "Funding request {status}" email in `FundingController.UpdateApproval`, or set `notify.email: true` in `RazorpayPaymentGatewayClient` and let Razorpay email it directly (simpler, but loses your own email branding/copy).
- **Only `payment_link.paid` is handled.** `payment_link.cancelled`/`payment_link.expired`/failed payment events exist in Razorpay's webhook event set and aren't processed - a real production integration should handle these too, marking the transaction `FAILED` rather than leaving it `PENDING` forever.
- **Retry/reconciliation** for `callback_delivered = false` rows (payment succeeded, but the callback to .NET failed) - still not built, same as before.
- **Going live**: swapping `rzp_test_...` keys for live keys (after Razorpay's business verification) is the only change needed to go from test to real payments - the code itself doesn't change.
