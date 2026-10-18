# Evidências de Comandos - Fase 5 (Cloud)

## 1. Criação da Infraestrutura no Azure

```bash
# Iniciar sessão e definir subscrição
az login
az account set --subscription "Azure for Students"

# Criar grupo de recursos na região Sweden Central (opção mais económica com disponibilidade)
az group create --name rg-lesto --location swedencentral

# Criar Máquina Virtual 
# Nota: Utilizado o tamanho Standard_B2als_v2 devido a restrições de capacidade (Capacity Restrictions / SkuNotAvailable) no modelo B2s.
az vm create \
  --resource-group rg-lesto \
  --name vm-lesto \
  --image Ubuntu2404 \
  --size Standard_B2als_v2 \
  --admin-username lesto \
  --ssh-key-values C:\Users\Legion\.ssh\lesto_vm.pub \
  --public-ip-sku Standard

# Aceder à VM por SSH
ssh -i C:\Users\Legion\.ssh\lesto_vm lesto@<IP_PUBLICO>

```

## 2. Preparação do Ambiente e Certificados (na VM)

```bash
# Atualizar sistema e instalar Docker
sudo apt update && sudo apt upgrade -y
curl -fsSL [https://get.docker.com](https://get.docker.com) | sudo sh
sudo usermod -aG docker $USER

# Navegar para a pasta de deploy
cd ~/lesto/deploy/docker

# Criar ficheiro .env com as credenciais e nova chave JWT gerada
cat > ../../.env << 'EOF'
POSTGRES_USER=example_user
POSTGRES_PASSWORD=replace_with_a_strong_database_password
JWT_SECRET=replace_with_a_random_secret_of_at_least_32_characters
ADMIN_EMAIL=admin@example.test
ADMIN_PASSWORD=replace_with_admin_password
GESTOR_EMAIL=gestor@example.test
GESTOR_PASSWORD=replace_with_gestor_password
ESTAFETA_EMAIL=estafeta@example.test
ESTAFETA_PASSWORD=replace_with_estafeta_password
CLIENTE_EMAIL=cliente@example.test
CLIENTE_PASSWORD=replace_with_cliente_password
EOF

# Gerar certificados HTTPS autoassinados para o Gateway
mkdir -p certs
IP_PUBLICO=$(curl -s ifconfig.me)
openssl req -x509 -newkey rsa:2048 -nodes \
  -keyout certs/tls.key -out certs/tls.crt -days 365 \
  -subj "/CN=$IP_PUBLICO" \
  -addext "subjectAltName=IP:$IP_PUBLICO"

# Criar ficheiro de sobreposição (override) do docker-compose para expor a porta 8443 com os certificados
cat > docker-compose.override.yml << 'EOF'
services:
  api-gateway:
    ports:
      - "8443:8443"
    volumes:
      - ./certs:/tls:ro
    environment:
      - ASPNETCORE_HTTPS_PORTS=8443
      - Kestrel__Certificates__Default__Path=/tls/tls.crt
      - Kestrel__Certificates__Default__KeyPath=/tls/tls.key
EOF

```

## 3. Publicação da Stack Docker

```bash
# Iniciar a base de dados
docker compose up postgres -d

# Compilar e iniciar serviços sequencialmente para evitar esgotamento de memória (OOM) na VM
docker compose up identity-service --build -d
docker compose up order-service-1 --build -d
docker compose up order-service-2 -d
docker compose up api-gateway --build -d

# Validar contentores em execução
docker compose ps

```