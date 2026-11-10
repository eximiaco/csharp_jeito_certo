# 04 · O shutdown não se resolve sozinho

**Branch:** `masterclass/dotnet-producao/04-shutdown` · **Parte de:** `03-tempo-de-vida`
**Para ver o que mudou:** `git diff masterclass/dotnet-producao/03-tempo-de-vida..masterclass/dotnet-producao/04-shutdown`

## O que acontece num deploy

Num deploy, o Kubernetes envia `SIGTERM` ao processo. O host .NET dispara o `stoppingToken`, para os serviços e começa a contar o **`ShutdownTimeout`** (padrão: 30 segundos). Depois que os serviços param, o host **descarta o container**, e é nesse momento que os singletons fecham o que possuem: pools de conexão e clientes de rede. Se o processo não sair a tempo, recebe `SIGKILL` e nada disso acontece.

Um encerramento correto tem quatro etapas:

1. **Parar de aceitar trabalho novo.**
2. **Terminar ou devolver** o que está em andamento, dentro do prazo.
3. **Fechar recursos** (conexões TCP, banco, arquivos) na ordem certa, e por quem é dono deles.
4. **Sair dentro do prazo.**

Esta branch trata dos dois lados: como o código **interpreta** o cancelamento (etapa 2), e como os recursos são **fechados** (etapa 3).

## O problema, parte 1: o deploy que gera erro falso

Imagine um evento sendo entregue a um destino que leva 20 segundos para responder, e um deploy do dispatcher exatamente nesse momento. Desde a branch `02-cancellation-token`, o token chega até a chamada HTTP, então o cancelamento de fato interrompe a entrega. A pergunta passa a ser: **o que o código faz com esse cancelamento?**

### 1. O cancelamento virava "falha crítica"

[RouteIntegrationEventApplication.cs](../src/Eximia.Integrations/IntegrationEvents/Application/Routing/RouteIntegrationEventApplication.cs) fechava com um `catch` genérico:

```csharp
catch (Exception ex)
{
    routeIntegrationEventTelemetry.CriticalFail(ex, command);
    throw new FailureDispatcherException(string.Empty, string.Empty, 0, ex.Message, ex);
}
```

Esse `catch` captura **também** a `OperationCanceledException`. Um shutdown virava "falha crítica de entrega" nos logs e na telemetria, e a exceção original ficava escondida dentro de outra.

### 2. O fallback do Polly transformava o cancelamento em resultado de negócio

[PolicyFactory.CreateFallbackPolicy](../src/Eximia.Integrations/Routing/Dispatchers/PolicyFactory.cs) usava `Handle<Exception>()`, que é amplo demais:

```csharp
Policy<DispatcherResult>
    .Handle<Exception>()
    .FallbackAsync(fallbackAction: async (result, context, token) => { ... DispatcherResult.Failure(...) })
```

Se o cancelamento durante a chamada HTTP chegar até a policy como `OperationCanceledException` (ou embrulhado pelo cliente HTTP), o fallback o **converte em `DispatcherResult.Failure`**. Daí o `HandleRoutingErrorService` grava o evento como `Error`, publica `IntegrationEventError` e **envia um callback de erro para a origem**: ela é avisada de uma falha que nunca aconteceu. Depois o offset é confirmado, e o evento fica com um erro falso, permanente.

> **Atenção, honestidade sobre este ponto.** O item 1 é certo, porque está no código. Este item 2 é uma hipótese lida no código, e **não foi observada rodando**: a forma exata como o Flurl 4 e o Polly propagam o cancelamento no meio da chamada HTTP precisa ser confirmada num teste. A correção abaixo cobre os dois caminhos possíveis.

### 3. Nenhum prazo foi configurado

Nenhum host configurava `ShutdownTimeout`. Com o padrão de 30 segundos, o dispatcher pode não ter tempo de terminar o que está em andamento, gravar o resultado (o token próprio de 10 segundos da branch `02`) e desconectar do Kafka. Se o prazo estoura, o processo recebe `SIGKILL` com o evento ainda em `Processing`, e ninguém registra o que houve.

## O problema, parte 2: recursos fechados por ninguém, ou pelo dono errado

### 4. Banco: um pool de conexões sem dono

Todo acesso a dados abria a conexão assim:

```csharp
await using var connection = new NpgsqlConnection(connectionStringFactory.CreateConnectionString().ConnectionString);
```

Abrir uma conexão curta por operação **é** o padrão certo: o Npgsql guarda as conexões físicas num pool. O problema é **de quem é esse pool**. Com `new NpgsqlConnection(connectionString)`, o pool é global e estático, indexado pela *connection string*, e **ninguém o descarta**. No encerramento, as conexões TCP com o Postgres não são fechadas pelo protocolo: elas morrem junto com o processo. O Postgres registra o fim abrupto no log (algo como `unexpected EOF on client connection`), e uma transação que estivesse aberta naquela conexão só é desfeita quando o servidor percebe a queda.

Além disso, o EF e o Dapper montavam a *connection string* cada um por conta própria. Nada no código dizia que os dois compartilhavam um recurso.

## A solução

### Cancelamento sobe como cancelamento

```csharp
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    // SHUTDOWN: cancelamento não é falha de entrega. Relança como está: o consumer não confirma o
    // offset, o evento é reentregue depois do restart e nenhum callback de erro sai.
    throw;
}
catch (Exception ex)
{
    // falha real
}
```

O filtro `when (cancellationToken.IsCancellationRequested)` distingue as duas causas possíveis de uma `OperationCanceledException`: o **shutdown** (o token do host foi cancelado, relança) e um **timeout** de uma chamada (o token do host não foi cancelado, segue como falha).

### O fallback não trata cancelamento como falha

```csharp
Policy<DispatcherResult>
    .Handle<Exception>(ex => ex is not OperationCanceledException)
    .FallbackAsync(
        fallbackAction: async (result, context, token) =>
        {
            // O cliente HTTP pode embrulhar o cancelamento em outra exceção: confira o token.
            token.ThrowIfCancellationRequested();
            // ...
        })
```

Duas defesas: a exceção `OperationCanceledException` é excluída do `Handle`, e o `ThrowIfCancellationRequested` cobre o caso de o cancelamento chegar embrulhado em outra exceção.

### Prazo explícito, alinhado com o orquestrador

```csharp
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(45));
```

| Host | `ShutdownTimeout` | Por quê |
|---|---|---|
| EventDispatcher | 45 s | A entrega em andamento, a gravação do resultado e a desconexão do Kafka precisam caber |
| HttpApi | 30 s | Requisições em andamento e o `OutboxWorker` terminam dentro dele |
| BackgroundAgent | 30 s | Uma iteração em andamento precisa caber aqui |

No Kubernetes, o `terminationGracePeriodSeconds` do pod precisa ser **maior** que o `ShutdownTimeout`: se for menor, o orquestrador mata o processo antes de o host terminar de parar os serviços e de descartar o container. Use também um `preStop` para tirar o pod do balanceamento antes do `SIGTERM`.

Outro detalhe de infraestrutura: o `ENTRYPOINT` do Dockerfile precisa estar em **forma exec** (`["dotnet", "App.dll"]`). Em forma *shell*, o `SIGTERM` vai para um shell intermediário e não chega ao processo .NET.

### Banco: um `NpgsqlDataSource` com dono

Em [DependencyInjection.cs](../src/Eximia.Integrations/DependencyInjection.cs):

```csharp
// DONO do pool de conexões com o Postgres. Por estar no container, ele é descartado no encerramento do
// host, e as conexões ociosas são fechadas pelo protocolo do Postgres, e não cortadas quando o processo morre.
services.AddSingleton(sp => NpgsqlDataSource.Create(
    sp.GetRequiredService<PostgresConnectionStringFactory>().CreateConnectionString()));
```

O acesso a dados recebe o `NpgsqlDataSource` e pede a conexão a ele:

```csharp
await using var connection = dataSource.CreateConnection();
```

E o EF usa o mesmo data source, e portanto o mesmo pool:

```csharp
services.AddDbContextPool<EximiaIntegrationDbContext>((sp, options) =>
    options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>()));
```

Agora o pool tem um dono (o container), um tempo de vida (o do host) e um momento de fechamento (o descarte do container, depois que os serviços pararam). O `NpgsqlDataSource` é a forma recomendada pelo Npgsql desde a versão 7.

> O outbox do Silverback e o storage do Hangfire ainda recebem a *connection string* e usam o pool global do Npgsql. São bibliotecas de terceiros com a própria configuração, e ficaram como estão.

## Conexões TCP e arquivos que você mesmo abre

Neste projeto, as conexões TCP de longa duração (Postgres, Kafka, HTTP) pertencem a bibliotecas que têm o próprio `Dispose`. O trabalho do código da aplicação é **registrá-las no container e deixar o container fechá-las**, como foi feito acima. O Silverback, por exemplo, desconecta do Kafka quando o host para: os consumers deixam de buscar mensagens e saem do grupo, e os producers enviam o que estava na fila. Isso só acontece se o processo tiver tempo para isso, o que é mais um motivo para o `ShutdownTimeout` explícito.

Quando é o seu código que abre o recurso (`TcpClient`, `FileStream`, um socket próprio):

- **`await using` / `DisposeAsync`**, e não `Dispose` síncrono de um recurso assíncrono. Um `FileStream` com buffer só garante o conteúdo no disco depois do `FlushAsync`/`DisposeAsync`. Um arquivo fechado no meio fica **truncado**.
- **Escreva num arquivo temporário e renomeie no fim.** Um arquivo que outro sistema vai ler nunca deve existir pela metade com o nome final.
- **TCP:** `Shutdown(SocketShutdown.Send)` antes de fechar envia um `FIN` gracioso e deixa o outro lado ler tudo. Fechar de uma vez pode mandar um `RST`, e os dados em trânsito se perdem.
- **O fechamento não pode depender do token já cancelado.** Se o `FlushAsync` receber o `stoppingToken`, ele nem tenta: use um prazo próprio e curto, como o `persist` da branch `02`.
- **`Environment.Exit` no handler de `SIGTERM`** interrompe tudo no meio, sem fechar nada.

## Regras para levar

- **Cancelamento não é falha.** Nunca o converta em resultado de negócio, evento de erro ou callback.
- **`catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }`** antes de qualquer `catch (Exception)` genérico.
- **`Handle<Exception>()` em Polly (e `catch (Exception)` em geral) é amplo demais:** exclua o cancelamento.
- **Todo recurso de longa duração precisa de um dono** que o feche. Em .NET, o dono natural é o container: registre o recurso como singleton e deixe o container descartá-lo.
- **Quem recebe uma dependência por injeção não a descarta.** O container a descarta uma única vez, no fim.
- **`Dispose` não pode lançar:** uma exceção no descarte do container aparece como "Program terminated unexpectedly" e transforma um deploy normal em um crash.
- **Configure o `ShutdownTimeout` de propósito** e o alinhe com o `terminationGracePeriodSeconds`.

## Novidades de plataforma

- `IHostedLifecycleService` (.NET 8): separa "parar de aceitar" (`StoppingAsync`) de "encerrar" (`StoppedAsync`).
- `HostOptions.ServicesStopConcurrently` (.NET 8) para parar serviços em paralelo, quando o encerramento de um não depende de outro.

## Próxima branch

`05-background-service`: o `BackgroundService` que é um `while(true)`, e o host certo para cada serviço.
