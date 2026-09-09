package com.startupims.payment.gateway;

/**
 * Abstraction over the actual payment gateway (Razorpay/Stripe/etc). Only a
 * mock implementation exists right now - swapping in a real gateway later
 * means writing one new class implementing this interface and pointing
 * Spring's dependency injection at it, without touching PaymentService.
 */
public interface PaymentGatewayClient {

    /**
     * Starts a payment session with the gateway and returns a URL the founder
     * would be redirected to complete payment.
     */
    GatewaySession createSession(String sessionId, java.math.BigDecimal amount, String currency);

    record GatewaySession(String paymentUrl) {}
}
