# Hard Interview & Project Viva Questions

This file contains advanced questions covering architectural trade-offs, network failures, production readiness, high scalability, and event-driven patterns.

---

### Q1: What happens if the network drops right after Razorpay payment succeeds, but before the webhook reaches our payment service? How do we resolve this?
*   **Answer**: This is a classic distributed systems problem:
    *   **The Issue**: The founder has paid, and Razorpay's ledger says the transaction is successful, but the payment service database still shows the status as `PENDING`.
    *   **Mitigation Strategy (Reconciliation / Polling Job)**:
        1.  Implement a background worker (e.g., Spring Boot `@Scheduled` task or a .NET Hangfire job) that periodically queries the `payment_transactions` table for rows stuck in `PENDING` status for more than a set period of time (e.g., 30 minutes).
        2.  For each pending transaction, the worker calls Razorpay's API (`GET /v1/payment_links/{link_id}`) using the Razorpay SDK to fetch the latest state directly from the source of truth.
        3.  If Razorpay reports it as paid, the worker updates the status to `SUCCESS` and triggers the callback to .NET to mark the funding as disbursed.

### Q2: If the callback from the Java Payment Service to the .NET Backend API fails (e.g., .NET API is temporarily down), how do we guarantee eventual consistency?
*   **Answer**:
    *   **The Issue**: The Payment Service receives the webhook, marks the transaction as `SUCCESS`, but fails to alert the .NET API. Now, the transaction database says `SUCCESS` but the core database still says `Pending Disbursement`.
    *   **Solution**:
        *   **Outbox Pattern / Retries**: Instead of calling the .NET API synchronously in the webhook handler and failing if the connection is dropped, we write an event to an "outbox" table in the Payment database.
        *   A background job continuously polls this outbox table and attempts to deliver the callback to the .NET API. It retries with exponential backoff until it receives a `200 OK` from the .NET API.
        *   The webhook callback endpoint on the .NET API must also be **idempotent**, meaning if it receives the same payment confirmation multiple times, it only processes it once to prevent duplicate disbursements.

### Q3: How would you transition this system from Razorpay Test Mode to Live Mode?
*   **Answer**: Swapping modes is simple at the code level but requires business compliance:
    1.  **Code & Configurations**: Update the environment variables in the Payment Service container to use live Razorpay Key IDs and secrets (e.g., swap `rzp_test_...` with `rzp_live_...` credentials).
    2.  **Webhook Setup**: Create a new webhook subscription in the Razorpay Live Dashboard pointing to the production URL, and configure the production Webhook Secret.
    3.  **Compliance & Activation**: Provide business verification details (PAN, GSTIN, Bank account proof) in the Razorpay dashboard to activate the live payment account.

---

### Q4: [What does this do?] What does the modular monolithic structure of the .NET database context achieve during high traffic?
*   **Answer**: It allows us to scale database resources independently:
    *   Since `IdentityDbContext` and `CoreDbContext` do not cross boundaries via SQL joins, we can easily move them to physically separate database servers (e.g., an Identity database server and a Core database server).
    *   During high login traffic, we can scale the Identity database server (adding read replicas) without affecting the performance or load of the Core business database.

---

### Q5: [What if we remove this?] What if we completely remove the Java Payment Service and merge its logic into the .NET Backend?
*   **Answer**:
    *   **Pros**:
        *   **Architectural Simplicity**: Reduces infrastructure complexity. You only need to run and maintain one backend API instead of two services.
        *   **Single Tech Stack**: The entire backend is written in C#/.NET Core, lowering the skill threshold for developers.
        *   **Zero Service-to-Service Latency/Securing**: Removes the need for service-to-service key validations or HTTP network calls between services.
    *   **Cons**:
        *   **Coupling**: The payment gateway client logic (Razorpay SDK) will mix with incubation business logic.
        *   **Polyglot Advantage Lost**: If another team wanted to write the payment gateway integration using Python or Node.js because of specific libraries, they wouldn't have the freedom to do so.
        *   **Resource Contention**: Heavy payment traffic or webhooks will consume thread resources on the same server that serves regular incubation API traffic.

### Q6: [What if we remove the Docker setup?
*   **Answer**:
    *   **Effect**: The application would have to be run on bare metal or using manual server installations.
    *   **Problem**: You would lose environment consistency. Developers would have to manually install specific versions of .NET SDK 8, Java JDK 17, and MySQL on their machines, set up custom ports, configure paths, and run multiple terminal instances to launch the services. This leads to the classic "it works on my machine" problem and makes automated CI/CD deployments significantly more complex.
