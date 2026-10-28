# Sequence diagrams

## Login

```mermaid
sequenceDiagram
    participant Client as API client
    participant Gateway as API Gateway
    participant Identity as Identity Service
    participant Db as identitydb
    Client->>Gateway: POST /api/auth/login
    Gateway->>Identity: Forward request
    Identity->>Db: Find user and verify password
    Db-->>Identity: User and role
    Identity-->>Gateway: JWT and user data
    Gateway-->>Client: 200 OK
```

## Create an order

```mermaid
sequenceDiagram
    participant Client as Customer
    participant Gateway as API Gateway
    participant Order as Order Service
    participant Db as orderdb
    participant Geo as Nominatim
    participant OSRM as OSRM
    Client->>Gateway: POST /api/orders
    Gateway->>Order: Forward JWT-authenticated request
    Order->>Db: Read points and pricing settings
    opt Free-text address not cached
        Order->>Geo: Search address restricted to Portugal
        Geo-->>Order: Coordinates and display name
    end
    Order->>OSRM: Calculate route distance and duration
    OSRM-->>Order: Route data
    Order->>Db: Save points and Pending/Unpaid order
    Order-->>Client: 201 Created
```

## Assign and deliver an order

```mermaid
sequenceDiagram
    participant Client as Manager or courier
    participant Gateway as API Gateway
    participant Order as Order Service
    participant Identity as Identity Service
    participant Db as orderdb
    Client->>Gateway: POST /api/orders/{id}/assign
    Gateway->>Order: Forward manager JWT
    Order->>Order: Validate state and Paid status
    Order->>Identity: Validate courier role
    Identity-->>Order: Courier is valid
    Order->>Db: Save courier and Validated status
    Client->>Gateway: POST /api/orders/{id}/pickup
    Gateway->>Order: Forward courier JWT
    Order->>Db: Save InTransit status
    Client->>Gateway: POST /api/orders/{id}/deliver or /fail
    Gateway->>Order: Forward courier JWT
    Order->>Db: Save final status
```

## Pay an order

```mermaid
sequenceDiagram
    participant Client as Customer
    participant Gateway as API Gateway
    participant Payment as Payment Service
    participant Order as Order Service
    participant Stripe as Stripe test mode
    participant Db as paymentdb
    Client->>Gateway: POST /api/payments/checkout
    Gateway->>Payment: Forward customer JWT
    Payment->>Order: GET /internal/orders/{id} with X-Internal-Key
    Order-->>Payment: Price, owner, state and payment status
    Payment->>Stripe: Create Checkout Session
    Stripe-->>Payment: Session id and URL
    Payment->>Db: Save Pending payment
    Payment-->>Client: 201 checkout URL
    Client->>Stripe: Complete hosted checkout
    Stripe->>Gateway: Signed POST /api/payments/webhook
    Gateway->>Payment: Forward webhook
    Payment->>Db: Mark payment Paid
    Payment->>Order: POST /internal/orders/{id}/payment
    Order-->>Payment: Payment state updated
```

The return page and `GET /api/payments/{orderId}` can query Stripe again when
the webhook has not arrived.

## Optimise a courier route

```mermaid
sequenceDiagram
    participant Courier as Courier
    participant Gateway as API Gateway
    participant Order as Order Service
    participant Db as orderdb
    participant OSRM as OSRM
    Courier->>Gateway: GET /api/routes/optimized
    Gateway->>Order: Forward courier JWT
    Order->>Db: Load Validated and InTransit orders
    Order->>OSRM: Request distance matrix
    alt OSRM unavailable
        Order->>Order: Build Haversine estimate
    end
    Order->>Order: Nearest neighbour and 2-opt
    Order-->>Courier: Ordered stops and route metrics
```
