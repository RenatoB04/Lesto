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

## Evidencias e seguimento

As verificacoes referem-se ao codigo atual; credenciais presentes no historico
Git exigem rotacao e nao sao removidas por estas alteracoes.
