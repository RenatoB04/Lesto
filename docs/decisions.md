# Architecture Decision Log

| ID | Decision | Status | Rationale |
|---|---|---|---|
| ADR-001 | Use C# and ASP.NET Core on .NET 10. | Accepted | Matches team tooling and supports REST services, OpenAPI, and testing. |
| ADR-002 | Use `Lesto.slnx` as the solution file. | Accepted | It is the default solution format in .NET 10. |
| ADR-003 | Use three backend components: API Gateway, Identity Service, and Order Service. | Accepted | Two business microservices demonstrate the architecture while keeping scope manageable. |
| ADR-004 | Use one PostgreSQL container with separate `identitydb` and `orderdb` databases. | Accepted | Preserves service data ownership with a low operational cost. |
| ADR-005 | Do not implement tracking, payments, free-text geocoding, route optimisation, notifications, or messaging. | Accepted | These features are outside the minimum academic scope. |
| ADR-006 | Use a seeded catalogue of delivery points. | Accepted | Avoids external geocoding and protects delivery address privacy. |
| ADR-007 | Use OSRM for route distance and duration. | Accepted | Provides one relevant external REST integration without requiring an API key. |
| ADR-008 | Use HS256 JWT tokens with a 60-minute lifetime. | Accepted | Simpler than asymmetric key management for this academic deployment; the shared key remains outside Git. |
| ADR-009 | Validate JWTs at Gateway and again at each service. | Accepted | Prevents services from trusting only the reverse proxy. |
| ADR-010 | Use Docker Compose instead of Kubernetes. | Accepted | Docker Compose supports the required containerisation and a two-instance Order Service demonstration. |
| ADR-011 | Deploy the final stack to a small cloud virtual machine. | Accepted | Directly demonstrates cloud publication and distribution. |
| ADR-012 | Build the client as a Kotlin and Jetpack Compose Android app. | Accepted | Satisfies the separate Android interface objective while consuming the same APIs. |
| ADR-013 | Include a minimal Order Service statistics endpoint. | Accepted | Ensures managers can obtain operational information as requested by the assignment. |

## Open questions

| ID | Question | Owner | Due date |
|---|---|---|---|
| Q-001 | Confirm the submission channel and format for the scope and first delivery. | Team | Before the first delivery. |
| Q-002 | Confirm whether the lecturer considers two business microservices plus a gateway sufficient. | Team | Before the first delivery. |
| Q-003 | Confirm whether Docker Compose is sufficient for the scalability demonstration. | Team | Before the first delivery. |
