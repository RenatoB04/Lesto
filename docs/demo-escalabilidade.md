# Demonstração de escalabilidade e manutenção

Este procedimento demonstra as duas instâncias do Order Service. Execute os
comandos a partir da raiz do repositório.

## 1. Iniciar a stack

```powershell
docker compose -f deploy\docker\docker-compose.yml up --build -d
docker compose -f deploy\docker\docker-compose.yml ps
```

Confirme que `order-service-1`, `order-service-2` e `api-gateway` estão ativos.

## 2. Confirmar as duas instâncias

```powershell
Invoke-WebRequest http://localhost:5002/swagger/v1/swagger.json -UseBasicParsing
Invoke-WebRequest http://localhost:5003/swagger/v1/swagger.json -UseBasicParsing
```

Ambos os pedidos devem devolver HTTP 200.

## 3. Reiniciar uma instância

Num terminal, monitorize a instância que permanecerá ativa:

```powershell
while ($true) {
  try {
    $response = Invoke-WebRequest http://localhost:5003/swagger/v1/swagger.json -UseBasicParsing -TimeoutSec 3
    "{0} order-service-2: HTTP {1}" -f (Get-Date -Format "HH:mm:ss"), $response.StatusCode
  } catch {
    "{0} order-service-2: pedido falhou - {1}" -f (Get-Date -Format "HH:mm:ss"), $_.Exception.Message
  }
  Start-Sleep -Seconds 1
}
```

Noutro terminal:

```powershell
docker compose -f deploy\docker\docker-compose.yml restart order-service-1
docker compose -f deploy\docker\docker-compose.yml ps
Invoke-WebRequest http://localhost:5002/swagger/v1/swagger.json -UseBasicParsing
```

Use `Ctrl+C` para terminar o monitor.

## 4. Gateway

O Gateway está disponível em `http://localhost:5000` e distribui as rotas do
Order Service em Round Robin. O header `X-Served-By` identifica a instância que
respondeu.

Esta demonstração não garante failover transparente: o YARP não está
configurado com retry ou circuit breaker. Durante o reinício, alguns pedidos
podem falhar se forem encaminhados para a instância indisponível.
