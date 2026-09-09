namespace StartupIMS.Shared.Settings;

public class PaymentServiceSettings
{
    public const string SectionName = "PaymentService";

    public string BaseUrl { get; set; } = "http://localhost:8081";

    // Sent by THIS API when calling the Java service's /api/payments/initiate
    public string OutboundApiKey { get; set; } = string.Empty;

    // Expected from the Java service when IT calls back into
    // /api/Funding/payment-webhook - a different secret than OutboundApiKey
    // on purpose, since these are two distinct trust directions.
    public string InboundApiKey { get; set; } = string.Empty;
}
