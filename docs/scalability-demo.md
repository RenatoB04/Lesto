# Scalability and maintenance demonstration

This procedure demonstrates the two Order Service instances. Run the commands
from the repository root.

## 1. Start the stack

```powershell
docker compose -f deploy\docker\docker-compose.yml up --build -d
docker compose -f deploy\docker\docker-compose.yml ps
```

Confirm that `order-service-1`, `order-service-2` and `api-gateway` are running.

## 2. Check both instances

```powershell
Invoke-WebRequest http://localhost:5002/swagger/v1/swagger.json -UseBasicParsing
Invoke-WebRequest http://localhost:5003/swagger/v1/swagger.json -UseBasicParsing
```

Both requests should return HTTP 200.

## 3. Restart one instance

In one terminal, monitor the instance that will remain active:

```powershell
while ($true) {
  try {
    $response = Invoke-WebRequest http://localhost:5003/swagger/v1/swagger.json -UseBasicParsing -TimeoutSec 3
    "{0} order-service-2: HTTP {1}" -f (Get-Date -Format "HH:mm:ss"), $response.StatusCode
  } catch {
    "{0} order-service-2: request failed - {1}" -f (Get-Date -Format "HH:mm:ss"), $_.Exception.Message
  }
  Start-Sleep -Seconds 1
}
```

In another terminal:

```powershell
docker compose -f deploy\docker\docker-compose.yml restart order-service-1
docker compose -f deploy\docker\docker-compose.yml ps
Invoke-WebRequest http://localhost:5002/swagger/v1/swagger.json -UseBasicParsing
```

Use `Ctrl+C` to stop the monitor.

## 4. Gateway

The Gateway is available at `http://localhost:5000` and distributes Order
Service routes using Round Robin. The `X-Served-By` header identifies the
responding instance.

This demonstration does not guarantee transparent failover: YARP is not
configured with retries or a circuit breaker. During the restart, some requests
may fail if they are routed to the unavailable instance.
