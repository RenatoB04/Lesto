# Demonstração de escalabilidade e manutenção

Este procedimento demonstra as duas instâncias do Order Service e que
`order-service-2` continua acessível enquanto `order-service-1` reinicia.
Execute os comandos a partir da raiz do repositório.

## 1. Iniciar os serviços

```powershell
docker compose -f deploy/docker/docker-compose.yml up --build -d
docker compose -f deploy/docker/docker-compose.yml ps
```

Confirme que `order-service-1`, `order-service-2` e `api-gateway` estão em
execução.

## 2. Confirmar que ambas respondem

O Swagger do Order Service permite verificar cada instância diretamente, sem
precisar de um token:

```powershell
Invoke-WebRequest http://localhost:5002/swagger/v1/swagger.json -UseBasicParsing
Invoke-WebRequest http://localhost:5003/swagger/v1/swagger.json -UseBasicParsing
```

Ambos os pedidos devem devolver HTTP 200.

## 3. Reiniciar uma instância e monitorizar a outra

Num terminal, deixe este pedido a repetir-se para a instância que não será
reiniciada:

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

Noutro terminal, reinicie `order-service-1`:

```powershell
docker compose -f deploy/docker/docker-compose.yml restart order-service-1
```

O monitor da porta `5003` deve continuar a mostrar HTTP 200 durante o
reinício. Confirme que a primeira instância voltou a ficar ativa:

```powershell
docker compose -f deploy/docker/docker-compose.yml ps
Invoke-WebRequest http://localhost:5002/swagger/v1/swagger.json -UseBasicParsing
```

Use `Ctrl+C` para terminar o monitor.

## 4. Verificar o gateway

O gateway está publicado em `http://localhost:5000`. As rotas para
`/api/orders`, `/api/points` e `/api/settings` apontam para o cluster do Order
Service, que distribui pedidos em round-robin pelas duas instâncias. As rotas
de negócio podem exigir um JWT válido.

**Nota:** a configuração atual não define health checks ativos nem retry para
os destinos do YARP. Por isso, durante o reinício, pedidos encaminhados pelo
gateway podem falhar temporariamente se o round-robin escolher a instância
que está indisponível. O teste direto na porta `5003` demonstra que a outra
instância continua a responder; não demonstra failover transparente do
gateway.
