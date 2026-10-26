# Sequence Diagrams

## 1. Login

```mermaid
sequenceDiagram
    participant Client as API Client
    participant Gateway as API Gateway
    participant Identity as Identity Service
    participant Db as identitydb

    Client->>Gateway: POST /api/auth/login
    Gateway->>Identity: Forward request
    Identity->>Db: Find user and verify password
    Db-->>Identity: User and role
    Identity-->>Gateway: accessToken, expiresAt, user
    Gateway-->>Client: 200 OK
```

## 2. Create order

```mermaid
sequenceDiagram
    participant Client as API Client
    participant Gateway as API Gateway
    participant Order as Order Service
    participant Db as orderdb
    participant OSRM as OSRM

    Client->>Gateway: POST /api/orders (Bearer customer token)
    Gateway->>Order: Forward authenticated request
    Order->>Order: Validate role, weight, and points
    Order->>Db: Read origin, destination, and weight setting
    Order->>OSRM: Route request with point coordinates
    OSRM-->>Order: Distance and duration
    Order->>Db: Persist Pending order
    Order-->>Gateway: 201 Created
    Gateway-->>Client: Created order
```

## 3. Assign order

```mermaid
sequenceDiagram
    participant Client as API Client
    participant Gateway as API Gateway
    participant Order as Order Service
    participant Identity as Identity Service
    participant Db as orderdb

    Client->>Gateway: POST /api/orders/{id}/assign
    Gateway->>Order: Forward manager bearer token
    Order->>Order: Validate manager role, Pending/Failed state, and Paid payment
    Order->>Identity: GET /api/users/{courierId} (manager token)
    Identity-->>Order: User profile and Estafeta role
    Order->>Db: Store courier and set Validated
    Order-->>Gateway: 200 OK
    Gateway-->>Client: Updated order
```

## 4. Update delivery status

```mermaid
sequenceDiagram
    participant Client as API Client
    participant Gateway as API Gateway
    participant Order as Order Service
    participant Db as orderdb

    Client->>Gateway: POST /api/orders/{id}/pickup
    Gateway->>Order: Forward courier bearer token
    Order->>Db: Load order
    Order->>Order: Check assigned courier and Validated state
    Order->>Db: Save InTransit status
    Order-->>Gateway: 200 OK
    Gateway-->>Client: Updated order

    Client->>Gateway: POST /api/orders/{id}/deliver or /fail
    Gateway->>Order: Forward courier bearer token
    Order->>Order: Check assigned courier and InTransit state
    Order->>Db: Save Delivered or Failed status
    Order-->>Gateway: 200 OK
    Gateway-->>Client: Updated order
```

## 5. Pay an order (Stripe Checkout)

```mermaid
sequenceDiagram
    participant Client as API Client / Browser
    participant Gateway as API Gateway
    participant Payment as Payment Service
    participant Order as Order Service
    participant Stripe as Stripe (test mode)
    participant Db as paymentdb

    Client->>Gateway: POST /api/payments/checkout {orderId} (customer token)
    Gateway->>Payment: Forward authenticated request
    Payment->>Order: GET /internal/orders/{id} (X-Internal-Key)
    Order-->>Payment: customerId, price, status, paymentStatus
    Payment->>Payment: Check owner, Pending and Unpaid
    Payment->>Stripe: Create Checkout Session (amount in cents)
    Stripe-->>Payment: sessionId, checkout URL
    Payment->>Db: Save Payment (Pending)
    Payment-->>Client: 201 checkoutUrl
    Client->>Stripe: Pay on the hosted page (card 4242 4242 4242 4242)
    Stripe->>Gateway: POST /api/payments/webhook (signed event)
    Gateway->>Payment: Forward (anonymous route)
    Payment->>Payment: Verify Stripe-Signature
    Payment->>Db: Mark Payment as Paid
    Payment->>Order: POST /internal/orders/{id}/payment {Paid} (X-Internal-Key)
    Order-->>Payment: 200 OK
    Stripe-->>Client: Redirect to /api/payments/return/success
    Note over Payment,Stripe: The return page and GET /api/payments/{orderId}<br/>query Stripe again, so a lost webhook is recovered.
```

## 6. Refund a rejected order

```mermaid
sequenceDiagram
    participant Client as API Client
    participant Gateway as API Gateway
    participant Payment as Payment Service
    participant Order as Order Service
    participant Stripe as Stripe (test mode)

    Client->>Gateway: POST /api/payments/{orderId}/refund (manager token)
    Gateway->>Payment: Forward authenticated request
    Payment->>Order: GET /internal/orders/{id} (X-Internal-Key)
    Order-->>Payment: status Rejected
    Payment->>Stripe: Create refund for the PaymentIntent
    Payment->>Order: POST /internal/orders/{id}/payment {Refunded}
    Payment-->>Client: 200 OK
```

## 7. Create order with free-text addresses

```mermaid
sequenceDiagram
    participant Client as API Client
    participant Gateway as API Gateway
    participant Order as Order Service
    participant Db as orderdb
    participant Nominatim as Nominatim
    participant OSRM as OSRM

    Client->>Gateway: POST /api/orders {originAddress, destinationAddress}
    Gateway->>Order: Forward authenticated request
    Order->>Db: Look for an already geocoded address
    alt Address not stored yet
        Order->>Nominatim: /search?q=...&countrycodes=pt (1 request/second)
        Nominatim-->>Order: lat, lon, display_name
    end
    Order->>OSRM: Route request with the coordinates
    OSRM-->>Order: Distance and duration
    Order->>Db: Save new points and the Pending order (one transaction)
    Order-->>Client: 201 Created (orderId, price)
```
