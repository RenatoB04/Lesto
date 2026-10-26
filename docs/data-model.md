# Data Model

## 1. Boundaries

Identity Service, Order Service, and Payment Service use separate PostgreSQL databases. There are
no foreign keys between services. Order Service stores selected user details as
a snapshot where needed, so an Identity user deletion does not invalidate past
orders.

## 2. Identity Service

Identity Service uses ASP.NET Core Identity with `Guid` keys. `AppUser` extends
the standard user with `DisplayName`.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Primary key. |
| `Email` | `string` | Unique login identifier. |
| `DisplayName` | `string` | User-facing name. |
| `Role` | `string` | `Cliente`, `Estafeta`, `Gestor`, or `Administrador`. |

Role storage follows the standard ASP.NET Core Identity tables. The
implementation currently uses the Portuguese role names `Cliente`, `Estafeta`,
`Gestor` and `Administrador`.

## 3. Order Service

### DeliveryPoint

The catalogue contains a small, seeded list of selectable origins and
destinations. Coordinates are stored locally and are the only data sent to
OSRM.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Primary key. |
| `Name` | `string` | Display name returned by the API. |
| `AddressLabel` | `string` | Read-only descriptive label. |
| `Latitude` | `decimal` | WGS84 latitude. |
| `Longitude` | `decimal` | WGS84 longitude. |
| `IsActive` | `bool` | Controls whether the point can be selected. |

### DeliveryOrder

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Primary key. |
| `CustomerId` | `Guid` | Identity user identifier; no cross-service foreign key. |
| `CustomerEmail` | `string` | Snapshot for display and audit. |
| `OriginPointId` | `Guid` | Local foreign key to `DeliveryPoint`. |
| `DestinationPointId` | `Guid` | Local foreign key to `DeliveryPoint`. |
| `WeightKg` | `decimal(8,2)` | Validated against the setting. |
| `Type` | `string` | `Document`, `Package`, `Fragile`, or `Bulky`. |
| `Description` | `string?` | Optional order description. |
| `RecipientName` | `string` | Delivery recipient. |
| `RecipientPhone` | `string` | Delivery recipient contact. |
| `DistanceMeters` | `int` | Result returned by OSRM. |
| `DurationSeconds` | `int` | Result returned by OSRM. |
| `Status` | `string` | Order state. |
| `CourierId` | `Guid?` | Assigned Identity user identifier. |
| `CourierName` | `string?` | Snapshot at assignment time. |
| `Reason` | `string?` | Rejection or delivery failure reason. |
| `CreatedAt` | `DateTimeOffset` | UTC timestamp. |
| `AssignedAt` | `DateTimeOffset?` | UTC timestamp. |
| `DeliveredAt` | `DateTimeOffset?` | UTC timestamp. |
| `UpdatedAt` | `DateTimeOffset` | UTC timestamp. |

Payment fields added to the order:

| Field | Type | Notes |
|---|---|---|
| `Price` | `numeric(10,2)` | Price in EUR, calculated at creation time. |
| `PaymentStatus` | `int` (enum) | `Unpaid`, `Paid`, or `Refunded`; changed only by Payment Service. |
| `PaidAt` | `DateTime?` | UTC timestamp of the payment confirmation. |

### SystemSetting

| Field | Type | Notes |
|---|---|---|
| `Key` | `string` | Primary key; seeded with `PesoMaximoKg`, `PrecoBaseEur`, `PrecoPorKmEur`, and `PrecoPorKgEur`. |
| `Value` | `string` | Stored configuration value. |

## 4. Payment Service

### Payment

One row per payment attempt (Stripe Checkout Session). An order can have
several attempts, but at most one reaches `Paid`.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Primary key; also sent to Stripe as metadata. |
| `OrderId` | `Guid` | Order identifier; no cross-service foreign key. Indexed. |
| `CustomerId` | `Guid` | Identity user who started the payment. |
| `AmountCents` | `long` | Amount in cents, as Stripe expects. |
| `Currency` | `string(3)` | `eur`. |
| `StripeSessionId` | `string` | Checkout Session id; unique index. |
| `StripePaymentIntentId` | `string?` | Needed for refunds. |
| `Status` | `string` | `Pending`, `Paid`, `Expired`, or `Refunded`. |
| `CreatedAt` | `DateTime` | UTC timestamp. |
| `PaidAt` | `DateTime?` | UTC timestamp. |
| `RefundedAt` | `DateTime?` | UTC timestamp. |

## 5. Entity relationship diagram

```mermaid
erDiagram
    DELIVERY_POINT ||--o{ DELIVERY_ORDER : "origin"
    DELIVERY_POINT ||--o{ DELIVERY_ORDER : "destination"
    DELIVERY_POINT {
        uuid id PK
        string name
        string address_label
        decimal latitude
        decimal longitude
        boolean is_active
    }
    DELIVERY_ORDER {
        uuid id PK
        uuid customer_id
        uuid origin_point_id FK
        uuid destination_point_id FK
        decimal weight_kg
        string status
        uuid courier_id
        int distance_meters
        int duration_seconds
        datetime created_at
    }
    SYSTEM_SETTING {
        string key PK
        string value
    }
```

## 6. Data conventions

- All identifiers use `Guid`.
- All timestamps use `DateTimeOffset` in UTC.
- API JSON uses camelCase.
- API enum-like values use the English strings documented in this repository.
