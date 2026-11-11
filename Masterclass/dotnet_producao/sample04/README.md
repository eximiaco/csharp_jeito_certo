# Sample 4 · A solução completa

Todos os pontos corrigidos e bem implementados. Os samples 1, 2 e 3 são **este sample com só os problemas do seu
tema plantados**: `diff -rq sampleNN/src sample04/src -x bin -x obj` mostra exatamente as correções.

Para compilar: `dotnet build Eximia.Integrations.slnx`. Nada aqui precisa ser executado: a leitura do código basta.

## O fluxo

```
Origem ──POST /api/integrations──► HttpApi ──(evento + outbox, mesma transação)──► Postgres
                                      │                                                │
                                 202 Accepted                                    OutboxWorker
                                                                                       ▼
                                                                  Kafka (RouteIntegrationEventRequestCommand)
                                                                                       │
                                                     EventDispatcher (consumer) ◄──────┘
                                                            │ entrega via HTTP
                                                            ▼
                                                         Destino ──POST /api/integrations/callback──► HttpApi ──► Origem

BackgroundAgent: reprocessa eventos parados e consome os eventos da plataforma (linha do tempo).
```

## 1️⃣ Código assíncrono de ponta a ponta

- Nenhum `.Result`, `.Wait()` ou `GetAwaiter().GetResult()` no fluxo de requisição e de mensagem. O `onRetry` do Polly
  virou `onRetryAsync` (`Routing/Dispatchers/PolicyFactory.cs`).
- **Token do destino com renovação single-flight** (`Routing/Authentication/TokenCache.cs`): sob carga, N mensagens
  simultâneas geram **uma** chamada ao provedor de identidade (um `SemaphoreSlim` por destino). Guarda o instante
  absoluto de expiração, devolve o valor do cache e é **singleton** (registro na linha 68 de `DependencyInjection.cs`), porque os
  semáforos precisam sobreviver entre as mensagens.

## 2️⃣ O token chega até onde importa

- Todo acesso a dados usa `CommandDefinition(..., cancellationToken: ct)`: o Npgsql envia o cancelamento e a query some
  do `pg_stat_activity`. `WaitAsync` não é usado em lugar nenhum: ele só para de esperar, **não cancela**.
- O token chega ao publisher, ao HTTP e à política de resiliência.
- **Quando NÃO usar o token do consumer:** depois que o destino recebeu o evento, gravar o resultado usa um token
  **próprio e curto** (`RouteIntegrationEventApplication.cs`, linha 74). Cancelar ali geraria reentrega e duplicidade.

## 5️⃣ Tempo de vida das dependências

- **Handler da API e `PublishPlataformEventService` scoped** (linha 90 de `DependencyInjection.cs`). `SendIntegrationEventService` também (linha 112): não havia o que compartilhar.
- A regra que decide o tempo de vida: **"esse objeto guarda ou depende de algo de uma operação específica?"**, e não
  "quantas vezes é usado".

| Tempo de vida | Quando | Exemplos neste projeto |
|---|---|---|
| **Singleton** | Sem estado por operação, thread-safe, caro de criar ou dono de um recurso compartilhado | `TokenCache`, `PostgresConnectionStringFactory`, `NpgsqlDataSource` |
| **Scoped** | Uma unidade de trabalho (requisição ou **mensagem**), ou qualquer coisa que dependa dela | `DbContext`, `IPublisher`, acesso a dados, handlers dos casos de uso |
| **Transient** | Barato e sem estado | (não houve necessidade) |

- **Escopo por iteração no `BackgroundService`:** o `ReprocessStuckEventsWorker` continua singleton, mas só **agenda**.
  O trabalho, que usa o `DbContext`, roda numa classe scoped (`ReprocessStuckEvents`) resolvida num escopo criado a
  cada iteração (linha 29): o `DbContext` nasce e morre com a iteração.
- **Validação de container ligada nos três hosts, em todos os ambientes** (`ValidateScopes` e `ValidateOnBuild`):
  `HttpApi/Program.cs` linha 51, `EventDispatcher/Program.cs` linha 44, `BackgroundAgent/Program.cs` linha 40.
- Sem sobras de Web API no dispatcher, e o `IOutboxReader` registrado para o health check da API.

## 6️⃣ Encerramento com cuidado

- **Cancelamento não é falha.** O retry e o fallback do Polly ignoram `OperationCanceledException` (`PolicyFactory.cs`,
  linha 64), e o consumer relança com `catch (OperationCanceledException) when (ct.IsCancellationRequested)`
  (`RouteIntegrationEventApplication.cs`, linha 81). O Silverback não confirma o offset, e o evento é reentregue após o restart:
  nada de erro falso nem callback de erro para a origem.
- **`ShutdownTimeout` explícito nos três hosts** (30 s; `HttpApi/Program.cs` linha 57, `EventDispatcher/Program.cs` linha 50, `BackgroundAgent/Program.cs` linha 44), e **menor** que o `terminationGracePeriodSeconds` do pod.
- **Pool com dono:** um `NpgsqlDataSource` singleton (`DependencyInjection.cs`, linha 43) é dono do pool e é
  descartado no encerramento do host. As conexões ociosas são fechadas pelo protocolo do Postgres, e não cortadas
  quando o processo morre.

## 4️⃣ O BackgroundService e o host certos para cada trabalho

| Host | Formato | Por quê |
|---|---|---|
| `HttpApi` | Web API completa | Recebe HTTP |
| `EventDispatcher` | **Web host mínimo** (`CreateSlimBuilder`, `Program.cs` linha 9) só com os probes `/health-*` | Só consome mensagens; o Kubernetes precisa de HTTP só para os probes |
| `BackgroundAgent` | **Worker Service puro** (`Microsoft.NET.Sdk.Worker`, `Host.CreateApplicationBuilder`) | Não recebe HTTP. Sem Kestrel, sem MVC, sem Hangfire |

- **`ReprocessStuckEventsWorker`:** `PeriodicTimer.WaitForNextTickAsync(stoppingToken)` (linha 23) em vez de `while(true)` +
  `Task.Delay`; `try/catch` **por iteração**, com filtro que deixa passar `OperationCanceledException`; tokens até o
  EF e o Dapper.
- **Exclusão entre réplicas** com *advisory lock* do Postgres (`Shared/Postgres/PostgresAdvisoryLock.cs`): é o que o
  Hangfire dava "de graça", e num Worker puro precisa ser explícito. A liberação do lock **não** depende do token já
  cancelado.
- Para rodar como serviço do Windows ou systemd, basta acrescentar `AddWindowsService()` / `AddSystemd()` no `Program.cs`.

## 3️⃣ Sem tarefas soltas

- O evento da plataforma é **aguardado** dentro do loop, e as ações pós-registro rodam em sequência, com `await`
  (`RegisterMultipleRoutesIntegrationEventApplication.cs`). O loop é **sequencial de propósito**: o `DbContext` não é
  thread-safe e há uma transação de outbox aberta.
- **Quando fire-and-forget é aceitável?** Quando perder o trabalho é aceitável **e** há um dono para a exceção
  (uma fila em memória limitada consumida por um `BackgroundService`). Quando perder não é aceitável, o caminho é o
  outbox: foi o que o sistema fez para as mensagens de negócio.

## O consumer Kafka com a lib pura da Confluent (`Workers/PlatformEventsConsumer.cs`)

O agent consome o tópico de eventos da plataforma (`Kafka:Events:Topic`, o que a API e o dispatcher já publicam) e
grava uma **linha do tempo** por integração (recebido, enviado, erro) em `integration_event_timeline`. O fluxo
principal continua no Silverback. O DDL está em [sql/integration_event_timeline.sql](sql/integration_event_timeline.sql):
nenhum worker cria tabela ou índice na subida.

As práticas recomendadas pela Confluent aplicadas:

| Prática | Como está no código |
|---|---|
| **Thread dedicada** | `Consume()` é bloqueante. Roda numa thread `LongRunning` (linha 49), não numa thread do pool. Como a thread é nossa, o I/O do handler é **síncrono** de propósito: não há `.Result` nem `GetResult()` |
| **At-least-once** | `EnableAutoCommit = true` e `EnableAutoOffsetStore = false` (linha 168): o offset só é marcado com `StoreOffset` (linha 98) **depois** de processar. O commit roda em segundo plano |
| **Idempotência** | A reentrega é normal. A gravação usa `INSERT ... ON CONFLICT DO NOTHING`, com a chave natural da mensagem (`topic`, `partition`, `offset`) |
| **Encerramento limpo** | `Close()` num `finally` (linha 108): confirma os offsets guardados e sai do grupo na hora, sem esperar o `session.timeout` |
| **Rebalance cooperativo** | `PartitionAssignmentStrategy.CooperativeSticky` (linha 170): só as partições que mudam de dono são pausadas |
| **Erros do client** | `SetErrorHandler` e `SetLogHandler` (para o Serilog). `ConsumeException` não fatal é logada e o loop segue; erro **fatal** derruba o serviço |
| **Mensagem venenosa** | Que não desserializa: logada e **pulada** (o offset é guardado), para não travar a partição |
| **Falha do banco** | Retentada com backoff curto e cancelável (`MaxAttempts`, linha 41). Persistindo, o serviço cai **sem guardar o offset**, e a mensagem é reentregue após o restart. Pular a falha em silêncio perderia dados |
| **Grupo próprio** | `GroupId` com sufixo próprio: não divide partições com os consumers do Silverback |
| **Um escopo por mensagem** | `IServiceScopeFactory.CreateScope()` por mensagem; os handlers (`PlatformEvents/PlatformEventHandlers.cs`) são scoped e **idempotentes** |

> Nada disso foi executado contra um Kafka real: o consumer segue a documentação da Confluent e foi compilado e
> validado no container do host (`ValidateOnBuild`).

### A mudança do .NET 10 no `BackgroundService` e este consumer

([Breaking change](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/10.0/backgroundservice-executeasync-task))
No .NET 10 o `ExecuteAsync` inteiro roda como uma `Task` em background: as partes síncronas dos hosted services
passam a rodar **em paralelo**, e não mais um de cada vez no `StartAsync`. Duas consequências:

1. **Um componente compartilhado entre dois `BackgroundService`, com partida não atômica, vira corrida** (sample 3:
   `PlatformEventsBus`). Aqui há **um único dono** do `IConsumer` e do loop, e os comportamentos viram handlers
   (`TimelineHandler`, `ErrorAlertHandler`), então não existe estado de partida compartilhado.
2. **O loop bloqueante não trava mais o startup, mas continua prendendo uma thread do pool.** Por isso a thread
   dedicada: a mudança do .NET 10 esconde o problema, a thread `LongRunning` o resolve.

Se o componente compartilhado não puder mudar, a correção mínima é mover o registro e a partida para um override de
`StartAsync` (que roda em sequência) e deixar o `ExecuteAsync` só com o trabalho; a alternativa é
`IHostedLifecycleService.StartingAsync`. Outra saída é tornar a partida idempotente e thread-safe (`SemaphoreSlim` ou
`Lazy<Task>`).

## O que ficou de fora, de propósito

Defeitos de **mensageria** do fluxo principal (consumidor não idempotente, retry sem limite, republicação fora do
outbox) e Vertical Slice não fazem parte deste material.
