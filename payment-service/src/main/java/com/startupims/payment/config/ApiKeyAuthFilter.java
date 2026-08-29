package com.startupims.payment.config;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;

/**
 * Simple shared-secret authentication for endpoints only the .NET backend
 * should call (not an end user, not the payment gateway). A full
 * OAuth2 client-credentials flow was considered but was overkill for a
 * 2-person project at this scale - see the project documentation report's
 * "Design Decision Comparisons" section for the reasoning.
 */
@Component
public class ApiKeyAuthFilter extends OncePerRequestFilter {

    @Value("${startupims.inbound-api-key}")
    private String expectedApiKey;

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain)
            throws ServletException, IOException {

        // Only guard the initiate-payment endpoint - the gateway webhook has
        // its own separate verification (see WebhookController's comment).
        if (request.getRequestURI().startsWith("/api/payments/initiate")) {
            String providedKey = request.getHeader("X-Service-Key");
            if (providedKey == null || !providedKey.equals(expectedApiKey)) {
                response.setStatus(HttpServletResponse.SC_UNAUTHORIZED);
                response.setContentType("application/json");
                response.getWriter().write("{\"error\":\"Missing or invalid X-Service-Key header\"}");
                return;
            }
        }

        filterChain.doFilter(request, response);
    }
}
