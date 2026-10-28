# Data model

## Database boundaries

The services own separate PostgreSQL databases:

- Identity Service: `identitydb`;
- Order Service: `orderdb`;
- Payment Service: `paymentdb`.

There are no foreign keys between services. Cross-service identifiers are
stored as `Guid` values and are validated through HTTP calls when necessary.

## Identity Service

ASP.NET Core Identity stores users, roles and authentication data.

The application user adds:

| Field | Type | Description |
|---|---|---|
| `Id` | `Guid` | User identifier |
| `Email` | `string` | Login email |
| `DisplayName` | `string` | Display name |

Roles used by the application are `Cliente`, `Estafeta`, `Gestor` and
`Administrador`.

## Order Service

### Point

| Field | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `Name` | `string` | Catalogue or geocoder display name |
| `Latitude` | `double` | WGS84 latitude |
| `Longitude` | `double` | WGS84 longitude |
| `IsCatalog` | `bool` | Whether the point belongs to the public catalogue |
| `Address` | `string?` | Normalised free-text address, when applicable |

Catalogue points are seeded. Points created from customer addresses have
`IsCatalog = false` and are not returned by `GET /api/points`.

### Order

| Field | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `CustomerId` | `Guid` | Identity user identifier |
| `CourierId` | `Guid?` | Assigned courier, if any |
| `OriginPointId` | `Guid` | Origin point foreign key |
| `DestinationPointId` | `Guid` | Destination point foreign key |
| `Weight` | `double` | Order weight |
| `Type` | `string` | Order type |
| `RecipientData` | `string` | Recipient information |
| `Status` | `OrderStatus` | Current delivery status |
| `Reason` | `string?` | Rejection or failure reason |
| `Distance` | `double` | Calculated route distance |
| `Duration` | `double` | Calculated route duration |
| `Price` | `decimal(10,2)` | Price in EUR, fixed at creation |
| `PaymentStatus` | `PaymentStatus` | `Unpaid`, `Paid` or `Refunded` |
| `PaidAt` | `DateTime?` | Payment confirmation time |

Order statuses are `Pending`, `Validated`, `Rejected`, `InTransit`,
`Delivered` and `Failed`.

### Setting

| Field | Type | Description |
|---|---|---|
| `Key` | `string` | Primary key |
| `Value` | `string` | Configured value |

Seeded settings are `PesoMaximoKg`, `PrecoBaseEur`, `PrecoPorKmEur` and
`PrecoPorKgEur`.

## Payment Service

### Payment

Each row represents a Stripe Checkout payment attempt.

| Field | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key and Stripe metadata |
| `OrderId` | `Guid` | Order identifier |
| `CustomerId` | `Guid` | Customer identifier |
| `AmountCents` | `long` | Amount sent to Stripe |
| `Currency` | `string` | Currently `eur` |
| `StripeSessionId` | `string` | Stripe Checkout Session identifier |
| `StripePaymentIntentId` | `string?` | Payment Intent used for refunds |
| `Status` | `PaymentRecordStatus` | `Pending`, `Paid`, `Expired` or `Refunded` |
| `CreatedAt` | `DateTime` | Creation time in UTC |
| `PaidAt` | `DateTime?` | Confirmation time in UTC |
| `RefundedAt` | `DateTime?` | Refund time in UTC |

An order may have more than one payment attempt. The application uses the
latest relevant payment when returning payment status.

## Data conventions

- Identifiers use `Guid`.
- API JSON uses camelCase.
- Prices are represented in euros in the Order API and in cents internally
  for Stripe requests.
- The current implementation uses UTC `DateTime` for payment timestamps and
  `PaidAt`; this is an implementation detail to keep in mind when exporting
  data.
