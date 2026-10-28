# Security review

This document describes the protections implemented in the current project
state. It does not replace secret rotation or a production security audit.

## Checklist

- [x] `JWT_SECRET` is required and must be supplied through `.env` or
  environment configuration.
- [x] Seed accounts are optional and are only created when their environment
  variables are provided.
- [x] The Gateway and services apply JWT authentication and role authorization.
- [x] OSRM and Nominatim failures return generic messages; details are kept
  only in server logs.
- [x] The Payment Service only accepts Stripe test keys (`sk_test_`).
- [x] Checkout uses Stripe-hosted pages; card data does not pass through Lesto.
- [x] `/internal/orders/...` endpoints have no Gateway route and require
  `X-Internal-Key`, compared in constant time.
- [x] Customers can only pay for and query their own orders.
- [x] Refunds require `Gestor` or `Administrador` and a rejected order.
- [x] Free-text addresses use `IsCatalog = false` and are not returned by
  `GET /api/points`.

## Secret configuration

The `.env` file is local and must not be committed. Secrets that were used in
the past should be rotated before a new deployment.

The most sensitive variables are:

- `JWT_SECRET`;
- `POSTGRES_PASSWORD`;
- `INTERNAL_API_KEY`;
- `STRIPE_SECRET_KEY`;
- `STRIPE_WEBHOOK_SECRET`;
- seed account passwords.

## Known limitations

Compose publishes internal ports to simplify local development. In a cloud
deployment, only the Gateway should be publicly exposed. The project also has
no global rate limiting, retries, circuit breakers, distributed tracing or
automatic internal-key rotation. These are conscious limitations of the
academic scope.
