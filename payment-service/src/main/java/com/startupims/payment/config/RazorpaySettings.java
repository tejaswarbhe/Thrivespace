package com.startupims.payment.config;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

@Component
public class RazorpaySettings {

    @Value("${razorpay.key-id}")
    private String keyId;

    @Value("${razorpay.key-secret}")
    private String keySecret;

    @Value("${razorpay.webhook-secret}")
    private String webhookSecret;

    public String getKeyId() { return keyId; }
    public String getKeySecret() { return keySecret; }
    public String getWebhookSecret() { return webhookSecret; }
}
