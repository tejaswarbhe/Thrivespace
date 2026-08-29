# Medium Interview & Project Viva Questions

This file contains intermediate questions covering system design decisions, security configurations, database isolation, and detailed component functions.

---

### Q1: What is a modular monolith, and why did you choose it for the .NET backend?
*   **Answer**: A modular monolith is a software design pattern where the code is written as a single deployable unit (monolith), but is split into distinct, loosely-coupled modules with strict boundaries.
    *   **Why we chose it**: It offers the simplicity of developing, testing, and deploying a single backend application (no complex microservice orchestration needed at the start), while maintaining clean architectural boundaries. This makes it trivial to split these modules into fully independent microservices in the future if one module (like Identity) needs to scale separately.

### Q2: What does the separate `IdentityDbContext` and `CoreDbContext` design achieve?
*   **Answer**:
    *   **Goal**: Physical and logical separation of concerns.
    *   **Details**: Even though they might run on the same MySQL instance, they point to separate database schemas (`startupims_identity` and `startupims_core`). There are **no** Entity Framework navigation properties crossing the boundaries (e.g., a Startup entity doesn't link to a User entity directly; it only stores a primitive `UserId` foreign key).
    *   **Benefit**: This isolates authentication logic from core business logic, preventing database coupling. If we decide to extract the identity service to an external OAuth provider (like Auth0 or Keycloak), we can do so without breaking any SQL joins or EF Core mapping relationships in our core database.

### Q3: How does the system handle security and validation for different roles?
*   **Answer**: The system uses role-based policies on the API endpoints. In .NET, this is achieved using policies like `AdminOnly`, `MentorOnly`, and `FounderOnly` mapped to claims inside the JWT token.
    *   For resource-level access (e.g., ensuring a Founder can only see *their own* startup details, whereas an Admin sees *all* startups), the controller checks the `User.FindFirst(ClaimTypes.NameIdentifier)` (User ID) from the JWT claims and matches it against the owner ID of the requested resource in the database.

---

### Q4: [What does this do?] What does the `X-Service-Key` header do?
*   **Answer**: It facilitates secure **service-to-service (S2S)** communication between the .NET backend API and the Java payment microservice.
    *   **Why it's needed**: When an Admin approves funding, the request is routed from .NET to Java. Since this is a server-to-server request, there is no active user context or user JWT. Instead, we use a shared secret key passed in the `X-Service-Key` header. Both services are configured with this secret, and any request lacking it or having an incorrect key is immediately rejected with a `401 Unauthorized` status.

### Q5: [What does this do?] What does `Utils.verifyWebhookSignature` do in the Payment Service?
*   **Answer**: When Razorpay fires a webhook (e.g., when a user makes a payment), it sends the event data along with a cryptographic signature header. `verifyWebhookSignature` recalculates the HMAC-SHA256 signature of the request payload using a local secret key (configured during setup) and compares it with the signature sent by Razorpay.
    *   This ensures that the webhook request actually originated from Razorpay and that the payment details were not intercepted or forged by a malicious third party.

---

### Q6: [What if we remove this?] What if we remove Refresh Tokens and only use JWT Access Tokens?
*   **Answer**:
    *   **Effect**: We are forced to make a trade-off between security and user experience.
    *   **If Access Tokens have a short lifespan (e.g., 15 mins)**: Users will be logged out and forced to re-login every 15 minutes, which is highly disruptive.
    *   **If Access Tokens have a long lifespan (e.g., 30 days)**: If an attacker steals the JWT, they can access the account for 30 days. Because JWTs are stateless, it is extremely difficult to revoke them before they expire.
    *   **Conclusion**: Removing refresh tokens compromises security or ruins the user experience.

### Q7: [What if we merge `IdentityDbContext` and `CoreDbContext` into a single DbContext?
*   **Answer**:
    *   **Effect**: The codebase will experience tighter coupling. Developers will likely start adding direct relationships (foreign keys and navigation properties) between entities like `Startup` and `User`.
    *   **Consequence**: It becomes extremely difficult to migrate user identity management to a separate microservice or third-party auth service (like Keycloak) in the future. You would have to refactor the entire database schema and rewrite significant portions of the EF Core queries to eliminate joins between user tables and business tables.

### Q8: [What if we remove Razorpay webhook verification?
*   **Answer**:
    *   **Effect**: The payment gateway will still process payments, but our backend becomes highly vulnerable to **financial fraud**.
    *   **Attack Vector**: An attacker could capture the JSON payload structure of a successful payment and manually send a POST request to our `/api/payments/webhook` endpoint with a spoofed transaction ID. If signature verification is missing, our Payment Service will mark the transaction as `SUCCESS`, prompting the .NET API to disburse funds to a startup without any real money actually being paid. 

### Q9: [What if we remove the Webhook Controller and rely on the frontend redirect to confirm payments?
*   **Answer**:
    *   **Effect**: Extremely unreliable and insecure database updates.
    *   **Why**: If a user pays on Razorpay's checkout page, and then closes their browser tab, experiences a network drop, or gets redirected but the frontend fails to send the API request to our backend, the system will never record that the payment succeeded. Webhooks are server-to-server notifications sent asynchronously, ensuring status updates succeed even if the user's browser crashes or is closed immediately after payment.
