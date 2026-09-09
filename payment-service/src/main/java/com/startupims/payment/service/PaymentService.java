package com.startupims.payment.service;

import com.startupims.payment.client.StartupImsClient;
import com.startupims.payment.dto.PaymentDtos.*;
import com.startupims.payment.entity.PaymentStatus;
import com.startupims.payment.entity.PaymentTransaction;
import com.startupims.payment.gateway.PaymentGatewayClient;
import com.startupims.payment.repository.PaymentTransactionRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.util.UUID;

@Service
public class PaymentService {

    private static final Logger log = LoggerFactory.getLogger(PaymentService.class);

    private final PaymentTransactionRepository repository;
    private final PaymentGatewayClient gateway;
    private final StartupImsClient startupImsClient;

    public PaymentService(PaymentTransactionRepository repository, PaymentGatewayClient gateway, StartupImsClient startupImsClient) {
        this.repository = repository;
        this.gateway = gateway;
        this.startupImsClient = startupImsClient;
    }

    public InitiatePaymentResponse initiate(InitiatePaymentRequest request) {
        String sessionId = "pay_" + UUID.randomUUID().toString().replace("-", "");

        var transaction = new PaymentTransaction(request.fundingRequestId(), request.amount(), request.currency(), sessionId);
        repository.save(transaction);

        var session = gateway.createSession(sessionId, request.amount(), request.currency());

        log.info("Initiated payment session {} for fundingRequestId {}", sessionId, request.fundingRequestId());
        return new InitiatePaymentResponse(sessionId, session.paymentUrl(), PaymentStatus.PENDING.name());
    }

    public void handleGatewayWebhook(String sessionId, String status, String gatewayReference) {
        var transaction = repository.findBySessionId(sessionId)
                .orElseThrow(() -> new IllegalArgumentException("Unknown payment session: " + sessionId));

        // Idempotency: if we've already processed this session, don't do it
        // again - Razorpay (like most gateways) may retry webhook delivery.
        if (transaction.getStatus() != PaymentStatus.PENDING) {
            log.info("Session {} already processed as {} - ignoring duplicate webhook", sessionId, transaction.getStatus());
            return;
        }

        var newStatus = "SUCCESS".equalsIgnoreCase(status) ? PaymentStatus.SUCCESS : PaymentStatus.FAILED;
        transaction.setStatus(newStatus);
        transaction.setGatewayReference(gatewayReference);
        transaction.setCompletedAt(Instant.now());
        repository.save(transaction);

        try {
            startupImsClient.notifyFundingPaymentResult(
                    transaction.getFundingRequestId(),
                    newStatus == PaymentStatus.SUCCESS ? "SUCCESS" : "FAILED",
                    transaction.getSessionId());
            transaction.setCallbackDelivered(true);
            repository.save(transaction);
        } catch (Exception ex) {
            // Deliberately not rethrown - the payment result is already
            // recorded here regardless. A failed callback to .NET means
            // FundingRequests.ApprovalStatus is now out of sync and needs a
            // manual retry/reconciliation job - callbackDelivered=false flags
            // exactly which rows need that.
            log.error("Payment {} processed as {} but callback to StartupIMS API failed", sessionId, newStatus, ex);
        }
    }
}
