# Architecture decision log

| ID | Decision | Status | Reason |
|---|---|---|---|
| ADR-001 | Use C# and ASP.NET Core on .NET 10. | Accepted | Fits the team's tooling and supports REST, Swagger and testing. |
| ADR-002 | Use `Lesto.slnx` as the solution file. | Accepted | Native solution format used by the project. |
| ADR-003 | Use an API Gateway and three business services. | Accepted | Separates identity, orders and payments while keeping the academic scope manageable. |
| ADR-004 | Use one PostgreSQL container with `identitydb`, `orderdb` and `paymentdb`. | Accepted | Preserves data ownership with simple local deployment. |
| ADR-005 | Use a seeded catalogue of delivery points. | Amended | The catalogue remains, but free-text addresses are also supported. |
| ADR-006 | Use OSRM for road distance and duration. | Accepted | Provides a useful external routing integration without an API key. |
| ADR-007 | Use HS256 JWTs with a 60-minute lifetime. | Accepted | Simpler key management for this academic deployment. |
| ADR-008 | Validate JWTs at the Gateway and at each service. | Accepted | Services do not have to trust the reverse proxy blindly. |
| ADR-009 | Use Docker Compose instead of Kubernetes. | Accepted | Sufficient for local deployment and the two-instance scalability demonstration. |
| ADR-010 | Use Stripe Checkout in test mode. | Accepted | Stripe hosts the card page and the application never receives card data. Live keys are refused. |
| ADR-011 | Keep payments in a separate Payment Service and database. | Accepted | Payments have their own sessions, confirmations and refunds. |
| ADR-012 | Calculate and store the order price in Order Service. | Accepted | Distance, weight and pricing settings belong to Order Service; storing the result keeps the charged amount stable. |
| ADR-013 | Protect internal payment/order calls with `X-Internal-Key`. | Accepted | A shared secret is sufficient for the small academic deployment and keeps internal routes outside the Gateway. |
| ADR-014 | Confirm payments through webhooks and Stripe queries. | Accepted | Querying Stripe on the return page and status endpoint provides a fallback when a webhook is delayed or unavailable. |
| ADR-015 | Geocode free-text addresses with Nominatim. | Accepted | It requires no API key and supports the Portuguese address flow. Requests use a User-Agent and a local rate limit. |
| ADR-016 | Store geocoded addresses as non-catalogue points. | Accepted | Existing order-point relationships can be reused while `GET /api/points` remains limited to public catalogue points. |
| ADR-017 | Optimise courier routes locally with nearest neighbour and 2-opt. | Accepted | The algorithm is simple, testable and preserves pickup-before-delivery precedence. OSRM supplies the road-distance matrix. |
| ADR-018 | Fall back to a Haversine estimate when OSRM's matrix is unavailable. | Accepted | The courier still receives a usable route, explicitly marked `estimativa`. |

Features intentionally out of scope are live payments, GPS tracking,
notifications, asynchronous messaging, Kubernetes, automatic scaling and
CI/CD.
