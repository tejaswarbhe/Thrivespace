# All Module Flow Chart

This flowchart details the overarching lifecycle of a Startup within the Incubation Management System, traversing through multiple core modules from Application to Funding.

```mermaid
graph TD
    %% Startups & Applications Module
    subgraph Startups & Applications
        Start[Founder Registers Account] --> CreateProfile[Create Startup Profile]
        CreateProfile --> SubmitApp[Submit Incubation Application]
        SubmitApp --> AdminReview{Admin Reviews}
        AdminReview -->|Rejected| End((End))
        AdminReview -->|Approved| Incubated[Startup Incubated]
    end

    %% Mentors & Assignments Module
    subgraph Mentorship Module
        Incubated --> AssignMentor[Admin Assigns Mentor]
        AssignMentor --> MentorSession[Mentoring Sessions & Guidance]
    end

    %% Progress Reports Module
    subgraph Progress Tracking Module
        MentorSession --> SubmitProgress[Founder Submits Progress Report]
        SubmitProgress --> ReviewProgress[Mentor Reviews Report]
        ReviewProgress --> AdminMonitor[Admin Monitors Progress]
    end

    %% Funding Module
    subgraph Funding & Payments Module
        AdminMonitor --> RequestFunds[Founder Requests Funding]
        RequestFunds --> EvaluateFunds{Admin Evaluates}
        EvaluateFunds -->|Rejected| DeclineFunds[Funding Declined]
        DeclineFunds --> AdminMonitor
        EvaluateFunds -->|Approved| InitPayment[Admin Initiates Disbursement]
        
        InitPayment --> PaymentService[Java Payment Service]
        PaymentService --> GenLink[Generate Razorpay Link]
        GenLink --> FounderPays[Founder Pays via Link]
        FounderPays --> Webhook[Razorpay Webhook: payment_link.paid]
        Webhook --> PaymentService
        PaymentService --> ConfirmFunding[.NET API: Mark Disbursed]
        ConfirmFunding --> FundingComplete((Funding Complete))
    end
```
