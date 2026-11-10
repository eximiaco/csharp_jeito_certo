# 06 · Disparar tarefas e esquecer

**Branch:** `masterclass/dotnet-producao/06-tasks-soltas` · **Parte de:** `05-background-service`
**Para ver o que mudou:** `git diff masterclass/dotnet-producao/05-background-service..masterclass/dotnet-producao/06-tasks-soltas`

## O problema

O registro do evento ([RegisterMultipleRoutesIntegrationEventApplication.cs](../src/Eximia.Integrations/IntegrationEvents/Application/Register/RegisterMultipleRoutesIntegrationEventApplication.cs)) tinha duas tarefas que ninguém aguardava de verdade.

### 1. `Task.Run` solto dentro da transação do outbox

```csharp
await publisher.PublishAsync(RouteIntegrationEventRequestCommand.Create(...));

// Evento da plataforma: roda em paralelo para não atrasar a resposta à origem.
_ = Task.Run(() => publishPlataformEventService.PublishIntegrationEventReceivedAsync(integrationEvent.Value));
```

A intenção era "não atrasar a resposta". O `_ =` descarta a `Task`, e o `Task.Run` empurra o trabalho para outra thread do pool. O efeito real:

- **A exceção some.** O `PublishPlataformEventService` registra o erro e o relança, mas ninguém observa a `Task`: o relançamento não chega a lugar nenhum, nem ao `catch` do método, nem ao tratamento de erro da API. Sobra uma linha de log sem o contexto da requisição. Em versões antigas do .NET uma exceção não observada podia derrubar o processo; hoje ela é descartada em silêncio.
- **O trabalho sobrevive à requisição que o criou.** A API devolve 202 e o escopo da requisição é descartado. Os serviços scoped que a tarefa usa (o `PublishPlataformEventService` e o `IPublisher` do Silverback) podem já ter sido descartados quando ela roda, e a falha aparece como uma `ObjectDisposedException` que, de novo, ninguém vê.
- **Uso concorrente de objetos que não são thread-safe.** O `IPublisher` que a tarefa usa é o **mesmo** que está enlistado na transação do outbox e que o loop continua usando na thread original. São duas threads usando o mesmo objeto ao mesmo tempo.
- **O trabalho não participa do ciclo de vida.** No shutdown (branch `04`), o host espera as requisições em andamento, mas não sabe que essa tarefa existe. Ela é interrompida no meio, sem rastro.
- **Não economiza nada.** Publicar um evento da plataforma leva milissegundos. O `Task.Run` adiciona uma troca de thread e esconde o custo, não o elimina.

### 2. `Parallel.ForEach` com lambda assíncrona: `async void`

```csharp
Parallel.ForEach(processedEvents, async integrationEvent =>
{
    await integrationEventActionsEngine.Execute(integrationEvent, cancellationToken);
});
```

`Parallel.ForEach` espera um `Action<T>`. Uma lambda `async` passada ali compila como **`async void`**. As consequências:

- **O `ForEach` retorna antes do trabalho terminar.** Ele só espera o código *até o primeiro `await`*. O método segue adiante, responde 202 e as ações pós-registro ainda estão rodando, ou nem começaram.
- **Exceções derrubam o processo.** Num `async void` não há `Task` para observar a falha: a exceção é relançada no pool de threads, e uma exceção não tratada ali **encerra o processo**. Uma falha numa ação pós-registro derruba a API inteira, com todas as requisições em andamento.
- **Sem limite de paralelismo**, e com o mesmo escopo e o mesmo `DbContext` compartilhados entre as iterações.

## A solução

```csharp
await publisher.PublishAsync(RouteIntegrationEventRequestCommand.Create(...));
await publishPlataformEventService.PublishIntegrationEventReceivedAsync(integrationEvent.Value);

// ...

foreach (var integrationEvent in processedEvents)
    await integrationEventActionsEngine.Execute(integrationEvent, cancellationToken);
```

- **Aguardar de verdade.** Falhas voltam para o chamador e viram erro tratado, não silêncio, e o trabalho termina dentro do escopo da requisição.
- **Sequencial de propósito.** O loop usa um `DbContext` e um escopo compartilhados. `DbContext` **não é thread-safe**: paralelizar ali seria trocar um erro por outro. A regra é: se as iterações são independentes, não compartilham estado mutável e há um limite claro, use `Parallel.ForEachAsync` com `MaxDegreeOfParallelism` e o `CancellationToken`. Se não, mantenha sequencial.

### E quando eu realmente não quero esperar?

Existem tarefas que podem ser feitas depois da resposta. Mesmo assim, "soltar" a `Task` não é o caminho: alguém precisa ser o dono dela. As opções sérias:

| Situação | O que usar |
|---|---|
| Perder a tarefa num crash é **aceitável** | Fila em memória limitada (`Channel<T>` com `BoundedChannelOptions`) consumida por um `BackgroundService` com o ciclo de vida da branch `05`: trata as exceções, respeita o cancelamento e tem o próprio escopo por item |
| Perder a tarefa **não** é aceitável | Outbox: gravar a intenção na mesma transação do negócio e publicar depois, como o projeto já faz com o `RouteIntegrationEventRequestCommand` |

Note que o projeto já faz uma escolha consciente em outro lugar: o producer de `plataform-event` no dispatcher usa `DisableDeliveryReports().IgnoreIfNotAcknowledged()`, que é "fire-and-forget" **declarado e configurado** na biblioteca de mensageria, com a semântica explícita, e não um `_ =` perdido no meio do código.

## Regras para levar

- **Nunca descarte uma `Task`** com `_ =` sem uma razão que caiba em um comentário. Se a razão é "não quero esperar", provavelmente você quer uma fila com dono.
- **`Task.Run` não é "rodar em background".** Ele só muda de thread: o trabalho continua sem dono, sem escopo e sem ciclo de vida.
- **Nunca use `async void`**, exceto em manipuladores de evento. Lambda `async` em `Parallel.ForEach` ou em `List.ForEach` vira `async void`.
- **`Parallel.ForEachAsync`** (.NET 6+) é a versão que espera de verdade, com limite de paralelismo e token.
- **`DbContext` não é thread-safe**: uma instância por unidade de trabalho, sem uso concorrente.

## Novidades de plataforma

- `Task.WhenEach` (.NET 9) entrega as tarefas na ordem em que terminam, útil para processar resultados assim que ficam prontos em vez de esperar todos.

## Recapitulando a masterclass

| Branch | O que corrigiu |
|---|---|
| `01-sync-over-async` | Threads bloqueadas esperando I/O; cache de token que nunca funcionou |
| `02-cancellation-token` | O token que parava antes do banco, do HTTP e das esperas do retry |
| `03-tempo-de-vida` | `DbContext` preso no worker e container sem validação |
| `04-shutdown` | Cancelamento tratado como falha, e recursos sem dono no encerramento |
| `05-background-service` | O `while(true)`, a mudança do .NET 10 e o host errado |
| `06-tasks-soltas` | `Task.Run` solto, `async void` e exceções que desaparecem |
