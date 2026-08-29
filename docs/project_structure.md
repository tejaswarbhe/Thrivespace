# Project Structure

The Startup Incubation Management System follows a microservices-oriented modular monolith architecture. 

## Technology Stack
- **Frontend**: React (Vite)
- **Primary Backend**: ASP.NET Core 8
- **Payment Microservice**: Java (Spring Boot)
- **Database**: MySQL
- **Payment Gateway**: Razorpay (Test Mode)

## Directory Tree

```text
Project_CDAC/
├── Backend/                            # .NET Core 8 API
│   ├── src/
│   │   ├── StartupIMS.Domain/          # Core entities, enums (Zero dependencies)
│   │   ├── StartupIMS.Shared/          # DTOs, JWT Settings
│   │   ├── StartupIMS.Infrastructure/  # EF Core DbContexts, Email/Token services
│   │   └── StartupIMS.API/             # Controllers, Program.cs (Entry point)
│   ├── database/                       # Database scripts/guides
│   └── tests/                          # Unit & Integration tests
│
├── startupims-frontend/                # React Vite Application
│   ├── src/
│   │   ├── pages/                      # Views (Admin, Auth, etc.)
│   │   ├── components/                 # Reusable UI components
│   │   └── ...
│   └── package.json
│
└── payment-service/                    # Java Spring Boot Microservice
    ├── src/main/java/com/startupims/payment/
    │   ├── controller/                 # Webhooks and Payment API
    │   ├── service/                    # Payment logic, Razorpay integration
    │   └── ...
    └── pom.xml
```

## Database Architecture
- **startupims_identity**: Stores user credentials, roles, and refresh tokens.
- **startupims_core**: Stores domain data (Startups, Mentors, Funding, Applications).
- **startupims_payments**: Stores transaction history and pending payment links for Razorpay.
