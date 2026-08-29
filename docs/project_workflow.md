# Project Workflow

The following flowchart illustrates how the React Frontend, .NET Backend API, and Java Payment Service interact to fulfill requests within the Startup Incubation Management System.

```mermaid
graph TD
    Client[React Frontend] -->|REST API Calls| Backend[.NET Core API]
    
    Backend -->|Authentication & Authorization| DB1[(MySQL: Identity DB)]
    Backend -->|CRUD Operations| DB2[(MySQL: Core DB)]
    
    Backend -->|S2S Auth Request| Payment[Java Payment Service]
    Payment -->|Tx & State Data| DB3[(MySQL: Payments DB)]
    
    Payment -->|Generate Payment Link| Razorpay[Razorpay Gateway]
    Razorpay -->|Webhook Event payment_link.paid| Payment
    Payment -->|Webhook Callback| Backend
```

### Key Interactions:
1. **Frontend to Backend**: All user interactions (Admin, Mentor, Founder) are routed through the .NET Core API.
2. **Backend to DB**: The backend is split into two contexts: `IdentityDbContext` for users and `CoreDbContext` for business data.
3. **Backend to Payment Service**: When an Admin approves funding, the .NET API calls the Java Payment Service securely via a shared Service-Key.
4. **Payment Service to Razorpay**: The Java service acts as the bridge to Razorpay, generating test payment links and handling webhook callbacks.
