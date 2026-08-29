namespace StartupIMS.Shared.DTOs;

// What we send to the Java service's POST /api/payments/initiate
public record InitiatePaymentRequest(int FundingRequestId, decimal Amount, string Currency);

// What the Java service replies with
public record InitiatePaymentResponse(string SessionId, string PaymentUrl, string Status);

// What the Java service sends back to US at POST /api/Funding/payment-webhook,
// once the gateway confirms success/failure
public record FundingPaymentWebhookRequest(int FundingRequestId, string Status, string TransactionReference);
