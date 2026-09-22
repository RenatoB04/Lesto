# Requirements

## 1. Purpose

Lesto is a distributed last-mile delivery management system. It supports the
delivery lifecycle from order creation to its final outcome and is consumed by
the native Android application of the same name.

The project is intentionally limited to the academic scope. It does not process
payments, provide real-time courier tracking, accept free-text address
geocoding, or optimise delivery routes. Origins and destinations are selected
from a pre-defined catalogue of delivery points.

## 2. Actors and functional requirements

### Customer

- **FR-CUS-01** — Register an account and sign in.
- **FR-CUS-02** — Create an order by selecting an origin and destination point,
  entering weight, type, description, and recipient details.
- **FR-CUS-03** — View only orders created by the authenticated customer.
- **FR-CUS-04** — View the current status, calculated distance, duration, and
  failure or rejection reason of an order when applicable.

### Courier

- **FR-COU-01** — View only orders assigned to the authenticated courier.
- **FR-COU-02** — Change an assigned order from `Assigned` to `InTransit`.
- **FR-COU-03** — Complete an in-transit order as `Delivered` or `Failed`.
  A failed delivery requires a reason.

### Manager

- **FR-MAN-01** — Create courier accounts.
- **FR-MAN-02** — View all orders and filter them by status.
- **FR-MAN-03** — Reject a pending order, providing a reason.
- **FR-MAN-04** — Assign a pending or failed order to a valid courier.
- **FR-MAN-05** — View simple operational data: order totals by status and the
  total number of failed deliveries.

### Administrator

- **FR-ADM-01** — Create users with any role.
- **FR-ADM-02** — Remove users.
- **FR-ADM-03** — View and change the maximum allowed order weight.

## 3. Business rules

- An order can only be created when its weight is less than or equal to
  `MaxOrderWeightKg`.
- An order is created with the `Pending` status.
- Only a manager can reject or assign an order.
- A rejection and a failed delivery always require a non-empty reason.
- An order can only be assigned to a user whose role is `Courier`.
- Only the assigned courier can update an order status.
- Customer access is restricted to the customer's own orders. Courier access is
  restricted to assigned orders. Managers and administrators can view all
  orders.
- Invalid state transitions return `409 Conflict`.

## 4. Order state model

| From | To | Actor | Condition |
|---|---|---|---|
| New | `Pending` | Customer | Order weight is valid. |
| `Pending` | `Rejected` | Manager | A rejection reason is supplied. |
| `Pending` | `Assigned` | Manager | Target user is a courier. |
| `Failed` | `Assigned` | Manager | Target user is a courier. |
| `Assigned` | `InTransit` | Assigned courier | — |
| `InTransit` | `Delivered` | Assigned courier | — |
| `InTransit` | `Failed` | Assigned courier | A failure reason is supplied. |

## 5. Non-functional requirements

- **NFR-01 — Security.** The system uses JWT authentication, role-based
  authorization, HTTPS at the public gateway, and secrets that are not stored
  in source control.
- **NFR-02 — Service isolation.** Identity and Order own separate databases;
  neither service accesses the other's database.
- **NFR-03 — Availability.** If OSRM is unavailable, the order service returns
  a controlled error and does not persist a partially calculated order.
- **NFR-04 — Scalability.** The Order Service can run as two stateless
  containers behind the API Gateway.
- **NFR-05 — Maintainability.** Services are documented with OpenAPI, tested,
  containerised, and started through documented Docker Compose commands.
- **NFR-06 — Cloud deployment.** The container stack is deployed to a small
  cloud virtual machine; only the HTTPS API Gateway is exposed publicly.

## 6. Information flows

1. The Android app sends all requests to the API Gateway.
2. The Gateway authenticates protected requests and forwards them to Identity
   Service or Order Service.
3. On order creation, Order Service calls OSRM with the coordinates of the
   selected catalogue points to calculate distance and duration.
4. During assignment, Order Service calls Identity Service with the manager's
   bearer token to confirm that the selected user has the `Courier` role.
5. Order Service persists the delivery lifecycle in its own database.

## 7. Out of scope

- Real payments and invoicing.
- Real-time location tracking or GPS history.
- Free-text address geocoding.
- Route optimisation.
- Email notifications and asynchronous messaging.
- Kubernetes, automatic scaling, and CI/CD pipelines.
