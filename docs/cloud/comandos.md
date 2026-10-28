# Deployment cloud com Azure e Docker Compose

Este documento descreve um deployment académico numa VM Linux. Não inclui
configuração de produção, gestão de segredos ou certificados de uma autoridade
certificadora.

## 1. Criar a VM

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

## 2. Preparar a VM

```bash
sudo apt update && sudo apt upgrade -y
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker "$USER"
git clone <REPOSITORY_URL> ~/lesto
cd ~/lesto
```

Crie `~/lesto/.env` com valores privados. Use uma nova chave JWT, uma
password forte para PostgreSQL, uma `INTERNAL_API_KEY` aleatória e chaves
Stripe de teste:

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

As variáveis de seed (`ADMIN_*`, `GESTOR_*`, `ESTAFETA_*` e `CLIENTE_*`) são
opcionais.

## 3. HTTPS no Gateway

Para a demonstração, pode ser usado um certificado autoassinado:

```bash
cd ~/lesto/deploy/docker
mkdir -p certs
IP_PUBLICO=$(curl -s ifconfig.me)
openssl req -x509 -newkey rsa:2048 -nodes \
  -keyout certs/tls.key -out certs/tls.crt -days 365 \
  -subj "/CN=$IP_PUBLICO" \
  -addext "subjectAltName=IP:$IP_PUBLICO"
```

Crie `deploy/docker/docker-compose.override.yml`:

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

Em cloud, publique no firewall apenas a porta `8443`. Não abra as portas
5432, 5001, 5002, 5003 ou 5004.

## 4. Iniciar os serviços

Execute a partir de `~/lesto/deploy/docker`:

```bash
docker compose up postgres -d
docker compose up identity-service --build -d
docker compose up order-service-1 --build -d
docker compose up order-service-2 -d
docker compose up payment-service --build -d
docker compose up api-gateway --build -d
docker compose ps
```

Verifique o Gateway:

```bash
curl -k https://localhost:8443/health/live
docker compose logs --tail=100 api-gateway payment-service
```

O webhook Stripe deve apontar para:

```text
https://<PUBLIC_IP>:8443/api/payments/webhook
```

Um certificado autoassinado pode impedir a entrega normal de webhooks Stripe;
nesse caso, use o Stripe CLI para desenvolvimento local ou um certificado
confiável.
