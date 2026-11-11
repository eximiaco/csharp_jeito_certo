# Sample 3 · Tarefas soltas, BackgroundService e a mudança do .NET 10

**Temas:** 3️⃣ Disparar tasks e esquecer (`Task.Run` solto, `async void` e exceções que desaparecem) ·
4️⃣ Tratar o `BackgroundService` como um `while(true)` (ciclo de vida, escolha do host, exemplos reais de tarefas em
background e o que muda no .NET 10).

Este sample é o sistema do sample 4 com **só os problemas 3 e 4** plantados, mais um consumer Kafka (client puro da
Confluent) escrito com **problemas de thread**, para falar de *thread starvation*. Tempo de vida das dependências,
encerramento, token e código assíncrono do fluxo principal estão corretos.

Para compilar: `dotnet build Eximia.Integrations.slnx`. Nada aqui precisa ser executado: a leitura do código basta.

## Problema 3 · Tarefas disparadas e esquecidas

**`Task.Run` solto no registro do evento** (`Eximia.Integrations/IntegrationEvents/Application/Register/RegisterMultipleRoutesIntegrationEventApplication.cs`)
- Linha 100: `_ = Task.Run(() => publishPlataformEventService.PublishIntegrationEventReceivedAsync(...))`.
- **Por quê:** o comentário diz "roda em paralelo para não atrasar a resposta", mas a task não é aguardada nem
  observada. (1) Se ela falhar, a exceção some: ninguém a vê, e o evento da plataforma se perde sem rastro.
  (2) Se o app parar, o trabalho some junto. (3) Ela usa o `IPublisher`, que é scoped e vive no escopo da requisição:
  quando a resposta é enviada e o escopo é descartado, a task pode usar um objeto já descartado. (4) Não há limite:
  sob carga, cada requisição joga mais uma task no pool.

**`Parallel.ForEach` com lambda `async`** (mesmo arquivo)
- Linha 110: `Parallel.ForEach(processedEvents, async integrationEvent => await ...Execute(...))`.
- **Por quê:** `Parallel.ForEach` recebe um `Action`, então o lambda `async` vira **`async void`**. O
  `Parallel.ForEach` termina assim que cada lambda atinge o primeiro `await`, **sem esperar o trabalho**. A resposta
  `Success` volta antes das ações pós-registro terminarem; se uma delas lançar, a exceção de um `async void` vai para
  o contexto de sincronização e, no ASP.NET Core, **derruba o processo**. E o `DbContext` e o `IPublisher` do escopo
  são usados por várias tasks ao mesmo tempo, sem ser thread-safe.

**No consumer Confluent (ver o bloco de threads abaixo):** uma `Task.Run` solta por mensagem
(`Workers/PlatformEventsBus.cs`, linha 58), sem limite, sem observar a exceção e com o offset já confirmado.

## Problema 4 · BackgroundService e host

**Worker escrito como `while(true)`** (`Eximia.Integration.BackgroundAgent/Workers/ReprocessStuckEventsWorker.cs`)
- Linha 21: `while (true)` que ignora o token.
- Linhas 57 e 79: `ToListAsync()` e `SaveChangesAsync()` sem o token.
- Linha 35: `Task.Delay` sem token.
- Sem exclusão entre réplicas.
- **Por quê:**
  - O `StopAsync` espera o `ExecuteAsync` terminar, e esse loop só termina quando o intervalo de 5 minutos vence. Um
    deploy espera até 5 minutos, estoura o `ShutdownTimeout` e o processo leva `SIGKILL` no meio de uma iteração.
  - O `catch (Exception)` do loop (linha 29) engole também a `OperationCanceledException`, então mesmo com token o serviço
    seguiria vivo.
  - Sem token, o EF não pode ser interrompido.
  - Com duas réplicas do agent, as duas reprocessam os mesmos eventos. O Hangfire dava essa exclusão com lock no
    banco, e ao sair dele ela precisa ser explícita.

**Agent como casca de Hangfire**
- `Eximia.Integration.BackgroundAgent/Eximia.Integration.BackgroundAgent.csproj`: linha 1 (SDK `Microsoft.NET.Sdk.Web`).
- `Eximia.Integration.BackgroundAgent/Program.cs`: linha 34 (`AddHangfireJobs`) e linha 67 (`UseHangfireDashboard`).
- `Eximia.Integration.BackgroundAgent/Infrastructure/JobsConfiguration.cs`: linha 7, um `BackgroundService` que não faz nada.
- **Por quê:** o serviço não recebe HTTP, mas carrega Kestrel, MVC, Hangfire e um dashboard. A superfície de ataque e o
  custo de manter crescem sem ganho: os dois jobs reais viram dois `BackgroundService` simples.

**Dispatcher como Web API completa** (`Eximia.Integrations.EventDispatcher`)
- `Eximia.Integrations.EventDispatcher/Eximia.Integrations.EventDispatcher.csproj`: linha 1 (SDK Web).
- `Eximia.Integrations.EventDispatcher/Program.cs`: `CreateBuilder` (linha 9), `AddSwaggerDoc` (linha 29), `AddControllers` (linha 46) e `MapGet("/Topics")` (linha 73).
- **Por quê:** o dispatcher só **consome** mensagens. Swagger, CORS e MVC sem nenhum controller são superfície inútil,
  e o `/Topics` expõe configuração. O único HTTP de que ele precisa são os probes de saúde do Kubernetes.

## O consumer Kafka com a lib da Confluent, com problemas de thread

Para ter um exemplo real de tarefa em background, o agent consome o tópico de eventos da plataforma (o que a API e o
dispatcher já publicam) e grava uma linha do tempo por integração (`integration_event_timeline`, ver
`sql/integration_event_timeline.sql`). Aqui ele foi escrito do jeito que costuma aparecer em código de produção.

O consumer fica em `Workers/PlatformEventsBus.cs`, compartilhado por dois `BackgroundService`:
`IntegrationTimelineWorker` (grava a linha do tempo) e `IntegrationErrorAlertWorker` (alerta quando uma integração falha).

**Cenário para a demonstração:** depois de um deploy, o consumer tem *lag* e recebe uma rajada de mensagens.

| Problema | Onde | Por que é um problema |
|---|---|---|
| Loop bloqueante numa thread do pool | `PlatformEventsBus.cs`, `Task.Run(ConsumeLoop)` (linha 45) e `Consume()` (linha 53) | `Consume()` bloqueia. O `while (true)` **prende uma thread do ThreadPool para sempre**, sem token. É uma thread a menos para todo o processo |
| Uma `Task.Run` solta por mensagem | linha 58 | Sem limite: numa rajada, milhares de work items entram na fila do pool. Exceções se perdem. A ordem dentro da partição deixa de existir |
| Sync-over-async dentro de cada work item | `PlatformEventsBus.cs` (linha 72) e `IntegrationTimelineWorker.cs` (linha 30) | Cada work item **bloqueia** uma thread do pool esperando o banco |
| `Thread.Sleep` no retry | `IntegrationTimelineWorker.cs`, linha 38 | Bloqueia a thread do pool por até 2·n segundos, por mensagem |
| Commit antes de processar | `PlatformEventsBus.cs`, `ConsumerConfig` sem `EnableAutoOffsetStore = false` | O offset é confirmado em segundo plano enquanto a `Task.Run` ainda nem rodou: um crash ou deploy **perde mensagens** |
| Sem `Close()` e sem handlers de erro | `PlatformEventsBus.cs` | O rebalance espera o `session.timeout`, e os erros do client somem |

**Por que isso vira thread starvation no host inteiro.** O pool cria threads aos poucos (*hill climbing*, cerca de
uma a cada 500 ms), e a fila de work items cresce mais rápido do que as threads aparecem. Cada work item bloqueia a
sua thread (banco e `Thread.Sleep`), então o pool nunca esvazia. Passam a esperar thread livre o
`ReprocessStuckEventsWorker`, os timers, os callbacks do Npgsql e o próprio Serilog. CPU baixa, latência alta, e o
processo parece travado.

**Como observar** (quando for possível executar): `dotnet-counters monitor --process-id <pid> System.Runtime` e
acompanhar `threadpool-queue-length` e `threadpool-thread-count` durante a rajada. Compare com o sample 4, onde a
thread dedicada e o processamento sequencial por mensagem mantêm o pool estável.

**Ligação com o sample 1:** o sample 1 trata o sync-over-async no fluxo HTTP e no consumer do Silverback. Aqui o
mesmo starvation nasce de **tarefas soltas mais um `BackgroundService` mal escrito**.

## A mudança do .NET 10: a corrida no startup

([Breaking change: `BackgroundService` runs all of `ExecuteAsync` as a `Task`](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/10.0/backgroundservice-executeasync-task))

- **Até o .NET 8:** a parte síncrona do `ExecuteAsync`, antes do primeiro `await`, rodava dentro do `StartAsync`.
  Os hosted services subiam **um de cada vez**.
- **No .NET 10:** o `ExecuteAsync` inteiro roda como uma `Task` em background, então essas partes síncronas rodam
  **em paralelo**.

**Onde isso morde aqui.** `IntegrationTimelineWorker` e `IntegrationErrorAlertWorker` fazem a mesma coisa na parte
síncrona do `ExecuteAsync`: `bus.Subscribe(...)` e `bus.StartReceive()`
(`IntegrationTimelineWorker.cs`, linha 16; `IntegrationErrorAlertWorker.cs`, linha 15).

O `StartReceive` (linha 20) tem uma guarda que **não é atômica**: `if (_started) return;` e, mais abaixo,
`_started = true`, com a criação do consumer no meio. No .NET 8 isso funcionava por acaso, porque as chamadas eram
sequenciais: o segundo serviço via o bus já ligado e só pegava carona. No .NET 10 as duas chamadas correm juntas:

- as duas passam pelo `if`, e o resultado é **intermitente**: a exceção "already running" na subida (linha 30), ou
  **dois consumers** no mesmo grupo e o primeiro vazado;
- o erro aparece só no start, dependendo do timing, então um serviço pode ter o problema sem nunca ter falhado antes.

**O outro lado da mesma mudança.** O loop bloqueante de `Consume()` antes do primeiro `await` **travaria o startup**
no .NET 8, porque rodaria no `StartAsync`. No .NET 10 não trava mais: a mudança corrige um defeito e **expõe** outro.
Mas a thread do pool continua presa, e a mudança só esconde o sintoma.

**Correção** (no sample 4): um único dono do `IConsumer` e do loop, handlers separados, e nenhum estado de partida
compartilhado entre serviços. A correção mínima, quando o componente compartilhado não pode mudar, é mover o registro
e a partida para um override de `StartAsync` (que roda em sequência) e deixar o `ExecuteAsync` só com o trabalho.

## Como ver só o que mudou

```
diff -rq sample03/src sample04/src -x bin -x obj
```

Os arquivos abaixo são os que diferem do sample 4. Tudo que não está na lista é idêntico: o sample foi montado a
partir do sample 4 com **só os problemas deste tema** plantados.

- `Eximia.Integration.BackgroundAgent/Dockerfile`
- `Eximia.Integration.BackgroundAgent/Eximia.Integration.BackgroundAgent.csproj`
- `Eximia.Integration.BackgroundAgent/Infrastructure/HangfireFilters  (só neste sample)`
- `Eximia.Integration.BackgroundAgent/Infrastructure/HttpGlobalExceptionFilter.cs  (só neste sample)`
- `Eximia.Integration.BackgroundAgent/Infrastructure/JobsConfiguration.cs  (só neste sample)`
- `Eximia.Integration.BackgroundAgent/Infrastructure/ServicesExtensions.cs`
- `Eximia.Integration.BackgroundAgent/PlatformEvents  (só no sample 4)`
- `Eximia.Integration.BackgroundAgent/Program.cs`
- `Eximia.Integration.BackgroundAgent/Properties/launchSettings.json`
- `Eximia.Integration.BackgroundAgent/Workers/IntegrationErrorAlertWorker.cs  (só neste sample)`
- `Eximia.Integration.BackgroundAgent/Workers/IntegrationTimelineWorker.cs  (só neste sample)`
- `Eximia.Integration.BackgroundAgent/Workers/PlatformEventsBus.cs  (só neste sample)`
- `Eximia.Integration.BackgroundAgent/Workers/PlatformEventsConsumer.cs  (só no sample 4)`
- `Eximia.Integration.BackgroundAgent/Workers/ReprocessStuckEventsWorker.cs`
- `Eximia.Integrations.EventDispatcher/Eximia.Integrations.EventDispatcher.csproj`
- `Eximia.Integrations.EventDispatcher/Infrastructure/ServicesExtensions.cs`
- `Eximia.Integrations.EventDispatcher/Program.cs`
- `Eximia.Integrations/DependencyInjection.cs`
- `Eximia.Integrations/IntegrationEvents/Application/Register/RegisterMultipleRoutesIntegrationEventApplication.cs`
- `Eximia.Integrations/PlatformEvents/PlatformEventTimelineDataAccess.cs`
- `Eximia.Integrations/Shared/Postgres/PostgresAdvisoryLock.cs  (só no sample 4)`
