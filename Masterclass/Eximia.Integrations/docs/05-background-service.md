# 05 · O `BackgroundService` não é um `while(true)`

**Branch:** `masterclass/dotnet-producao/05-background-service` · **Parte de:** `04-shutdown`
**Para ver o que mudou:** `git diff masterclass/dotnet-producao/04-shutdown..masterclass/dotnet-producao/05-background-service`

## Escolhendo o tipo de host

O tipo de projeto diz o que o serviço **é**. Escolha pelo que ele faz:

| Preciso de… | Use | Observação |
|---|---|---|
| Receber requisições HTTP de negócio | **Web API** (`Microsoft.NET.Sdk.Web`) | Kestrel, pipeline HTTP, autenticação |
| Só consumir fila ou rodar tarefas, sem endpoints de negócio | **Worker Service** (`Microsoft.NET.Sdk.Worker`) | `Host.CreateApplicationBuilder`, sem pipeline HTTP |
| Rodar como serviço do Windows | Worker + `AddWindowsService()` | É um Worker com outro "lifetime", não outro tipo de projeto |
| Rodar como serviço Linux (systemd) | Worker + `AddSystemd()` | Idem |
| Probes de saúde do Kubernetes | Web host **mínimo**, só com `/health-*` | Ou, num Worker sem HTTP, um arquivo de *heartbeat* |

Worker Service, Windows Service e systemd **não são três tecnologias**: são o mesmo `BackgroundService` hospedado de formas diferentes.

## O ciclo de vida de um `BackgroundService`

```
StartAsync ──► ExecuteAsync(stoppingToken) roda em segundo plano ──► SIGTERM ──► stoppingToken cancelado
                                                                                 │
                                       StopAsync espera o ExecuteAsync terminar ◄┘ (até o ShutdownTimeout)
```

- O host chama `StartAsync`, que dispara o `ExecuteAsync`.
- No encerramento, o host cancela o `stoppingToken` e o `StopAsync` **espera** o `ExecuteAsync` terminar, até o `ShutdownTimeout` (branch `04`).
- Uma exceção não tratada no `ExecuteAsync` **derruba o host** (padrão do `BackgroundServiceExceptionBehavior` desde o .NET 6).

O `ExecuteAsync` é um método que **precisa terminar quando o token for cancelado**. Um `while(true)` não termina.

## O problema

### 1. O `BackgroundService` escrito como um `while(true)`

O `ReprocessStuckEventsWorker`, depois da branch `03`:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (true)                                                          // ignora o token
    {
        try { /* ... RunAsync() ... */ }
        catch (Exception ex) { logger.Error(ex, "..."); }                 // engole inclusive o cancelamento

        await Task.Delay(TimeSpan.FromMinutes(5));                        // sem token
    }
}
```

- **`while (true)` e `Task.Delay` sem token.** O `StopAsync` espera o loop terminar, e o loop só termina quando o intervalo vence. Um deploy espera até 5 minutos, estoura o `ShutdownTimeout` e o processo leva `SIGKILL` no meio de uma iteração.
- **`catch (Exception)` engole o cancelamento.** Mesmo com o token passado, a `OperationCanceledException` cairia no `catch` e o loop seguiria vivo.
- **Nada recebe o token.** Nem o EF (`ToListAsync()`, `SaveChangesAsync()`), nem as rotas.
- **Sem exclusão entre réplicas.** Se houver duas réplicas do agent, as duas reprocessam os mesmos eventos. O Hangfire resolvia isso com lock no banco. Ao sair dele, a exclusão precisa ser explícita.

### 2. A mudança do .NET 10 no `BackgroundService`

Até o .NET 9, a parte **síncrona** do `ExecuteAsync`, antes do primeiro `await`, rodava na thread principal durante a subida, e **bloqueava a subida dos outros serviços**. Qualquer trabalho de inicialização colocado no começo do método (abrir uma conexão, aquecer um cache, uma chamada síncrona ao banco) segurava o host inteiro, e com o banco lento ou fora do ar o serviço simplesmente não subia.

**No .NET 10 isso mudou** (é uma *breaking change* documentada): todo o `ExecuteAsync` passa a rodar numa thread em segundo plano, e nenhuma parte dele bloqueia a subida dos outros serviços.

Duas consequências práticas:

- **Código que dependia do bloqueio quebra em silêncio.** Se alguém colocava a inicialização no começo do `ExecuteAsync` justamente para que ela terminasse antes de o resto subir, isso deixou de ser verdade. A documentação indica onde colocar esse código agora: no construtor, num override de `StartAsync` (antes de chamar `base.StartAsync`), com `IHostedLifecycleService`, ou implementando `IHostedService` diretamente.
- **O defeito de "travar a subida" some, mas o código síncrono continua errado.** Ele ainda bloqueia uma thread do pool e ainda ignora o cancelamento. O código certo não depende do comportamento de nenhuma das duas versões.

> O worker deste projeto já começa direto no loop, sem trabalho antes do primeiro `await`, então nada nele muda entre o .NET 9 e o .NET 10. A mudança vale como regra para o que você escrever depois.

### 3. O agent era uma casca de Hangfire

O `BackgroundAgent` hospedava um dashboard do Hangfire, com autenticação OpenID Connect e filtros de autorização, para rodar **um único** `BackgroundService` que nem usava o Hangfire. A infraestrutura (Kestrel, MVC, OpenID Connect, storage do Hangfire no Postgres) era maior do que o trabalho.

### 4. O consumer Kafka vestido de Web API

O `EventDispatcher` só consome mensagens, mas era hospedado como Web API completa ([Program.cs](../src/Eximia.Integrations.EventDispatcher/Program.cs)):

```csharp
builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerDoc()          // Swagger num serviço que só consome mensagens
    .AddCustomCors()          // CORS idem
    // ...
    .AddControllers();        // MVC sem nenhum controller

app.MapGet("/Topics", (KafkaConfig config) => Results.Ok(new { config.Connection.GroupId, config.Consumer.Inbounds }));
app.MapControllers();
```

Custos reais: superfície de ataque sem necessidade (o `/Topics` expõe a configuração do consumer), pacotes e tempo de subida a mais, e a confusão sobre o que o serviço de fato é.

## A solução

### O `BackgroundService` correto

[ReprocessStuckEventsWorker.cs](../src/Eximia.Integration.BackgroundAgent/Workers/ReprocessStuckEventsWorker.cs):

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(Interval);

    // WaitForNextTickAsync respeita o token: o loop termina limpo no shutdown, sem esperar o intervalo.
    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ReprocessStuckEvents>().RunAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Uma iteração com falha não derruba o serviço: registra e tenta no próximo ciclo.
            logger.Error(ex, "Error reprocessing stuck integration events.");
        }
    }
}
```

- **`PeriodicTimer`** no lugar de `while (true)` + `Task.Delay`: ele termina no cancelamento e não acumula atraso entre os ciclos.
- **Nenhum trabalho antes do primeiro `await`.** O `ExecuteAsync` começa direto no `PeriodicTimer`, então o comportamento é o mesmo no .NET 9 e no .NET 10.
- **Token em tudo**: EF Core, rotas, lock.
- **Exceção por iteração** não derruba o serviço, mas o cancelamento passa (`when (ex is not OperationCanceledException)`). O oposto, um `catch {}` vazio, deixaria o serviço "vivo" e quebrado.
- **Escopo por iteração**, da branch `03`, continua: o worker só agenda.

### Exclusão entre réplicas

[PostgresAdvisoryLock.cs](../src/Eximia.Integrations/Shared/Postgres/PostgresAdvisoryLock.cs) usa um *advisory lock* do Postgres para que só uma réplica trabalhe por vez:

```csharp
await using var lease = await advisoryLock.TryAcquireAsync(LockName, cancellationToken);
if (lease is null)
    return;                                     // outra réplica já está trabalhando
```

O lock vale para a **sessão**, então a conexão (pedida ao `NpgsqlDataSource` da branch `04`) fica aberta enquanto o trabalho roda. Ele é liberado no `DisposeAsync` **com `CancellationToken.None`**: liberar não pode depender do token que acabou de ser cancelado pelo shutdown. Se o processo morrer sem liberar, o Postgres libera o lock quando a sessão cai.

### Agent: Worker Service puro

[Program.cs](../src/Eximia.Integration.BackgroundAgent/Program.cs) usa `Host.CreateApplicationBuilder`, e o projeto passou para `Microsoft.NET.Sdk.Worker`. Saíram o Hangfire, o dashboard, o OpenID Connect e os filtros. A imagem Docker passou de `dotnet/aspnet` para `dotnet/runtime`, porque o serviço não depende mais do ASP.NET.

> O agent ficou sem endpoint HTTP. Os probes do Kubernetes para ele precisam ser de outro tipo (por exemplo, `exec` verificando um arquivo de *heartbeat* que o worker atualiza a cada ciclo). Isso não foi implementado aqui.

### Dispatcher: host web mínimo

```csharp
// Este serviço CONSOME mensagens: não tem controllers, Swagger nem CORS. O host web mínimo existe só para os
// probes de saúde que o Kubernetes precisa.
var builder = WebApplication.CreateSlimBuilder(args);
// ...
app.MapHealthChecks("/health-ready", ...);
app.MapHealthChecks("/health-check", ...);
app.Run();
```

Saíram o Swagger, o CORS, o MVC, o `/Topics` e os pacotes que só eles usavam (`Swashbuckle`, `Microsoft.Identity.Web`, `JwtBearer`).

Com isso, os três hosts mostram as três formas lado a lado: a **API completa**, o **consumer com host web mínimo** e o **agent como Worker puro**.

## O que continua fora, de propósito

O `ReprocessStuckEventsWorker` publica no Kafka direto, fora da transação que grava o status do evento (*dual write*). É um defeito de **mensageria**, e fica fora deste material.

## Regras para levar

- **Escolha o host pelo que o serviço faz.** Um consumer não precisa de Swagger, CORS nem MVC; um agente de tarefas não precisa de Kestrel.
- **O `ExecuteAsync` precisa terminar quando o token for cancelado.** Use `PeriodicTimer` e repasse o `stoppingToken` a tudo que for assíncrono.
- **Não dependa do que roda antes do primeiro `await`.** Inicialização que precisa terminar antes da subida vai para `StartAsync` ou `IHostedLifecycleService`.
- **Trate falha por iteração, mas deixe o cancelamento passar.**
- **Serviço de background com várias réplicas precisa de exclusão explícita** quando o trabalho não pode rodar em duplicidade.

## Novidades de plataforma

- **.NET 10:** o `BackgroundService` executa todo o `ExecuteAsync` numa thread em segundo plano; nenhuma parte dele bloqueia a subida dos outros serviços ([nota de compatibilidade](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/10.0/backgroundservice-executeasync-task)).
- **.NET 8:** `IHostedLifecycleService` (`StartingAsync`, `StartedAsync`, `StoppingAsync`, `StoppedAsync`) e `HostOptions.ServicesStartConcurrently`/`ServicesStopConcurrently`.
- `BackgroundServiceExceptionBehavior` escolhe entre derrubar o host (padrão) e ignorar uma exceção não tratada do `ExecuteAsync`.

## Próxima branch

`06-tasks-soltas`: `Task.Run` solto, `async void` e exceções que desaparecem.
