# Technical Contracts

## 1. General conventions

- All API endpoints are exposed through the API Gateway.
- Requests and responses use JSON with camelCase property names.
- Date and time values use ISO 8601 in UTC.
- Protected endpoints require `Authorization: Bearer <access-token>`.
- Errors use RFC 7807 `ProblemDetails`.
- List endpoints return paginated data where applicable, using `page` and
  `pageSize` query parameters.

## 2. JWT contract

| Property | Value |
|---|---|
| Algorithm | HS256 |
| Lifetime | 60 minutes |
| Issuer | `lesto-identity` |
| Audience | `lesto-api` |
| Claims | `sub`, `email`, `name`, `role` |
| Roles | `Cliente`, `Estafeta`, `Gestor`, `Administrador` |

The JWT secret is configured through `Jwt__Key`. All three backend components
validate the same token contract. The secret must only appear in local `.env`
files or cloud configuration.

## 3. Identity Service endpoints

| Method | Endpoint | Roles | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Anonymous | Creates a Customer account. |
| `POST` | `/api/auth/login` | Anonymous | Returns token, expiry, and user information. |
| `GET` | `/api/users` | Manager, Administrator | Lists users; accepts `role`. |
| `GET` | `/api/users/{id}` | Manager, Administrator | Gets one user. |
| `POST` | `/api/users` | Administrador; Gestor for Estafeta only | Creates a user. |
| `DELETE` | `/api/users/{id}` | Administrator | Removes a user. |
| `GET` | `/health/live` | Anonymous | Liveness check. |

### Login response

```json
{
  "accessToken": "eyJ...",
  "expiresAt": "2026-10-01T12:00:00Z",
  "user": {
    "id": "00000000-0000-0000-0000-000000000000",
    "email": "customer@lesto.test",
    "displayName": "Demo Customer",
    "role": "Customer"
  }
}
```

## 4. Order Service endpoints

| Method | Endpoint | Roles | Description |
|---|---|---|---|
| `GET` | `/api/points` | Authenticated | Lists catalogue points only (customer addresses are never listed). |
| `GET` | `/api/geocode?q=...` | Cliente | Previews an address: returns `query`, `displayName`, `latitude`, `longitude`; `404` if not found. |
| `POST` | `/api/orders` | Customer | Creates an order. |
| `GET` | `/api/orders` | Authenticated | Returns orders filtered by caller role; Manager and Administrator may use `status`. |
| `GET` | `/api/orders/{id}` | Owner, assigned Estafeta, Gestor, Administrador | Returns one order. |
| `POST` | `/api/orders/{id}/reject` | Manager | Rejects a pending order. |
| `POST` | `/api/orders/{id}/assign` | Manager | Assigns a courier. |
| `POST` | `/api/orders/{id}/pickup` | Assigned Estafeta | Moves an assigned order to `InTransit`. |
| `POST` | `/api/orders/{id}/deliver` | Assigned Estafeta | Completes an in-transit order. |
| `POST` | `/api/orders/{id}/fail` | Assigned Estafeta | Marks an in-transit order as failed. |
| `GET` | `/api/settings` | Administrator | Reads settings, including the pricing rates. |
| `PUT` | `/api/settings` | Administrator | Changes settings. |
| `GET` | `/health/live` | Anonymous | Liveness check. |

### Create order request

```json
{
  "originPointId": "00000000-0000-0000-0000-000000000001",
  "destinationPointId": "00000000-0000-0000-0000-000000000002",
  "weightKg": 2.5,
  "type": "Package",
  "description": "Small box",
  "recipientName": "Example Recipient",
  "recipientPhone": "+351900000000"
}
```

Each side accepts either a point id or an address (exactly one):

```json
{
  "originAddress": "Avenida da Liberdade, Braga",
  "destinationPointId": "00000000-0000-0000-0000-000000000002",
  "weight": 3,
  "type": "Normal",
  "recipientData": "Example Recipient"
}
```

### Update status request

```json
{
  "status": "Failed",
  "reason": "Recipient was unavailable"
}
```

### Statistics response

```json
{
  "byStatus": {
    "Pending": 2,
    "Validated": 1,
    "InTransit": 1,
    "Delivered": 8,
    "Failed": 1,
    "Rejected": 1
  },
  "failedCount": 1
}
```

Order responses now include `price` (EUR) and `paymentStatus`
(`Unpaid`, `Paid`, `Refunded`). `POST /api/orders` returns `orderId` and
`price`. `POST /api/orders/{id}/assign` returns `409` when the order is unpaid.

### Internal endpoints (not routed by the Gateway)

| Method | Endpoint | Caller | Description |
|---|---|---|---|
| `GET` | `/internal/orders/{id}` | Payment Service | Returns `id`, `customerId`, `price`, `status`, `paymentStatus`. |
| `POST` | `/internal/orders/{id}/payment` | Payment Service | Body `{ "paymentStatus": "Paid" \| "Refunded" }`; idempotent. |

Both require the header `X-Internal-Key` equal to `INTERNAL_API_KEY`;
otherwise they return `401` (`503` if the key is not configured).

## 5. Payment Service endpoints

| Method | Endpoint | Roles | Description |
|---|---|---|---|
| `POST` | `/api/payments/checkout` | Cliente | Body `{ "orderId": "..." }`; creates a Stripe Checkout Session and returns `checkoutUrl`. |
| `GET` | `/api/payments/{orderId}` | Owner Cliente, Gestor, Administrador | Returns the payment; if still pending, queries Stripe first. |
| `POST` | `/api/payments/{orderId}/refund` | Gestor, Administrador | Refunds a paid, rejected order. |
| `POST` | `/api/payments/webhook` | Anonymous (Stripe signature) | Receives `checkout.session.completed`, `async_payment_succeeded`, and `expired`. |
| `GET` | `/api/payments/return/success` | Anonymous | Stripe return page; confirms the session with Stripe. |
| `GET` | `/api/payments/return/cancel` | Anonymous | Stripe cancel page. |
| `GET` | `/health/live` | Anonymous | Liveness check. |

### Checkout response

```json
{
  "paymentId": "00000000-0000-0000-0000-000000000000",
  "orderId": "00000000-0000-0000-0000-000000000001",
  "amount": 7.50,
  "currency": "eur",
  "checkoutUrl": "https://checkout.stripe.com/c/pay/cs_test_..."
}
```

## 6. Gateway routes

| Route prefix | Destination |
|---|---|
| `/api/auth/` | Identity Service |
| `/api/users/` | Identity Service |
| `/api/orders/` | Order Service |
| `/api/points/` | Order Service |
| `/api/settings/` | Order Service |
| `/api/geocode/` | Order Service |
| `/api/payments/webhook` | Payment Service (anonymous; Stripe signature checked by the service) |
| `/api/payments/return/` | Payment Service (anonymous) |
| `/api/payments/` | Payment Service (authenticated) |

## 7. Environment variables

| Variable | Used by | Example or purpose |
|---|---|---|
| `ConnectionStrings__Identity` | Identity Service | Connection to `identitydb`. |
| `ConnectionStrings__Order` | Order Service | Connection to `orderdb`. |
| `Jwt__Key` | Gateway, Identity, Order | Shared development secret. |
| `Jwt__Issuer` | Gateway, Identity, Order | `lesto-identity`. |
| `Jwt__Audience` | Gateway, Identity, Order | `lesto-api`. |
| `IDENTITY_API_URL` | Order Service | Internal Identity Service URL. |
| `OSRM_BASE_URL` | Order Service | OSRM routing endpoint. |
| `NOMINATIM_BASE_URL` | Order Service | Nominatim endpoint (default `https://nominatim.openstreetmap.org`). |
| `STRIPE_SECRET_KEY` | Payment Service | Stripe test secret key (`sk_test_...`); live keys are refused. |
| `STRIPE_WEBHOOK_SECRET` | Payment Service | Webhook signing secret (`whsec_...`), e.g. from `stripe listen --print-secret`. |
| `INTERNAL_API_KEY` | Order, Payment | Shared key for `/internal/...` endpoints. |
| `ORDER_INTERNAL_URL` | Payment Service | Internal Order Service URL. |
| `PUBLIC_BASE_URL` | Payment Service | Public Gateway URL used in Stripe return links. |
| `ASPNETCORE_URLS` | All backend containers | Kestrel listening address. |

## 8. HTTP status codes

| Status | Meaning |
|---|---|
| `200 OK` | Successful read or update. |
| `201 Created` | Order or user created. |
| `400 Bad Request` | Invalid input, weight, or selected courier. |
| `401 Unauthorized` | Missing, invalid, or expired token. |
| `403 Forbidden` | Authenticated user lacks permission. |
| `404 Not Found` | Requested resource does not exist or is inaccessible. |
| `422 Unprocessable Entity` | Free-text address not found by the geocoder. |
| `409 Conflict` | Invalid order or payment state (e.g. assigning an unpaid order). |
| `503 Service Unavailable` | OSRM, Nominatim, Stripe, or Order Service unavailable. |
