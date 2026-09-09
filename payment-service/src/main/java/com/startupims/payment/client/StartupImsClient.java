package com.startupims.payment.client;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestClient;

/**
 * Calls back into the ASP.NET Core API to report a payment result. This is
 * genuine service-to-service communication - not a user request - so it uses
 * a shared static API key (X-Service-Key header) rather than a JWT. A JWT
 * belongs to a logged-in human; this call has no human behind it. See the
 * .NET side's PaymentWebhookController for the matching validation.
 */
@Component
public class StartupImsClient {

    private final RestClient restClient;
    private final String serviceKey;

    public StartupImsClient(
            @Value("${startupims.api.base-url}") String baseUrl,
            @Value("${startupims.api.service-key}") String serviceKey) {
        this.restClient = RestClient.builder().baseUrl(baseUrl).build();
        this.serviceKey = serviceKey;
    }

    public void notifyFundingPaymentResult(Integer fundingRequestId, String status, String transactionReference) {
        record FundingWebhookRequest(Integer fundingRequestId, String status, String transactionReference) {}

        restClient.post()
                .uri("/api/Funding/payment-webhook")
                .header("X-Service-Key", serviceKey)
                .body(new FundingWebhookRequest(fundingRequestId, status, transactionReference))
                .retrieve()
                .toBodilessEntity();
    }
}
