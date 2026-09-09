package com.startupims.payment.entity;

import jakarta.persistence.*;
import java.math.BigDecimal;
import java.time.Instant;

/**
 * This service's own record of a payment attempt. Deliberately NOT the same
 * table as .NET's FundingRequests - this is a separate bounded context with
 * its own database, exactly the kind of module boundary the rest of
 * StartupIMS was designed around. fundingRequestId is a plain foreign-key
 * value pointing back into the .NET service's data, not a JPA relationship -
 * there is no way to join across two different databases/services, and this
 * service should never try to.
 */
@Entity
@Table(name = "payment_transactions")
public class PaymentTransaction {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false)
    private Integer fundingRequestId; // FK into .NET's FundingRequests.Id - not enforced here

    @Column(nullable = false, precision = 18, scale = 2)
    private BigDecimal amount;

    @Column(nullable = false, length = 10)
    private String currency = "USD";

    @Column(nullable = false, unique = true, length = 100)
    private String sessionId; // our own generated reference, returned to the gateway as the payment session

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 20)
    private PaymentStatus status = PaymentStatus.PENDING;

    @Column(length = 200)
    private String gatewayReference; // populated once the gateway confirms

    @Column(nullable = false)
    private Instant createdAt = Instant.now();

    private Instant completedAt;

    // Whether the callback to .NET succeeded - lets an admin/ops process spot
    // and retry cases where the payment succeeded but .NET was unreachable.
    @Column(nullable = false)
    private boolean callbackDelivered = false;

    protected PaymentTransaction() {
        // JPA requires a no-arg constructor
    }

    public PaymentTransaction(Integer fundingRequestId, BigDecimal amount, String currency, String sessionId) {
        this.fundingRequestId = fundingRequestId;
        this.amount = amount;
        this.currency = currency;
        this.sessionId = sessionId;
    }

    // --- Getters and setters ---

    public Long getId() { return id; }

    public Integer getFundingRequestId() { return fundingRequestId; }

    public BigDecimal getAmount() { return amount; }

    public String getCurrency() { return currency; }

    public String getSessionId() { return sessionId; }

    public PaymentStatus getStatus() { return status; }
    public void setStatus(PaymentStatus status) { this.status = status; }

    public String getGatewayReference() { return gatewayReference; }
    public void setGatewayReference(String gatewayReference) { this.gatewayReference = gatewayReference; }

    public Instant getCreatedAt() { return createdAt; }

    public Instant getCompletedAt() { return completedAt; }
    public void setCompletedAt(Instant completedAt) { this.completedAt = completedAt; }

    public boolean isCallbackDelivered() { return callbackDelivered; }
    public void setCallbackDelivered(boolean callbackDelivered) { this.callbackDelivered = callbackDelivered; }
}
