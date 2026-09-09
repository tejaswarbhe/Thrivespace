package com.startupims.payment.repository;

import com.startupims.payment.entity.PaymentTransaction;
import org.springframework.data.jpa.repository.JpaRepository;
import java.util.Optional;

public interface PaymentTransactionRepository extends JpaRepository<PaymentTransaction, Long> {
    Optional<PaymentTransaction> findBySessionId(String sessionId);
}
