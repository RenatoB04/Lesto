# Requirements

## 1. Purpose

Lesto is a distributed last-mile delivery management system. It supports the
delivery lifecycle from order creation to its final outcome through a secured,
documented REST API.

The project is intentionally limited to the academic scope. Payments are
processed through Stripe in test mode only (no real money). It does not provide
real-time courier tracking. Free-text addresses are
geocoded with Nominatim (OpenStreetMap). Origins and destinations are selected
from a pre-defined catalogue of delivery points.

## 2. Actors and functional requirements

### Customer

- **FR-CUS-01** — Register an account and sign in.
- **FR-CUS-02** — Create an order by choosing, for both origin and destination,
  either a catalogue point or a free-text address, and entering weight, type,
  description, and recipient details.
- **FR-CUS-07** — Preview a free-text address (matched place and coordinates)
  before creating the order.
- **FR-CUS-03** — View only orders created by the authenticated customer.
- **FR-CUS-04** — View the current status, calculated distance, duration, and
  failure or rejection reason of an order when applicable.
- **FR-CUS-05** — See the price of an order, calculated at creation time from a
  base fee, the route distance, and the weight.
- **FR-CUS-06** — Pay a pending order through a Stripe Checkout page and check
  the payment status.

### Courier

- **FR-COU-01** — View only orders assigned to the authenticated courier.
- **FR-COU-02** — Move a validated assigned order to `InTransit`.
- **FR-COU-03** — Complete an in-transit order as `Delivered` or `Failed`.
  A failed delivery requires a reason.
- **FR-COU-04** — Get an optimised order of stops (pickups and deliveries) for
  the orders assigned to the authenticated courier, with total distance and
  duration, the saving compared with handling one order at a time, and a Google
  Maps navigation link.

### Manager

- **FR-MAN-01** — Create courier accounts.
- **FR-MAN-02** — View all orders and filter them by status.
- **FR-MAN-03** — Reject a pending order, providing a reason.
- **FR-MAN-04** — Assign a pending or failed order to a valid courier.
- **FR-MAN-05** — View simple operational data: order totals by status and the
  total number of failed deliveries.
- **FR-MAN-06** — View the payment status of any order and refund the payment
  of a rejected order.

### Administrator

- **FR-ADM-01** — Create users with any role.
- **FR-ADM-02** — Remove users.
- **FR-ADM-03** — View and change the maximum allowed order weight.
- **FR-ADM-04** — View and change the pricing settings `PrecoBaseEur`,
  `PrecoPorKmEur`, and `PrecoPorKgEur`.

## 3. Business rules

- An order can only be created when its weight is less than or equal to
  `PesoMaximoKg`.
- An order is created with the `Pending` status.
- Only a manager can reject or assign an order.
- A rejection and a failed delivery always require a non-empty reason.
- An order can only be assigned to a user whose role is `Estafeta`.
- Only the assigned courier can update an order status.
- Customer access is restricted to the customer's own orders. Courier access is
  restricted to assigned orders. Managers and administrators can view all
  orders.
- Invalid state transitions return `409 Conflict`.
- An order price is `PrecoBaseEur + km × PrecoPorKmEur + kg × PrecoPorKgEur`,
  rounded to cents, with a minimum of 0.50 EUR (Stripe's minimum charge).
- An order is created with payment status `Unpaid`. Only the Payment Service can
  change it, to `Paid` or from `Paid` to `Refunded`.
- An order can only be assigned to a courier after it has been paid; otherwise
  the assignment returns `409 Conflict`.
- Only a customer can pay, and only for their own `Pending`, `Unpaid` orders.
- Only a rejected order with a confirmed payment can be refunded.
- Each side of an order (origin, destination) takes exactly one of a catalogue
  point id or a free-text address; otherwise `400 Bad Request`.
- Free-text addresses are geocoded only in Portugal, are limited to 200
  characters, and are stored as non-catalogue points. An address that is not
  found returns `422 Unprocessable Entity`.
- In an optimised route, the pickup of an order always comes before its
  delivery. `Validated` orders give a pickup and a delivery stop; `InTransit`
  orders give only a delivery stop. A route has at most 40 stops.
- The public point list (`GET /api/points`) returns only catalogue points;
  customer addresses are visible only through the orders they belong to.

## 4. Order state model

| From | To | Actor | Condition |
|---|---|---|---|
| New | `Pending` | Customer | Order weight is valid. |
| `Pending` | `Rejected` | Manager | A rejection reason is supplied. |
| `Pending` | `Validated` | Manager | Target user is an `Estafeta` and the order is `Paid`. |
| `Failed` | `Validated` | Manager | Target user is an `Estafeta` and the order is `Paid`. |
| `Validated` | `InTransit` | Assigned `Estafeta` | — |
| `InTransit` | `Delivered` | Assigned `Estafeta` | — |
| `InTransit` | `Failed` | Assigned `Estafeta` | A failure reason is supplied. |

## 5. Non-functional requirements

- **NFR-01 — Security.** The system uses JWT authentication, role-based
  authorization, HTTPS at the public gateway, and secrets that are not stored
  in source control.
- **NFR-02 — Service isolation.** Identity, Order, and Payment own separate
  databases; no service accesses another service's database.
- **NFR-03 — Availability.** If OSRM is unavailable, the order service returns
  a controlled error and does not persist a partially calculated order. If
  Nominatim is unavailable, order creation with an address returns `503` and
  nothing is persisted. If Stripe or the Order Service is unavailable, the
  Payment Service returns `503` and nothing is persisted. If the OSRM table
  service is unavailable, route optimisation still answers using a Haversine
  estimate and marks the result as `estimativa`. A lost Stripe webhook is recovered by querying
  Stripe on the return page or on `GET /api/payments/{orderId}`.
- **NFR-04 — Scalability.** The Order Service can run as two stateless
  containers behind the API Gateway.
- **NFR-05 — Maintainability.** Services are documented with OpenAPI, tested,
  containerised, and started through documented Docker Compose commands.
- **NFR-06 — Cloud deployment.** The container stack is deployed to a small
  cloud virtual machine; only the HTTPS API Gateway is exposed publicly.

## 6. Information flows

1. API consumers send all requests to the API Gateway.
2. The Gateway authenticates protected requests and forwards them to Identity
   Service or Order Service.
3. On order creation, Order Service geocodes any free-text address with
   Nominatim (reusing previously geocoded addresses), then calls OSRM with the
   coordinates to calculate distance and duration.
4. During assignment, Order Service calls Identity Service with the manager's
   bearer token to confirm that the selected user has the `Estafeta` role.
5. Order Service persists the delivery lifecycle in its own database.
6. To pay, the customer calls Payment Service, which reads the order price from
   Order Service's internal endpoint, creates a Stripe Checkout Session, and
   returns the payment URL.
7. Stripe confirms the payment through a signed webhook (or the return page
   queries Stripe), and Payment Service marks the order as paid through Order
   Service's internal endpoint.

## 7. Out of scope

- Real payments (live Stripe keys) and invoicing.
- Real-time location tracking or GPS history.
- Email notifications and asynchronous messaging.
- Kubernetes, automatic scaling, and CI/CD pipelines.
