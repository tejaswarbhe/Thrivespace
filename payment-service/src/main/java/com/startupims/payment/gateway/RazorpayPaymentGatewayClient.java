package com.startupims.payment.gateway;

import com.razorpay.PaymentLink;
import com.razorpay.RazorpayClient;
import com.razorpay.RazorpayException;
import com.startupims.payment.config.RazorpaySettings;
import org.json.JSONObject;
import org.springframework.stereotype.Component;

import java.math.BigDecimal;

/**
 * Real Razorpay integration using the Payment Links API - a founder gets a
 * payable URL (which Razorpay can also email/SMS directly) rather than an
 * embedded checkout widget, which fits this project's async
 * "Admin approves, founder gets notified, founder pays later" flow better
 * than Razorpay's standard Checkout.js overlay (which expects the customer
 * to be actively present in a browser session at creation time).
 *
 * Uses Razorpay TEST MODE credentials (key_id starting with "rzp_test_").
 * Test-mode payment links behave identically to live ones but never move
 * real money - see https://razorpay.com/docs/payments/payments/test-card-upi-details/
 * for test card/UPI numbers to complete a test payment.
 */
@Component
public class RazorpayPaymentGatewayClient implements PaymentGatewayClient {

    private final RazorpaySettings settings;

    public RazorpayPaymentGatewayClient(RazorpaySettings settings) {
        this.settings = settings;
    }

    @Override
    public GatewaySession createSession(String sessionId, BigDecimal amount, String currency) {
        try {
            RazorpayClient razorpay = new RazorpayClient(settings.getKeyId(), settings.getKeySecret());

            // Razorpay amounts are in the smallest currency unit (paise for
            // INR, cents for USD) - multiply by 100 and drop to a whole number.
            long amountInSubunits = amount.multiply(BigDecimal.valueOf(100)).longValue();

            JSONObject request = new JSONObject();
            request.put("amount", amountInSubunits);
            request.put("currency", currency);
            request.put("reference_id", sessionId); // comes back in the webhook payload - this is how we correlate the callback to our own PaymentTransaction row
            request.put("description", "StartupIMS funding disbursement");

            JSONObject notify = new JSONObject();
            notify.put("email", false); // this service sends its own emails via the .NET API's notification flow instead
            notify.put("sms", false);
            request.put("notify", notify);

            request.put("reminder_enable", false);

            // Without this, Razorpay leaves the browser on its own success
            // page after payment instead of returning the user to the app.
            request.put("callback_url", settings.getCallbackUrl());
            request.put("callback_method", "get");

            PaymentLink paymentLink = razorpay.paymentLink.create(request);
            String shortUrl = paymentLink.get("short_url");

            return new GatewaySession(shortUrl);
        } catch (RazorpayException e) {
            throw new RuntimeException("Failed to create Razorpay payment link: " + e.getMessage(), e);
        }
    }
}
