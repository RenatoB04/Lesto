# Architecture Decision Log

| ID | Decision | Status | Rationale |
|---|---|---|---|
| ADR-001 | Use C# and ASP.NET Core on .NET 10. | Accepted | Matches team tooling and supports REST services, OpenAPI, and testing. |
| ADR-002 | Use `Lesto.slnx` as the solution file. | Accepted | It is the default solution format in .NET 10. |
| ADR-003 | Use three backend components: API Gateway, Identity Service, and Order Service. | Accepted | Two business microservices demonstrate the architecture while keeping scope manageable. |
| ADR-004 | Use one PostgreSQL container with separate `identitydb` and `orderdb` databases. | Accepted | Preserves service data ownership with a low operational cost. |
| ADR-005 | Do not implement tracking, payments, free-text geocoding, route optimisation, notifications, or messaging. | Superseded in part by ADR-014 | These features are outside the minimum academic scope. Payments were later added (ADR-014); the other items remain out of scope. |
| ADR-006 | Use a seeded catalogue of delivery points. | Accepted | Avoids external geocoding and protects delivery address privacy. |
| ADR-007 | Use OSRM for route distance and duration. | Accepted | Provides one relevant external REST integration without requiring an API key. |
| ADR-008 | Use HS256 JWT tokens with a 60-minute lifetime. | Accepted | Simpler than asymmetric key management for this academic deployment; the shared key remains outside Git. |
| ADR-009 | Validate JWTs at Gateway and again at each service. | Accepted | Prevents services from trusting only the reverse proxy. |
| ADR-010 | Use Docker Compose instead of Kubernetes. | Accepted | Docker Compose supports the required containerisation and a two-instance Order Service demonstration. |
| ADR-011 | Deploy the final stack to a small cloud virtual machine. | Accepted | Directly demonstrates cloud publication and distribution. |
| ADR-012 | Deliver the backend as the project interface. | Accepted | OpenAPI, Swagger UI, and Postman provide a complete and demonstrable interface without a dedicated client application. |
| ADR-013 | Include a minimal Order Service statistics endpoint. | Accepted | Ensures managers can obtain operational information as requested by the assignment. |
| ADR-014 | Process payments with Stripe Checkout in test mode. | Accepted | Adds a second relevant external service; hosted checkout means card data never reaches Lesto; test mode needs no business verification and moves no real money. The service refuses keys that are not `sk_test_`. |
| ADR-015 | Add a separate Payment Service with its own `paymentdb`. | Accepted | Payments are a distinct bounded context with their own lifecycle (sessions, confirmations, refunds); a third business service also strengthens the microservices demonstration (answers Q-002). |
| ADR-016 | Calculate the order price in Order Service at creation time, from settings editable by the Administrator. | Accepted | The price depends on order data (distance, weight) owned by Order Service; storing it on the order keeps the amount charged stable even if the rates change later. |
| ADR-017 | Protect service-to-service endpoints (`/internal/orders/...`) with a shared key in `X-Internal-Key`, and do not route them through the Gateway. | Accepted | Payment confirmation has no user token (it comes from a Stripe webhook); a shared secret outside Git, compared in constant time, is simpler than mutual TLS or client-credential tokens for this scope. |
| ADR-018 | Confirm payments by Stripe webhook with signature verification, and also by querying Stripe on the return page and status endpoint. | Accepted | Webhooks are the standard confirmation path; the query fallback makes confirmation work when webhooks cannot reach the server (self-signed certificate on the cloud VM) and makes confirmation idempotent. |
| ADR-019 | Only paid orders can be assigned; rejected paid orders can be refunded by a Manager or Administrator. | Accepted | Prevents couriers from being dispatched for unpaid deliveries and gives a clear path back to the customer when an order is rejected after payment. |

## Open questions

| ID | Question | Owner | Due date |
|---|---|---|---|
| Q-001 | Confirm the submission channel and format for the scope and first delivery. | Team | Before the first delivery. |
| Q-002 | Confirm whether the lecturer considers two business microservices plus a gateway sufficient. | Team | Before the first delivery. |
| Q-003 | Confirm whether Docker Compose is sufficient for the scalability demonstration. | Team | Before the first delivery. |
