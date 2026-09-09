package com.startupims.payment.controller;

import com.razorpay.Utils;
import com.startupims.payment.config.RazorpaySettings;
import com.startupims.payment.service.PaymentService;
import org.json.JSONObject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/payments")
public class WebhookController {

    private static final Logger log = LoggerFactory.getLogger(WebhookController.class);

    private final PaymentService paymentService;
    private final RazorpaySettings razorpaySettings;

    public WebhookController(PaymentService paymentService, RazorpaySettings razorpaySettings) {
        this.paymentService = paymentService;
        this.razorpaySettings = razorpaySettings;
    }

    /**
     * Razorpay calls this directly when a Payment Link is paid (or fails).
     * Configure this URL in your Razorpay Dashboard -> Settings -> Webhooks,
     * subscribed to at least the "payment_link.paid" event. In local dev,
     * Razorpay can't reach localhost directly - use a tunnel (ngrok, or
     * Razorpay's own webhook testing tool) so it can reach this endpoint.
     *
     * IMPORTANT: the signature is computed over the RAW request body exactly
     * as Razorpay sent it - do not let Spring deserialize this into a typed
     * object first, since re-serializing it back to a string for
     * verification is not guaranteed to produce byte-identical output
     * (whitespace/key ordering can differ). Taking it as a raw String
     * parameter is what keeps the signature check reliable.
     */
    @PostMapping("/webhook")
    public ResponseEntity<Void> handleWebhook(
            @RequestBody String rawBody,
            @RequestHeader("X-Razorpay-Signature") String signature) {

        boolean valid;
        try {
            valid = Utils.verifyWebhookSignature(rawBody, signature, razorpaySettings.getWebhookSecret());
        } catch (Exception e) {
            log.warn("Webhook signature verification threw an exception - rejecting", e);
            return ResponseEntity.status(400).build();
        }

        if (!valid) {
            log.warn("Webhook signature verification failed - rejecting request");
            return ResponseEntity.status(400).build();
        }

        JSONObject body = new JSONObject(rawBody);
        String event = body.optString("event");

        // Only the "paid" event actually means money moved. Other events
        // (payment_link.cancelled, payment_link.expired, payment.failed,
        // etc.) exist in Razorpay's event set but are not handled here -
        // add more `else if` branches as needed for a fuller integration.
        if (!"payment_link.paid".equals(event) && !"payment.failed".equals(event)) {
            log.info("Ignoring Razorpay webhook event type: {}", event);
            return ResponseEntity.ok().build();
        }

        if (!body.getJSONObject("payload").has("payment_link")) {
            log.warn("Webhook payload does not contain payment_link entity. Cannot extract reference_id.");
            return ResponseEntity.ok().build();
        }

        JSONObject paymentLinkEntity = body
                .getJSONObject("payload")
                .getJSONObject("payment_link")
                .getJSONObject("entity");

        String referenceId = paymentLinkEntity.optString("reference_id"); // this is OUR sessionId, round-tripped back to us

        String paymentId = null;
        if (body.getJSONObject("payload").has("payment")) {
            paymentId = body.getJSONObject("payload")
                    .getJSONObject("payment")
                    .getJSONObject("entity")
                    .optString("id");
        }

        String status = "payment_link.paid".equals(event) ? "SUCCESS" : "FAILED";
        paymentService.handleGatewayWebhook(referenceId, status, paymentId);
        return ResponseEntity.ok().build();
    }
}
