# 03 · Tempo de vida das dependências

**Branch:** `masterclass/dotnet-producao/03-tempo-de-vida` · **Parte de:** `02-cancellation-token`
**Para ver o que mudou:** `git diff masterclass/dotnet-producao/02-cancellation-token..masterclass/dotnet-producao/03-tempo-de-vida`

## O problema

Em serviços persistentes (API, consumer, worker), as dependências vivem por muito tempo. Um erro de tempo de vida não aparece no primeiro teste: aparece depois de horas, como vazamento de memória, dado velho ou uma exceção que só acontece sob carga.

As dependências são registradas no container padrão do .NET, por caso de uso, em [DependencyInjection.cs](../src/Eximia.Integrations/DependencyInjection.cs). Os problemas estão em **quanto tempo cada coisa vive**.

### 1. Captive dependency na API: o handler que prende o `DbContext`

```csharp
// Usado em toda requisição da API: uma instância só.
services.AddSingleton<RegisterMultipleRoutesIntegrationEventApplication>();
```

O raciocínio parece bom ("é usado em toda requisição, então uma instância só"), mas o handler **depende de serviços scoped**: o `EximiaIntegrationDbContext`, o `IPublisher` do Silverback e o `JsonSchemaService`. O container resolve as dependências de um singleton a partir do escopo raiz, então elas passam a viver o tempo do processo. Na prática, **todas as requisições da API compartilham o mesmo `DbContext`**:

- **Concorrência.** O `DbContext` não é thread-safe. Duas requisições simultâneas usam o mesmo objeto, e o EF Core costuma acusar isso com "A second operation was started on this context instance".
- **Estado misturado.** O *change tracker* e a transação do outbox ([OutboxTransaction](../src/Eximia.Integrations/Outbox/OutboxTransaction.cs)) são de uma requisição, e aqui são de todas. Os eventos de uma requisição podem ser gravados no `SaveChanges` de outra.
- **A economia é falsa.** Criar o handler por requisição é barato. O que custa e tem estado é o `DbContext`, e o `AddDbContextPool` já torna a criação dele barata.

O `PublishPlataformEventService` tem o mesmo defeito em menor escala: é registrado como singleton e recebe o `IPublisher`, que é scoped.

Nada disso quebra um teste com uma requisição por vez. O sintoma aparece sob carga, como exceções intermitentes e dados trocados, que é o pior tipo de defeito para investigar.

### 2. Singleton desnecessário: o serviço que não precisa ser compartilhado

```csharp
// Só lê uma configuração e faz um POST: uma instância basta para todo o processo.
services.AddSingleton<SendIntegrationEventService>();
```

Este caso é diferente dos anteriores: **nada quebra hoje**. O `SendIntegrationEventService` só recebe o `IConfiguration` (que é singleton), lê uma URL no construtor e faz um POST. Não tem estado por operação e não depende de nada scoped. O problema é que **singleton é uma decisão de compartilhar uma instância**, e aqui não há o que compartilhar:

- **Sem ganho.** A construção é barata (uma leitura de configuração) e não há recurso, cache ou semáforo para dividir entre as requisições. Quem justifica um singleton é o `TokenCache` (os semáforos single-flight vivem nele) ou o `NpgsqlDataSource` (é dono do pool). Aqui o singleton não protege nada.
- **Uma armadilha para quem vem depois.** Amanhã alguém precisa do `IntegrationEventsDataAccess` ou do `DbContext` nesse serviço e adiciona ao construtor. Ele vira uma *captive dependency* (problema 1) **sem nenhum aviso**, porque a validação do container está desligada (problema 4).
- **Configuração congelada.** A URL é lida uma única vez e vale até o processo reiniciar. Com o serviço scoped, ela é relida a cada mensagem.

O sinal de alerta é a justificativa: "é só um serviço simples, uma instância basta". Isso descreve o que o objeto **não precisa**, e não o que ele **é**.

### 3. Captive dependency: um `BackgroundService` prendendo o `DbContext`

O `ReprocessStuckEventsWorker` (um `BackgroundService`, ou seja, **singleton**) recebia o `DbContext` diretamente no construtor:

```csharp
public class ReprocessStuckEventsWorker(
    EximiaIntegrationDbContext dbContext,       // pensado para viver uma unidade de trabalho
    RoutingDataAccess routingDataAccess,
    IPublisher publisher,
    ILogger logger) : BackgroundService
```

O `DbContext` foi projetado para uma unidade de trabalho curta. Preso ao singleton, ele vive o tempo do processo, e isso gera três problemas:

- **Vazamento de memória.** O *change tracker* guarda toda entidade carregada. A cada 5 minutos o worker carrega até 100 eventos, e nenhum é esquecido.
- **Dados velhos.** O contexto devolve entidades já rastreadas em vez de reler do banco: um cache involuntário.
- **Exceções de concorrência.** O `DbContext` não é thread-safe, e um único objeto serve a todas as iterações.

O mesmo vale para o `RoutingDataAccess` e para qualquer serviço scoped recebido no construtor de um `BackgroundService`.

### 4. A validação do container desligada

Nos três hosts:

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = false;
    options.ValidateOnBuild = false;
});
```

Por padrão, o container do .NET só valida em **Development**: `ValidateScopes` acusa um singleton que consome um serviço scoped, e `ValidateOnBuild` acusa uma dependência não registrada, ambos na subida. Fora do Development, nada disso é verificado. Desligar nos três hosts deixa os problemas 1 e 3 **passarem em silêncio em todos os ambientes**.

Isso foi conferido: religando a validação na baseline, a API deixa de subir com `Cannot consume scoped service '...TopicNameFactoryPeerEnviroment' from singleton '...RegisterMultipleRoutesIntegrationEventApplication'`, e o agent acusa o worker.

A mesma validação revelou um terceiro defeito, antigo e escondido:

- **O health check do outbox nunca funcionou.** O `OutboxHealthCheckService` do Silverback exige um `IOutboxReader` que nada registra. O `/health-check` da API falharia na primeira chamada de verdade, e sem validação ninguém via.

### 5. Sobras de Web API nos hosts sem HTTP

O dispatcher e o agent registram `HttpContextAccessor`, `IntegrationRequestHttpAdapter`, `IIntegrationRequest` e `EximiaProblemDetailsFactory`: classes de Web API que um consumer e um worker não usam, já que não há `HttpContext` nenhum. Registrar o que ninguém usa engana quem lê (cada linha sugere que algo depende dela) e carrega dependências sem necessidade.

## A solução

### Handler scoped, e a regra para decidir

```csharp
services.AddScoped<RegisterMultipleRoutesIntegrationEventApplication>();
services.AddScoped<PublishPlataformEventService>();
services.AddScoped<SendIntegrationEventService>();   // não havia o que compartilhar
```

A pergunta que decide o tempo de vida não é "quantas vezes esse objeto é usado?", e sim **"esse objeto guarda ou depende de algo de uma operação específica?"**. Se guarda ou depende, não pode ser singleton, por mais que seja usado.

| Tempo de vida | Quando | Exemplos neste projeto |
|---|---|---|
| **Singleton** | Sem estado por operação, thread-safe, caro de criar ou dono de um recurso compartilhado | `TokenCache`, `PostgresConnectionStringFactory` |
| **Scoped** | Uma unidade de trabalho (requisição ou **mensagem**), ou qualquer coisa que dependa dela | `DbContext`, `IPublisher`, acesso a dados, handlers dos casos de uso |
| **Transient** | Barato e sem estado | (não houve necessidade) |

### Escopo por iteração no `BackgroundService`

O worker continua singleton, mas só **agenda**. O trabalho mudou para uma classe scoped, resolvida num escopo criado a cada iteração ([ReprocessStuckEventsWorker.cs](../src/Eximia.Integration.BackgroundAgent/Workers/ReprocessStuckEventsWorker.cs)):

```csharp
public class ReprocessStuckEventsWorker(IServiceScopeFactory scopes, ILogger logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();          // nasce aqui
                await scope.ServiceProvider.GetRequiredService<ReprocessStuckEvents>().RunAsync();
            }                                                               // e é descartado aqui
            catch (Exception ex) { /* ... */ }

            await Task.Delay(TimeSpan.FromMinutes(5));
        }
    }
}

public class ReprocessStuckEvents(EximiaIntegrationDbContext dbContext, RoutingDataAccess routingDataAccess, ...)
```

O `DbContext` agora nasce e morre com a iteração: o *change tracker* não cresce e cada ciclo lê dados frescos. No consumer Kafka o Silverback já cria um escopo por mensagem para os subscribers registrados com `AddScopedSubscriber`.

> O `while (true)` e o `Task.Delay` sem token continuam aqui de propósito: o ciclo de vida do `BackgroundService` é assunto da branch `05-background-service`.

### Validação de container ligada

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;      // captive dependency vira erro na subida
    options.ValidateOnBuild = true;     // dependência não registrada vira erro na subida
});
```

Com isso, um singleton que dependa de um serviço scoped (o handler da API, o worker recebendo o `DbContext`) faz o host **não subir**: o container recusa a combinação, em vez de deixá-la vazar em silêncio. É a diferença entre descobrir o problema ao subir ou às 3 da manhã, e vale em **todos** os ambientes, não só em Development.

A correção do health check está no `AddHealth` da API ([ServicesExtensions.cs](../src/Eximia.Integrations.HttpApi/Infrastructure/ServicesExtensions.cs)): o `IOutboxReader` passa a ser registrado.

### Limpeza do que era sobra

Saíram do dispatcher e do agent o `HttpContextAccessor`, o `IntegrationRequestHttpAdapter`, o `IIntegrationRequest` e o `EximiaProblemDetailsFactory`.

## O que sobrou de service locator, de propósito

- **`DestinationDispatcherFactory`**: o dispatcher é escolhido pelo **nome da classe gravado na rota** (no banco). A resolução em tempo de execução é o ponto de extensão, e por isso todo dispatcher possível precisa estar registrado em `AddIntegrationsRouting`.
- **`IntegrationEventActionsEngine`**: resolve as ações pós-registro por tipo via `IServiceProvider`, e por isso essas ações precisam estar registradas explicitamente.

## Regras para levar

- **Singleton é para o que não guarda nem depende do estado de uma operação**, e não para o que é muito usado.
- **Singleton exige um motivo.** Estado compartilhado, recurso caro ou dono de um recurso. "É simples e basta uma instância" não é motivo: na dúvida, scoped.
- **Singleton não recebe serviço scoped.** Em um serviço de background, crie um escopo por iteração com `IServiceScopeFactory.CreateAsyncScope()`.
- **Ligue `ValidateScopes` e `ValidateOnBuild` em todos os ambientes**, não só em Development. Desligá-los para "calar o erro" esconde o defeito em vez de corrigi-lo.
- **Registre só o que o host usa**, e nada por conveniência: cada registro é uma afirmação de que alguém depende daquilo.

## Próxima branch

`04-shutdown`: o encerramento seguro, e conexões, banco e recursos fechados do jeito certo.
