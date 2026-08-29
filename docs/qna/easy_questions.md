# Easy Interview & Project Viva Questions

This file contains fundamental questions about the project's purpose, components, roles, and simple "what does this do?" or "what if we remove this?" scenarios.

---

### Q1: What is the overall purpose of this project?
*   **Answer**: The project is a **Startup Incubation Management System (StartupIMS)**. It helps incubators manage the lifecycle of startups from initial application submission to incubation, mentor assignment, progress reporting, and funding request approvals and disbursements.

### Q2: What are the three roles in the system, and what can they do?
*   **Answer**:
    *   **Founder**: Represents the startup. Can create and manage their startup profile, apply for incubation, submit periodic progress reports, and request funding.
    *   **Mentor**: Assigned to guide specific startups. Can view assigned startups, review progress reports, and give feedback.
    *   **Admin**: The administrator. Can manage users, approve/reject incubation applications, assign mentors to startups, and approve funding requests.

### Q3: What is the tech stack used in this project?
*   **Answer**:
    *   **Frontend**: React (built with Vite) for a modern, fast single-page user interface.
    *   **Backend (Core)**: ASP.NET Core 8 Web API implementing a modular monolith structure.
    *   **Payment Service**: Java Spring Boot microservice.
    *   **Database**: MySQL.
    *   **Payment Gateway**: Razorpay (simulated in Test Mode).

### Q4: What database is used, and how is the database structured?
*   **Answer**: MySQL is the database. It is split logically into three separate schemas (databases):
    1.  `startupims_identity`: Handles user authentication credentials, roles, and session refresh tokens.
    2.  `startupims_core`: Handles core domain models like Startups, Mentors, Applications, and Funding.
    3.  `startupims_payments`: Handles transaction logs and webhook statuses for the payment microservice.

---

### Q5: [What does this do?] What does the `AuthController` in the .NET backend do?
*   **Answer**: It handles user management actions, specifically:
    *   **User Registration**: Creating new user accounts with encrypted passwords.
    *   **Login**: Validating credentials and issuing secure JWT Access Tokens and Refresh Tokens.
    *   **Refresh Token**: Extending the user's session without requiring them to re-type their password.

### Q6: [What does this do?] What is Razorpay, and why is it used here?
*   **Answer**: Razorpay is a payment gateway provider. It is integrated into the Java Payment Service in **Test Mode** to generate unique payment links for startups when their funding requests are approved by an Admin. The startup founder can open the generated link to execute a mock payment using test credentials.

---

### Q7: [What if we remove this?] What if we remove the `startupims-frontend` folder?
*   **Answer**: 
    *   **Effect**: The users (Admins, Mentors, Founders) will lose the visual web interface. They will no longer have a dashboard to log in, submit progress reports, or approve funding via buttons.
    *   **Survivability**: The APIs (.NET Backend and Java Payment Service) will still function independently. Developers can still interact with the backend using API testing tools like Swagger or Postman, but the application will not be usable by regular end-users.

### Q8: [What if we remove this?] What if we remove the SMTP Email Service configuration or server?
*   **Answer**:
    *   **Effect**: Automated email notifications (such as "Application Approved", "Mentor Assigned", or "Funding Disbursed") will fail to send.
    *   **Survivability**: The system will continue to work, but the application might throw errors during actions that trigger emails (unless errors are caught and logged gracefully). Founders won't get updates unless they check their dashboards.

### Q9: [What if we remove this?] What if we delete the `appsettings.json` file in the .NET backend?
*   **Answer**:
    *   **Effect**: The .NET Core backend will fail to start.
    *   **Why**: It contains critical configuration settings, such as database connection strings, JWT configuration keys, and SMTP server configurations, which the application requires to boot up and initialize services. I can, however, use environment variables to override these if they are set in the host environment.
