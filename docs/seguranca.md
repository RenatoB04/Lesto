# Revisão de segurança

Este documento descreve as proteções implementadas no estado atual do
projeto. Não substitui a rotação de segredos nem uma auditoria de produção.

## Checklist

- [x] `JWT_SECRET` é obrigatório e deve ser fornecido por `.env` ou pela
  configuração do ambiente.
- [x] As contas de seed são opcionais e só são criadas quando as respetivas
  variáveis de ambiente são fornecidas.
- [x] O Gateway e os serviços aplicam autenticação JWT e autorização por roles.
- [x] As falhas de OSRM e Nominatim devolvem mensagens genéricas; os detalhes
  ficam apenas nos logs do servidor.
- [x] O Payment Service só aceita chaves Stripe de teste (`sk_test_`).
- [x] O checkout usa a página alojada pelo Stripe; dados de cartão não passam
  pelo Lesto.
- [x] O webhook Stripe valida `Stripe-Signature` antes de processar eventos.
- [x] Os endpoints `/internal/orders/...` não têm rota no Gateway e exigem
  `X-Internal-Key`, comparada em tempo constante.
- [x] Clientes só podem pagar e consultar as suas encomendas.
- [x] Reembolsos exigem `Gestor` ou `Administrador` e uma encomenda rejeitada.
- [x] Moradas livres usam `IsCatalog = false` e não aparecem em
  `GET /api/points`.

## Configuração de segredos

O ficheiro `.env` é local e não deve ser versionado. Segredos que tenham sido
usados no passado devem ser rodados antes de um novo deployment.

As variáveis mais sensíveis são:

- `JWT_SECRET`;
- `POSTGRES_PASSWORD`;
- `INTERNAL_API_KEY`;
- `STRIPE_SECRET_KEY`;
- `STRIPE_WEBHOOK_SECRET`;
- passwords das contas de seed.

## Limitações conhecidas

O Compose publica portas internas para facilitar o desenvolvimento local.
Num deployment cloud, apenas o Gateway deve ser exposto publicamente. O
projeto também não possui rate limiting global, retries, circuit breakers,
tracing distribuído ou rotação automática da chave interna. Estas são
limitações conscientes do âmbito académico.
