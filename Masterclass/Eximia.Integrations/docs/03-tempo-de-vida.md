# 03 · Tempo de vida das dependências

**Branch:** `masterclass/dotnet-producao/03-tempo-de-vida` · **Parte de:** `02-cancellation-token`
**Para ver o que mudou:** `git diff masterclass/dotnet-producao/02-cancellation-token..masterclass/dotnet-producao/03-tempo-de-vida`

## O problema

Em serviços persistentes (API, consumer, worker), as dependências vivem por muito tempo. Um erro de tempo de vida não aparece no primeiro teste: aparece depois de horas, como vazamento de memória, dado velho, telemetria misturada ou uma exceção que só acontece sob carga.

### 1. Singleton por conveniência: a telemetria que guardava o span da mensagem

No módulo do Autofac do dispatcher:

```csharp
// A telemetria de roteamento é usada em TODA mensagem consumida: uma instância só, em vez de uma por mensagem.
builder.RegisterType<RouteIntegrationEventOtelTelemetry>().As<IRouteIntegrationEventTelemetry>().SingleInstance();
```

O raciocínio parece bom ("evita alocar um objeto por mensagem"), mas a classe **tem estado por mensagem**. O span é criado no construtor e encerrado no `Dispose`:

```csharp
public sealed class RouteIntegrationEventOtelTelemetry(ITelemetryFactory telemetryFactory, ...)
{
    private IOtelTelemetry? _activity = telemetryFactory.Create("RouteIntegrationEventApplication.ConsumeMessage");
    // ... cada método acrescenta tags e eventos ao _activity
    public void Dispose() => _activity?.Dispose();
}
```

Como singleton:

- **Um span para o processo inteiro.** O span nasce na primeira mensagem e só termina quando o container é descartado, no shutdown. Todo evento consumido escreve no mesmo span.
- **Tags de mensagens diferentes se sobrescrevem.** O consumer processa até 10 mensagens em paralelo: o `IntegrationEventId`, o destino e o erro de uma aparecem no rastro de outra.
- **Um erro marca todas.** `SetError` numa mensagem marca o span compartilhado.
- **Captive dependency escondida.** A classe recebe `ITelemetryFactory`, que é scoped. O singleton prende a instância do primeiro escopo para sempre.

Nada disso quebra um teste. O sintoma é um rastro em produção que não faz sentido, exatamente quando você precisa dele para investigar um incidente.

### 2. Captive dependency: um `BackgroundService` prendendo o `DbContext`

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

### 3. Tempo de vida inconsistente para o mesmo tipo

`OtelTracingService` era **singleton** no dispatcher e **scoped** na API e no agent, com o mesmo código. Ele é dono de um `ActivitySource`, o que o faz um singleton por natureza: criar um `ActivitySource` por requisição é caro e não tem sentido.

### 4. Autofac com varredura de assembly e sem validação

Os três hosts usavam `RegisterAssemblyTypes(...).AsClosedTypesOf(typeof(IService<>))`: toda classe que implementa o marcador `IService<T>` era registrada automaticamente, com `InstancePerLifetimeScope`. Dois efeitos:

- **Ninguém lê o que está registrado**, nem com qual tempo de vida. O marcador `IService<T>` é cerimônia que só serve à varredura.
- **O container não avisa de erros de escopo.** O Autofac não detecta *captive dependency*: os problemas 1 e 2 sobem sem nenhum aviso. Uma dependência não registrada também só falha no primeiro uso.

## A solução

### Escopo por iteração no `BackgroundService`

O worker continua singleton, mas só **agenda**. O trabalho mudou para uma classe scoped, resolvida num escopo criado a cada iteração ([ReprocessStuckEventsWorker.cs](../src/Eximia.Integration.BackgroundAgent/Workers/ReprocessStuckEventsWorker.cs)):

```csharp
public class ReprocessStuckEventsWorker(IServiceScopeFactory scopes, ILogger logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ...
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

### Registro explícito, com o tempo de vida certo

[DependencyInjection.cs](../src/Eximia.Integrations/DependencyInjection.cs) lista o que existe, com o tempo de vida e o motivo. A telemetria de roteamento volta a ser scoped: **um span por mensagem**, encerrado quando o escopo da mensagem é descartado.

| Tempo de vida | Quando | Exemplos neste projeto |
|---|---|---|
| **Singleton** | Sem estado por mensagem, thread-safe, caro de criar ou dono de um recurso compartilhado | `TokenCache`, `PlatformMetrics`, `OutboxMetrics`, `DatadogMetricsService`, `OtelTracingService`, `PostgresConnectionStringFactory` |
| **Scoped** | Uma unidade de trabalho (requisição ou **mensagem**), ou qualquer coisa que guarde estado dela | `DbContext`, acesso a dados, telemetria por mensagem, handlers dos casos de uso |
| **Transient** | Barato e sem estado | (não houve necessidade) |

A pergunta que decide não é "quantas vezes esse objeto é usado?", e sim **"esse objeto guarda algo de uma operação específica?"**. Se guarda, não pode ser singleton, por mais que seja usado.

Saíram o Autofac, o marcador `IService<T>` e o `Ambient` que servia à varredura. Cada host registra só o que usa:

- **API:** `AddIntegrationsShared()` + `AddIntegrationsRouting()` + `AddRegisterIntegrationEvent()`.
- **Dispatcher:** os três acima + `AddIntegrationsAuth()` + `AddRouteIntegrationEvent()` + `AddProcessIntegrationEventCallback()`.
- **Agent:** `AddIntegrationsShared()` + o acesso a rotas e a unidade de trabalho do worker.

### Validação de container ligada

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;      // captive dependency vira erro na subida
    options.ValidateOnBuild = true;     // dependência não registrada vira erro na subida
});
```

Com isso, registrar de novo a telemetria como singleton faria o host **não subir**: o container recusa um singleton que depende de um serviço scoped (`ITelemetryFactory`).

### O que a validação revelou

- **O health check do outbox nunca funcionou.** Ao ligar `ValidateOnBuild`, a API parou de subir: o health check do Silverback (`OutboxHealthCheckService`) exige um `IOutboxReader` que nada registrava. O `/health-check` da API falharia na primeira chamada de verdade, e o Autofac escondia isso. A correção está no `AddHealth` da API ([ServicesExtensions.cs](../src/Eximia.Integrations.HttpApi/Infrastructure/ServicesExtensions.cs)).
- **A validação não enxerga dentro de *factories*.** O leitor do outbox é criado por uma lambda singleton que resolve `OutboxMetricsReader`. Se ele fosse scoped, o `ValidateScopes` só acusaria o erro **em tempo de execução**, na primeira leitura. Como o `OutboxMetricsReader` não tem estado, ele passou a ser singleton.

### Limpeza do que era sobra

O dispatcher registrava `EximiaProblemDetailsFactory`, `IntegrationRequestHttpAdapter` e `HttpContextAccessor`, classes de Web API que um consumer não usa, e a interface `IIntegrationRequest` não tinha mais nenhum uso. Saíram, junto com `RouteValidator` e `RegisterAsyncIntegrationEventApplication`, que nenhum host chamava.

## O que sobrou de service locator, de propósito

- **`DestinationDispatcherFactory`**: o dispatcher é escolhido pelo **nome da classe gravado na rota** (no banco). A resolução em tempo de execução é o ponto de extensão, e por isso todo dispatcher possível precisa estar registrado em `AddIntegrationsRouting`.
- **`IntegrationEventPolicyEngine` e `IntegrationEventActionsEngine`**: resolvem regras por tipo via `IServiceProvider`, e por isso essas regras precisam estar registradas explicitamente.

## Regras para levar

- **Singleton é para o que não guarda estado de uma operação**, e não para o que é muito usado.
- **Singleton não recebe serviço scoped.** Em um serviço de background, crie um escopo por iteração com `IServiceScopeFactory.CreateAsyncScope()`.
- **Ligue `ValidateScopes` e `ValidateOnBuild` em todos os ambientes**, não só em desenvolvimento. É a diferença entre descobrir o problema ao subir ou às 3 da manhã.
- **Registro explícito vale mais que varredura**: quem lê sabe o que existe e com qual tempo de vida.

## Próxima branch

`04-shutdown`: o encerramento seguro, e conexões, banco e recursos fechados do jeito certo.
