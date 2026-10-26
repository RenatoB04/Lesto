# Revisao de seguranca

Revisao realizada em 2026-10-16. Esta checklist regista o estado encontrado no
repositorio; itens com problemas identificados nao devem ser considerados
conformes ate a correcao e nova verificacao.

## Checklist

- [x] **Segredo JWT fora do Git - CONFORME NO CODIGO ATUAL.** Gateway,
  Identity Service e Order Service exigem `JWT_SECRET` externa; o `.env` local
  esta ignorado e nao esta versionado. Como uma chave predefinida esteve
  versionada, rodar a chave antes da proxima utilizacao/deploy e verificar o
  historico Git.
- [x] **Credenciais de contas fora do Git - CONFORME NO CODIGO ATUAL.** Foram
  removidas as contas e palavras-passe predefinidas. O administrador inicial e
  opcional e so e criado quando `ADMIN_EMAIL` e `ADMIN_PASSWORD` sao fornecidos
  fora do Git. Credenciais existentes em bases de dados persistentes devem ser
  rodadas ou removidas; configurar variaveis de ambiente privadas no Postman.
- [x] **Roles e autorizacao corretas nos endpoints revistos - CORRIGIDO.**
  `GET /api/orders/{id}` permite ao cliente apenas a propria encomenda, ao
  estafeta apenas encomendas atribuidas e a Gestor/Administrador acesso geral;
  outras roles recebem `403`.
- [x] **Erros sem detalhes internos - CORRIGIDO NOS ENDPOINTS REVISTOS.**
  `OrdersController` e `TesteOsrmController` devolvem mensagens genericas para
  falhas OSRM; a excecao completa e registada no log do servidor, nao na resposta.
- [x] **Pesquisa de segredos no repositorio executada.** A pesquisa inicial nos
  ficheiros versionados encontrou a chave JWT predefinida e credenciais fixas
  do seeder e da colecao Postman. Foram removidas do codigo/documentacao atual
  e substituidas por configuracao externa. O `.env` nao esta versionado.
  Verificar novamente o historico Git e rodar quaisquer credenciais que tenham
  sido utilizadas.

- [x] **Pagamentos (Stripe) - 2026-10-26.** A chave `STRIPE_SECRET_KEY` e o
  `STRIPE_WEBHOOK_SECRET` existem apenas no `.env`; o Payment Service recusa
  arrancar com chaves que nao sejam de teste (`sk_test_`). O pagamento e feito
  na pagina alojada pelo Stripe, por isso nenhum dado de cartao passa pelo Lesto.
- [x] **Webhook autenticado.** `POST /api/payments/webhook` e anonimo no
  Gateway, mas o servico verifica a assinatura `Stripe-Signature`; pedidos sem
  assinatura valida recebem `400`. A pagina de regresso nao confia no
  `session_id` do URL: confirma sempre o estado junto do Stripe.
- [x] **Comunicacao entre servicos.** Os endpoints `/internal/orders/...` nao
  tem rota no Gateway e exigem `X-Internal-Key` igual a `INTERNAL_API_KEY`,
  comparada em tempo constante; sem chave configurada devolvem `503`.
- [x] **Autorizacao dos pagamentos.** O cliente so paga e consulta as proprias
  encomendas; reembolsos so para Gestor/Administrador e apenas em encomendas
  rejeitadas.

## Evidencias e seguimento

As verificacoes referem-se ao codigo atual; credenciais presentes no historico
Git exigem rotacao e nao sao removidas por estas alteracoes.
