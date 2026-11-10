# 02 · O `CancellationToken` que fica pelo caminho

**Branch:** `masterclass/dotnet-producao/02-cancellation-token` · **Parte de:** `01-sync-over-async`
**Para ver o que mudou:** `git diff masterclass/dotnet-producao/01-sync-over-async..masterclass/dotnet-producao/02-cancellation-token`

## O problema

O `CancellationToken` percorria os métodos do consumer, mas parava antes de chegar onde o trabalho realmente acontece: no banco, na chamada HTTP e nas operações que uma política de resiliência cria por conta própria.

### 1. O token para na porta do banco

O token entra pelo consumer e desce até o acesso a dados, mas **ninguém o entrega ao driver**. Em [IntegrationEventsDataAccess.cs](../src/Eximia.Integrations/IntegrationEvents/IntegrationEventsDataAccess.cs):

```csharp
public async Task<Maybe<IntegrationEvent>> GetAndSetEventToDispatch(Guid uniqueId, CancellationToken cancellationToken)
{
    // ...
    var dto = await connection.QueryFirstOrDefaultAsync<EventDto>(sql, param);   // o token não vai junto
```

O mesmo vale para `GetAsync` e `GetDestinyAsync`. A query segue até o fim no Postgres (visível em `pg_stat_activity`), a conexão do pool continua ocupada e, no shutdown, o processo espera, ou é morto, com trabalho pendurado. Para quem olha o código, "está cancelável". Para o banco, não está.

> **E o `.WaitAsync(cancellationToken)`?** Ele não resolve. Pendurar `.WaitAsync(ct)` numa chamada que não recebeu o token só faz **quem chamou** parar de esperar quando o token é cancelado: a operação em si continua rodando. O caminho certo é passar o token **para a própria chamada**, que é o que a seção "O token vai até o driver" faz.

### 2. Aceitar o token na assinatura e ignorá-lo

```csharp
public async Task InsertAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
{
    // ...
    var result = await connection.ExecuteScalarAsync<int>(sql, param);   // o token nunca chega aqui
```

Isto é **pior** do que não ter o token: dá a falsa impressão de que o método respeita o cancelamento.

### 3. Métodos que nem recebem o token

`UpdateIntegrationProcess` e `UpdateKafkaOffsetAsync` nem tinham o parâmetro. O mesmo valia para as consultas de rota (`RoutingDataAccess`), e `GetRoutesByOriginAndEntityAsync` recebia o token na assinatura sem usá-lo.

### 4. A política de resiliência executada sem o token: HTTP e retries que não param

A entrega ao destino ([HttpJsonWithJwtDispatcher.cs](../src/Eximia.Integrations/Routing/Dispatchers/HttpJsonWithJwtDispatcher.cs) e [HttpJsonDispatcher.cs](../src/Eximia.Integrations/Routing/Dispatchers/HttpJsonDispatcher.cs)) roda dentro de uma política do Polly (fallback, renovação de token e retry):

```csharp
var result = await policy.ExecuteAsync(
    action: async context =>
    {
        var authToken = await clientCredentialsProvider.GetTokenAsync(route, integrationEvent.ControlCode);
        using var response = await uri
            // ...
            .WithTimeout(TimeSpan.FromSeconds(route.TimeOut))
            .PostStringAsync(payload);
        // ...
    },
    policyContext);
```

O método recebe `cancellationToken`, mas ele **não entra na política**. As consequências vão além da chamada HTTP:

- **A chamada HTTP só termina pelo timeout da rota.** No shutdown, uma entrega para um destino lento segura o processo até `route.TimeOut`.
- **As esperas entre tentativas não podem ser canceladas.** O retry espera `2^n` segundos entre as tentativas (2, 4, 8, 16, 32...). Essas esperas são **operações filhas criadas pela política**, e ela só consegue cancelá-las com o token que recebe em `ExecuteAsync`. Sem ele, um destino fora do ar prende o consumer por mais de um minuto, cancelado ou não.
- **A aquisição do token JWT também fica sem cancelamento.** É outra chamada HTTP, feita dentro de cada tentativa.

É o caso mais traiçoeiro deste documento: **não há nenhum `CancellationToken.None` escrito**. O token simplesmente não foi passado adiante num único ponto, e tudo que nasce depois dele herda "nunca cancele".

### 5. O resultado depende do token do consumer, mesmo quando não deveria

Depois que o destino **já recebeu** o evento, a gravação do resultado usava o mesmo token do consumer. Se o processo for encerrado exatamente nesse momento, o cancelamento aborta a gravação e o evento fica "não confirmado". Quando o Kafka reentrega, o destino recebe a mesma entrega **duas vezes**.

## A solução

### O token vai até o driver

```csharp
var dto = await connection.QueryFirstOrDefaultAsync<EventDto>(new CommandDefinition(
    $@"UPDATE public.integration_events ... RETURNING {_eventColumns};",
    param,
    cancellationToken: cancellationToken));
```

`CommandDefinition` (Dapper) leva o token para o Npgsql, que envia o cancelamento ao servidor: a query **realmente para** e a conexão volta ao pool. Todos os métodos de acesso a dados passaram a receber e repassar o token: `InsertAsync`, `GetAndSetEventToDispatch`, `UpdateIntegrationProcess`, `FinalizeOriginalIntegrationEventsForCallback`, `GetAsync`, `UpdateKafkaOffsetAsync`, `UpdatePayloadAsync`, `GetDestinyAsync`, as consultas de rota e a configuração de erro por destino. O `PublishAsync` do callback de erro (`HandleRoutingErrorService`) também recebe o token. Os chamadores (`RouteIntegrationEventApplication` e `HandleRoutingErrorService`) foram ajustados para repassá-lo.

### O token entra na política, e a política o entrega a cada tentativa

```csharp
var result = await policy.ExecuteAsync(
    action: async (context, token) =>
    {
        var authToken = await clientCredentialsProvider.GetTokenAsync(route, integrationEvent.ControlCode, token);
        using var response = await uri
            // ...
            .PostStringAsync(payload, cancellationToken: token);
        // ...
    },
    policyContext,
    cancellationToken);
```

Agora o cancelamento interrompe as três coisas: a chamada em andamento, a espera entre tentativas e a aquisição do token. Repare que o delegate usa o `token` **que a política entrega**, e não o `cancellationToken` do método: políticas como timeout criam um token ligado ao seu, e é esse que precisa chegar à chamada.

A regra vale para qualquer código que cria trabalho em seu nome (Polly, `Parallel.ForEachAsync`, `Task.WhenAll` sobre várias chamadas, um `Channel`): **o token precisa entrar no orquestrador**, que o repassa a cada operação filha.

### Onde o token do chamador NÃO deve ser usado

```csharp
// O destino JÁ recebeu o evento. Gravar o resultado com o token do consumer faria um cancelamento
// (shutdown) deixar o evento "não confirmado" e a reentrega duplicaria a entrega: por isso, aqui, um
// token PRÓPRIO e curto, e não o do chamador.
using var persist = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await integrationEventsDataAccess.UpdateIntegrationProcess(integrationEvent.Value, persist.Token);
```

A regra geral: **depois de um efeito colateral externo que você não consegue desfazer**, registrar o resultado é parte da mesma operação. Esse passo não deve ser abortado pelo shutdown, mas também não pode ficar sem prazo: daí o token próprio de 10 segundos.

## O que ficou de fora, de propósito

- **`IOutboxReader`** (interface do Silverback) não recebe token em `GetAsync` nem em `AcknowledgeAsync`, então o `EximiaOutboxReader` não tem de onde propagá-lo.
- As consultas de rota usam um cache com *factory*. O token do chamador é repassado, mas lembre que o factory pode rodar em nome de várias requisições: uma exceção de cancelamento não é armazenada no cache e a próxima tenta de novo.
- O callback de renovação de token no 401 (`onRetryAsync` do Polly) não recebe token nessa versão da biblioteca.

## Regras para levar

- **`.WaitAsync(ct)` não cancela a operação**, só faz quem chama parar de esperar por ela. Passe o token para a própria chamada.
- **Receber um `CancellationToken` e não repassá-lo é um defeito**, mesmo que compile e passe nos testes.
- **O token precisa entrar em quem cria operações filhas** (políticas de retry, loops paralelos, `WhenAll`), e cada operação filha usa o token que recebe dele.
- **Combine token do host com timeout próprio** com `CancellationTokenSource.CreateLinkedTokenSource(stoppingToken)` + `CancelAfter(timeout)`, e distinga os dois casos no `catch`: `catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)` é o timeout; o contrário é o shutdown.
- **Nunca use `CancellationToken.None` por comodidade.** Quando for intencional (como o `persist` acima), diga por quê num comentário.

## Novidades de plataforma

- `CancellationTokenSource.CancelAsync` (.NET 8) executa os callbacks de cancelamento de forma assíncrona, evitando que callbacks lentos bloqueiem quem cancela.

## Próxima branch

`03-tempo-de-vida`: singleton desnecessário e `DbContext` preso em serviço persistente.
