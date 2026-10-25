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
| `GET` | `/api/points` | Authenticated | Lists active catalogue points. |
| `POST` | `/api/orders` | Customer | Creates an order. |
| `GET` | `/api/orders` | Authenticated | Returns orders filtered by caller role; Manager and Administrator may use `status`. |
| `GET` | `/api/orders/{id}` | Owner, assigned Estafeta, Gestor, Administrador | Returns one order. |
| `POST` | `/api/orders/{id}/reject` | Manager | Rejects a pending order. |
| `POST` | `/api/orders/{id}/assign` | Manager | Assigns a courier. |
| `POST` | `/api/orders/{id}/pickup` | Assigned Estafeta | Moves an assigned order to `InTransit`. |
| `POST` | `/api/orders/{id}/deliver` | Assigned Estafeta | Completes an in-transit order. |
| `POST` | `/api/orders/{id}/fail` | Assigned Estafeta | Marks an in-transit order as failed. |
| `GET` | `/api/settings` | Administrator | Reads settings. |
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

## 5. Gateway routes

| Route prefix | Destination |
|---|---|
| `/api/auth/` | Identity Service |
| `/api/users/` | Identity Service |
| `/api/orders/` | Order Service |
| `/api/points/` | Order Service |
| `/api/settings/` | Order Service |

## 6. Environment variables

| Variable | Used by | Example or purpose |
|---|---|---|
| `ConnectionStrings__Identity` | Identity Service | Connection to `identitydb`. |
| `ConnectionStrings__Order` | Order Service | Connection to `orderdb`. |
| `Jwt__Key` | Gateway, Identity, Order | Shared development secret. |
| `Jwt__Issuer` | Gateway, Identity, Order | `lesto-identity`. |
| `Jwt__Audience` | Gateway, Identity, Order | `lesto-api`. |
| `IDENTITY_API_URL` | Order Service | Internal Identity Service URL. |
| `OSRM_BASE_URL` | Order Service | OSRM routing endpoint. |
| `ASPNETCORE_URLS` | All backend containers | Kestrel listening address. |

## 7. HTTP status codes

| Status | Meaning |
|---|---|
| `200 OK` | Successful read or update. |
| `201 Created` | Order or user created. |
| `400 Bad Request` | Invalid input, weight, or selected courier. |
| `401 Unauthorized` | Missing, invalid, or expired token. |
| `403 Forbidden` | Authenticated user lacks permission. |
| `404 Not Found` | Requested resource does not exist or is inaccessible. |
| `409 Conflict` | Invalid order state transition. |
| `503 Service Unavailable` | OSRM route calculation unavailable. |
