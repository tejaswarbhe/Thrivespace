package com.startupims.payment.dto;

import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import java.math.BigDecimal;

public class PaymentDtos {

    // What .NET sends us to start a payment for an approved funding request
    public record InitiatePaymentRequest(
        @NotNull Integer fundingRequestId,
        @NotNull @DecimalMin("0.01") BigDecimal amount,
        @NotBlank String currency
    ) {}

    // What we hand back to .NET - a session id and a URL the founder would be
    // redirected to (or emailed, via Razorpay's own notify options) to pay
    public record InitiatePaymentResponse(
        String sessionId,
        String paymentUrl,
        String status
    ) {}
}
