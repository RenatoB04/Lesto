# Cloud deployment with Azure and Docker Compose

This document describes an academic deployment on a Linux VM. It does not
include production configuration, secret management or certificates issued by
a trusted certificate authority.

## 1. Create the VM

```bash
az login
az account set --subscription "Azure for Students"
az group create --name rg-lesto --location swedencentral
az vm create \
  --resource-group rg-lesto \
  --name vm-lesto \
  --image Ubuntu2404 \
  --size Standard_B2als_v2 \
  --admin-username lesto \
  --ssh-key-values ~/.ssh/lesto_vm.pub \
  --public-ip-sku Standard
ssh -i ~/.ssh/lesto_vm lesto@<PUBLIC_IP>
```

## 2. Prepare the VM

```bash
sudo apt update && sudo apt upgrade -y
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker "$USER"
git clone <REPOSITORY_URL> ~/lesto
cd ~/lesto
```

Create `~/lesto/.env` with private values. Use a new JWT secret, a strong
PostgreSQL password, a random `INTERNAL_API_KEY` and Stripe test keys:

```text
POSTGRES_USER=example_user
POSTGRES_PASSWORD=<strong-password>
JWT_SECRET=<random-secret-at-least-32-characters>
INTERNAL_API_KEY=<random-internal-key>
OSRM_BASE_URL=https://router.project-osrm.org
NOMINATIM_BASE_URL=https://nominatim.openstreetmap.org
STRIPE_SECRET_KEY=sk_test_<test-key>
STRIPE_WEBHOOK_SECRET=whsec_<webhook-secret>
PUBLIC_BASE_URL=https://<PUBLIC_IP>:8443
```

Seed variables (`ADMIN_*`, `GESTOR_*`, `ESTAFETA_*` and `CLIENTE_*`) are
optional.

## 3. Configure HTTPS on the Gateway

For the demonstration, a self-signed certificate can be used:

```bash
cd ~/lesto/deploy/docker
mkdir -p certs
PUBLIC_IP=$(curl -s ifconfig.me)
openssl req -x509 -newkey rsa:2048 -nodes \
  -keyout certs/tls.key -out certs/tls.crt -days 365 \
  -subj "/CN=$PUBLIC_IP" \
  -addext "subjectAltName=IP:$PUBLIC_IP"
```

Create `deploy/docker/docker-compose.override.yml`:

```yaml
services:
  api-gateway:
    ports:
      - "8443:8443"
    volumes:
      - ./certs:/tls:ro
    environment:
      ASPNETCORE_HTTPS_PORTS: 8443
      Kestrel__Certificates__Default__Path: /tls/tls.crt
      Kestrel__Certificates__Default__KeyPath: /tls/tls.key
```

In the cloud, expose only port `8443` in the firewall. Do not open ports 5432,
5001, 5002, 5003 or 5004.

## 4. Start the services

Run from `~/lesto/deploy/docker`:

```bash
docker compose up postgres -d
docker compose up identity-service --build -d
docker compose up order-service-1 --build -d
docker compose up order-service-2 -d
docker compose up payment-service --build -d
docker compose up api-gateway --build -d
docker compose ps
```

Check the Gateway:

```bash
curl -k https://localhost:8443/health/live
docker compose logs --tail=100 api-gateway payment-service
```

The Stripe webhook should point to:

```text
https://<PUBLIC_IP>:8443/api/payments/webhook
```

A self-signed certificate may prevent normal Stripe webhook delivery. In that
case, use the Stripe CLI for local development or a trusted certificate.
