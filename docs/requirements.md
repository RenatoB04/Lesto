# Requirements and business rules

## Purpose and scope

Lesto is a secured REST platform for last-mile delivery management. It covers
registration, orders, payment, assignment and delivery status changes.

Payments use Stripe test mode only. The project does not implement live
payments, card storage, invoicing, real-time tracking, notifications or
asynchronous messaging.

## Actors and features

### Customer (`Cliente`)

- Register and sign in.
- Create an order using one catalogue point or one free-text address for each
  side of the route.
- Preview a Portuguese address before creating an order.
- View only their own orders and payment status.
- Pay a pending unpaid order through Stripe Checkout.

### Courier (`Estafeta`)

- View only assigned orders.
- Pick up a validated order.
- Mark an in-transit order as delivered or failed.
- Request an optimised route for assigned validated and in-transit orders.

### Manager (`Gestor`)

- List users and create courier users.
- View all orders and filter by status.
- Reject pending orders with a reason.
- Assign paid pending or failed orders to couriers.
- View payment status and refund a paid rejected order.

### Administrator (`Administrador`)

- Perform manager operations.
- Create users with any valid role.
- Delete users, except their own account.
- Read and update delivery and pricing settings.

## Business rules

- New orders start in `Pending` and `Unpaid`.
- Weight must not exceed `PesoMaximoKg`.
- Origin and destination must each contain exactly one of:
  `PointId` or `Address`.
- Free-text addresses are normalised, limited to 200 characters and searched
  in Portugal through Nominatim.
- An unknown address returns `422`; an unavailable geocoder returns `503`.
- The public point list contains catalogue points only.
- Price is calculated when the order is created:
  `PrecoBaseEur + distance × PrecoPorKmEur + weight × PrecoPorKgEur`.
- The price is rounded to cents and never falls below `0.50 EUR`.
- Only the Payment Service changes an order's payment status.
- Only paid `Pending` or `Failed` orders can be assigned.
- Rejections and failed deliveries require a non-empty reason.
- Only the assigned courier can pick up, deliver or fail an order.
- Invalid state transitions return `409 Conflict`.
- A refund requires a confirmed payment and a `Rejected` order.
- Route optimisation preserves pickup-before-delivery precedence and accepts
  at most 40 stops.

## Order state model

| From | To | Actor | Condition |
|---|---|---|---|
| New | `Pending` | Customer | Valid order data |
| `Pending` | `Rejected` | Manager | Reason supplied |
| `Pending` | `Validated` | Manager | Paid and valid courier |
| `Failed` | `Validated` | Manager | Paid and valid courier |
| `Validated` | `InTransit` | Assigned courier | — |
| `InTransit` | `Delivered` | Assigned courier | — |
| `InTransit` | `Failed` | Assigned courier | Reason supplied |

Payment states are independent:

```text
Unpaid -> Paid -> Refunded
```

The Payment Service repeats already-applied updates safely where possible.

## Non-functional requirements

- JWT authentication and role-based authorization.
- Separate database ownership per service.
- Docker Compose deployment.
- Two Order Service instances behind the Gateway.
- Swagger/OpenAPI available on the backend services.
- Controlled errors for unavailable OSRM, Nominatim, Stripe or internal
  services.
- Secrets supplied by environment configuration and not committed to Git.

## Out of scope

- Live Stripe keys and real-money transactions;
- invoicing;
- GPS tracking;
- email or push notifications;
- message broker and background processing;
- Kubernetes, autoscaling and CI/CD;
- distributed tracing and production monitoring.
