# Technical contracts

## General conventions

- The normal base URL is the API Gateway: `http://localhost:5000`.
- Requests and responses use JSON with camelCase properties.
- Protected routes require `Authorization: Bearer <JWT>`.
- Anonymous routes are explicitly identified below.
- Internal routes are not exposed by the Gateway.
- The current controllers return JSON objects with a `Message` field for many
  errors; not every error is an RFC 7807 `ProblemDetails` response.
- Swagger is generated at runtime by each backend service.

## JWT

| Property | Value |
|---|---|
| Algorithm | HS256 |
| Lifetime | 60 minutes |
| Issuer | `LestoIdentityService` |
| Audience | `LestoApiGateway` |
| Roles | `Cliente`, `Estafeta`, `Gestor`, `Administrador` |

The signing secret is provided by `JWT_SECRET`.

## Identity Service

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Anonymous | Creates a `Cliente` account |
| POST | `/api/auth/login` | Anonymous | Returns JWT and user information |
| GET | `/api/users` | Gestor, Administrador | Lists users; optional `role` filter |
| GET | `/api/users/{id}` | Gestor, Administrador | Gets one user |
| POST | `/api/users` | Gestor, Administrador | Creates a user; a Gestor can only create `Estafeta` |
| DELETE | `/api/users/{id}` | Administrador | Deletes a user, except the caller |
| GET | `/health/live` | Anonymous | Liveness check |

## Order Service

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/points` | Authenticated | Lists catalogue points |
| GET | `/api/geocode?q=...` | Cliente | Previews a Portuguese address |
| POST | `/api/orders` | Cliente | Creates an order |
| GET | `/api/orders?estado=...` | Authenticated | Lists orders according to the caller role; managers and couriers can filter by state |
| GET | `/api/orders/{id}` | Owner, assigned Estafeta, Gestor, Administrador | Gets an order |
| POST | `/api/orders/{id}/reject` | Gestor, Administrador | Rejects a pending order |
| POST | `/api/orders/{id}/assign` | Gestor, Administrador | Assigns a paid order |
| POST | `/api/orders/{id}/pickup` | Assigned Estafeta | Moves to `InTransit` |
| POST | `/api/orders/{id}/deliver` | Assigned Estafeta | Moves to `Delivered` |
| POST | `/api/orders/{id}/fail` | Assigned Estafeta | Moves to `Failed` with a reason |
| GET | `/api/routes/optimized` | Estafeta | Returns an optimised route |
| GET | `/api/testeosrm` | Administrador | Tests connectivity to OSRM with fixed coordinates |
| GET | `/api/settings` | Administrador | Lists settings |
| PUT | `/api/settings/{key}` | Administrador | Updates one setting |
| GET | `/health/live` | Anonymous | Liveness check |

### Create order

Each side must use exactly one point id or address:

```json
{
  "originPointId": "00000000-0000-0000-0000-000000000001",
  "destinationAddress": "Rua Exemplo 1, Braga",
  "weight": 2,
  "type": "Normal",
  "recipientData": "Maria Costa"
}
```

The response contains the order id, current status, points, distance,
duration, calculated price and payment status.

### Optimised route

`GET /api/routes/optimized` accepts optional `lat` and `lon`. They must be
provided together and represent the courier's current position.

The response includes:

- `source`: `osrm` or `estimativa`;
- ordered stops with `Recolha` or `Entrega`;
- total distance and duration;
- baseline distance;
- percentage saving;
- optional `googleMapsUrl`.

The route uses OSRM when available and a Haversine estimate as fallback.

## Internal Order Service endpoints

These endpoints are not routed by the Gateway and are called by Payment
Service with `X-Internal-Key`.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/internal/orders/{id}` | Returns customer, price, order state and payment state |
| POST | `/internal/orders/{id}/payment` | Applies `Paid` or `Refunded` |

The header must equal `INTERNAL_API_KEY`. Missing configuration returns `503`;
an invalid header returns `401`.

## Payment Service

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/payments/checkout` | Cliente | Creates a Stripe Checkout Session |
| GET | `/api/payments/{orderId}` | Owner Cliente, Gestor, Administrador | Gets the latest payment |
| POST | `/api/payments/{orderId}/refund` | Gestor, Administrador | Refunds a paid rejected order |
| POST | `/api/payments/webhook` | Anonymous with Stripe signature | Processes Stripe session events |
| GET | `/api/payments/return/success` | Anonymous | Confirms the session and shows a result page |
| GET | `/api/payments/return/cancel` | Anonymous | Shows the cancellation page |
| GET | `/health/live` | Anonymous | Liveness check |

The checkout response contains `paymentId`, `orderId`, `amount`, `currency` and
`checkoutUrl`. Stripe keys must be test keys beginning with `sk_test_`.

## Gateway routes

| Prefix | Destination | Gateway policy |
|---|---|---|
| `/api/auth/` | Identity Service | Anonymous where supported |
| `/api/users/` | Identity Service | `RequireAuth` |
| `/api/orders/` | Order Service | `RequireAuth` |
| `/api/points/` | Order Service | Controller authentication |
| `/api/settings/` | Order Service | `RequireAuth` |
| `/api/geocode/` | Order Service | `RequireAuth` |
| `/api/routes/` | Order Service | `RequireAuth` |
| `/api/payments/webhook` | Payment Service | Anonymous; Stripe signature checked by service |
| `/api/payments/return/` | Payment Service | Anonymous |
| `/api/payments/` | Payment Service | `RequireAuth` |

## Environment variables

| Variable | Used by | Purpose |
|---|---|---|
| `POSTGRES_USER` | All database services | PostgreSQL user |
| `POSTGRES_PASSWORD` | All database services | PostgreSQL password |
| `JWT_SECRET` | Gateway and backend services | Shared JWT signing secret |
| `IDENTITY_API_URL` | Order Service | Identity Service URL |
| `OSRM_BASE_URL` | Order Service | OSRM base URL |
| `NOMINATIM_BASE_URL` | Order Service | Nominatim base URL |
| `STRIPE_SECRET_KEY` | Payment Service | Stripe test secret key |
| `STRIPE_WEBHOOK_SECRET` | Payment Service | Stripe webhook signing secret |
| `INTERNAL_API_KEY` | Order and Payment Services | Internal API authentication |
| `ORDER_INTERNAL_URL` | Payment Service | Internal Order Service URL |
| `PUBLIC_BASE_URL` | Payment Service | Public URL used in Stripe return links |
| `ADMIN_*`, `GESTOR_*`, `ESTAFETA_*`, `CLIENTE_*` | Identity Service | Optional seed accounts |

## Common status codes

| Status | Meaning |
|---|---|
| 200 | Successful read or update |
| 201 | Resource or checkout created |
| 400 | Invalid input |
| 401 | Missing or invalid authentication |
| 403 | Authenticated user lacks permission |
| 404 | Resource not found |
| 409 | Invalid business-state transition |
| 422 | Address could not be geocoded |
| 503 | Required external or internal service unavailable |
