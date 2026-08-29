package com.startupims.payment.controller;

import com.startupims.payment.dto.PaymentDtos.InitiatePaymentRequest;
import com.startupims.payment.dto.PaymentDtos.InitiatePaymentResponse;
import com.startupims.payment.service.PaymentService;
import jakarta.validation.Valid;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/payments")
public class PaymentController {

    private final PaymentService paymentService;

    public PaymentController(PaymentService paymentService) {
        this.paymentService = paymentService;
    }

    // Called by the .NET API (with the X-Service-Key header - see
    // ApiKeyAuthFilter) once an Admin approves a funding request and the
    // founder needs to actually receive the money.
    @PostMapping("/initiate")
    public ResponseEntity<InitiatePaymentResponse> initiate(@Valid @RequestBody InitiatePaymentRequest request) {
        return ResponseEntity.ok(paymentService.initiate(request));
    }
}
