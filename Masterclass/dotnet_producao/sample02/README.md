# Sample 2 · Tempo de vida das dependências e encerramento

**Temas:** 5️⃣ Errar o tempo de vida das dependências (singletons desnecessários, `DbContext` preso em serviço
persistente) · 6️⃣ Achar que o shutdown se resolve sozinho (conexões TCP, banco e arquivos encerrados do jeito certo).

Este sample é o sistema do sample 4 com **só os problemas 5 e 6** plantados. Código assíncrono, propagação do
token, tarefas soltas e hosts estão corretos.

Para compilar: `dotnet build Eximia.Integrations.slnx`. Nada aqui precisa ser executado: a leitura do código basta.

## Problema 5 · Tempo de vida das dependências

O container é o padrão do .NET (sem Autofac), e as dependências são registradas por caso de uso em
`Eximia.Integrations/DependencyInjection.cs`.

**Captive dependency: singleton que recebe serviço scoped** (`Eximia.Integrations/DependencyInjection.cs`)
- Linha 75: `AddSingleton<RegisterMultipleRoutesIntegrationEventApplication>()`, o handler da API.
- Linha 32: `AddSingleton<PublishPlataformEventService>()`.
- **Por quê:** o handler depende de serviços **scoped**: o `EximiaIntegrationDbContext`, o `IPublisher` do Silverback
  e o `JsonSchemaService`. O container resolve as dependências de um singleton a partir do escopo raiz, então elas
  passam a viver o tempo do processo, e **todas as requisições da API compartilham o mesmo `DbContext`**. Efeitos
  (leitura do código, não observados rodando): o `DbContext` não é thread-safe, então requisições simultâneas
  colidem ("A second operation was started on this context instance"); o *change tracker* e a transação do outbox,
  que são de uma requisição, passam a ser de todas, e eventos de uma podem ir no `SaveChanges` de outra. A
  justificativa "é usado em toda requisição, então uma instância só" não se sustenta: criar o handler por requisição
  é barato, e o `AddDbContextPool` já torna barato o que tem estado. O `PublishPlataformEventService` recebe o
  `IPublisher`, que é scoped: mesma regra. Nada disso quebra um teste com uma requisição por vez.

**Singleton desnecessário** (`Eximia.Integrations/DependencyInjection.cs`)
- Linha 99: `AddSingleton<SendIntegrationEventService>()`, com o comentário "uma instância basta para todo o processo".
- **Por quê:** singleton é a decisão de compartilhar uma instância, e aqui não há o que compartilhar. O serviço só
  recebe o `IConfiguration` (singleton), lê a URL da API no construtor e faz um POST: não guarda estado por operação,
  não é caro de criar e não é dono de nenhum recurso. **Nada quebra hoje**, e é isso que o torna traiçoeiro:
  - **Sem ganho.** Singleton se justifica por estado compartilhado, recurso caro ou dono de recurso (o `TokenCache`
    e o `NpgsqlDataSource` são assim). Este não protege nada.
  - **Armadilha para a próxima mudança.** Quem precisar do `DbContext` ou do acesso a dados nesse serviço e adicionar
    ao construtor cria uma captive dependency **sem aviso**, porque a validação do container está desligada.
  - **Configuração congelada.** A URL é lida uma vez e vale até reiniciar o processo; com o serviço scoped, é relida
    a cada mensagem.
- **Contraste com um singleton correto:** `PostgresConnectionStringFactory` (linha 29) lê a configuração uma vez e é
  imutável, então compartilhar a instância faz sentido. O `TokenCache` (linha 50) também: os semáforos dele precisam sobreviver entre mensagens.

**`DbContext` preso em serviço persistente**
- `Eximia.Integration.BackgroundAgent/Workers/ReprocessStuckEventsWorker.cs`: linha 18. O `BackgroundService` (singleton) recebe o
  `EximiaIntegrationDbContext` no construtor.
- **Por quê:** o `DbContext` foi feito para uma unidade de trabalho curta. Preso ao singleton, ele vive o tempo do
  processo:
  - **Vazamento de memória:** o *change tracker* guarda toda entidade carregada. A cada 5 minutos o worker carrega
    até 100 eventos, e nenhum é esquecido.
  - **Dados velhos:** o contexto devolve entidades já rastreadas em vez de reler do banco, um cache involuntário.
  - **Concorrência:** o `DbContext` não é thread-safe, e um único objeto serve a todas as iterações.

**Validação do container desligada** (`ValidateScopes = false` e `ValidateOnBuild = false`)
- `Eximia.Integrations.HttpApi/Program.cs`: linha 51.
- `Eximia.Integrations.EventDispatcher/Program.cs`: linha 50.
- `Eximia.Integration.BackgroundAgent/Program.cs`: linha 40.
- **Por quê:** por padrão, o container do .NET só valida em Development: `ValidateScopes` acusa um singleton que
  consome um serviço scoped, e `ValidateOnBuild` acusa uma dependência não registrada, ambos na subida. Desligar nos
  três hosts deixa os problemas acima passarem em silêncio em **todos** os ambientes. Religando a validação, a API
  deixa de subir (`Cannot consume scoped service ... from singleton ...RegisterMultipleRoutesIntegrationEventApplication`)
  e o agent acusa o worker.

**Health check do outbox sem `IOutboxReader`**
- `Eximia.Integrations.HttpApi/Infrastructure/ServicesExtensions.cs`, método `AddHealth` (linha 134).
- **Por quê:** o health check do Silverback (`OutboxHealthCheckService`) exige um `IOutboxReader` que nada registra.
  Com a validação ligada, a API não sobe por causa disso, o que indica que o `/health-check` falharia na primeira
  chamada de verdade. Sem validação, ninguém vê.

**Sobras de Web API no host sem HTTP** (`HttpContextAccessor`, `IntegrationRequestHttpAdapter`/`IIntegrationRequest` e `EximiaProblemDetailsFactory`)
- `Eximia.Integrations.EventDispatcher/Program.cs`: linhas 35, 37 e 39.
- **Por quê:** um consumer não tem `HttpContext`. Registrar o que ninguém usa engana quem lê (cada linha sugere que
  algo depende dela) e carrega dependências sem necessidade.

## Problema 6 · Encerramento sem cuidado

**Cancelamento tratado como falha de entrega**
- `Eximia.Integrations/Routing/Dispatchers/PolicyFactory.cs`: linha 63, `Handle<Exception>()` no fallback do Polly.
- `Eximia.Integrations/IntegrationEvents/Application/Routing/RouteIntegrationEventApplication.cs`: linha 82, `catch (Exception ex)`.
- **Por quê (hipótese lida no código, não observada rodando):** se o cancelamento chegar à política, o fallback o
  converte em `DispatcherResult.Failure`. Daí o `HandleRoutingErrorService` (linha 25) grava o evento como `Error`,
  publica o evento de erro e **envia um callback de erro à origem**, avisando de uma falha que nunca aconteceu.
  Depois o offset é confirmado, e o erro falso fica permanente. Um deploy, que é só um cancelamento, vira
  integração com erro.

**Sem prazo de encerramento**
- Nenhum dos três hosts configura `HostOptions.ShutdownTimeout`: `HttpApi/Program.cs`, `EventDispatcher/Program.cs` e
  `BackgroundAgent/Program.cs`.
- **Por quê:** vale o padrão de 30 segundos, mas os retries do destino (`2^n` segundos × `MaxRetry`, mais o `TimeOut`
  da rota) podem passar disso. O processo recebe `SIGKILL` com o evento ainda em `Processing`, e o descarte do
  container, que fecharia os recursos, nem chega a acontecer. O prazo também precisa ser menor que o
  `terminationGracePeriodSeconds` do pod.

**Pool de conexões sem dono** (`new NpgsqlConnection(...)`, 15 ocorrências)
- `Eximia.Integrations/IntegrationEvents/IntegrationEventsDataAccess.cs`: 8.
- `Eximia.Integrations/Routing/RoutingDataAccess.cs`: 4.
- `Eximia.Integrations/DestinationsErrorsConfiguration/DataAccess/DestinationErrorConfigurationDataAccess.cs`: 1.
- `Eximia.Integrations/Shared/Postgres/PostgresAdvisoryLock.cs`: 1 (linha 18). Aqui a conexão **precisa** ficar aberta enquanto o lock vale.
- `Eximia.Integrations/PlatformEvents/PlatformEventTimelineDataAccess.cs`: 1 (linha 26).
- **Por quê:** abrir uma conexão curta por operação é o padrão certo. O problema é **de quem é o pool**: com
  `new NpgsqlConnection(connectionString)` ele é global e indexado pela *connection string*, e ninguém o descarta.
  No encerramento, as conexões TCP com o Postgres não são fechadas pelo protocolo, morrem com o processo, e o
  servidor percebe o fim abrupto depois. É leitura do comportamento do Npgsql, não observada rodando aqui.

## O que NÃO é problema neste sample

- O agent é um Worker Service puro e o dispatcher usa o web host mínimo: o tema dos hosts é do sample 3.
- Não há `.Result`, tarefas soltas nem token perdido: temas dos samples 1 e 3.

## Como ver só o que mudou

```
diff -rq sample02/src sample04/src -x bin -x obj
```

Os arquivos abaixo são os que diferem do sample 4. Tudo que não está na lista é idêntico: o sample foi montado a
partir do sample 4 com **só os problemas deste tema** plantados.

- `Eximia.Integration.BackgroundAgent/Infrastructure/ServicesExtensions.cs`
- `Eximia.Integration.BackgroundAgent/Program.cs`
- `Eximia.Integration.BackgroundAgent/Workers/PlatformEventsConsumer.cs`
- `Eximia.Integration.BackgroundAgent/Workers/ReprocessStuckEventsWorker.cs`
- `Eximia.Integrations.EventDispatcher/Infrastructure/EximiaProblemDetailsFactory.cs  (só neste sample)`
- `Eximia.Integrations.EventDispatcher/Infrastructure/IntegrationRequestHttpAdapter.cs  (só neste sample)`
- `Eximia.Integrations.EventDispatcher/Program.cs`
- `Eximia.Integrations.HttpApi/Infrastructure/IntegrationRequestHttpAdapter.cs  (só neste sample)`
- `Eximia.Integrations.HttpApi/Infrastructure/ServicesExtensions.cs`
- `Eximia.Integrations.HttpApi/Program.cs`
- `Eximia.Integrations/DependencyInjection.cs`
- `Eximia.Integrations/DestinationsErrorsConfiguration/DataAccess/DestinationErrorConfigurationDataAccess.cs`
- `Eximia.Integrations/DispatchersTopics/DispatchersTopicsDataAccess.cs`
- `Eximia.Integrations/IntegrationEvents/Application/Routing/RouteIntegrationEventApplication.cs`
- `Eximia.Integrations/IntegrationEvents/IntegrationEventsDataAccess.cs`
- `Eximia.Integrations/PlatformEvents/PlatformEventTimelineDataAccess.cs`
- `Eximia.Integrations/Routing/Dispatchers/PolicyFactory.cs`
- `Eximia.Integrations/Routing/RoutingDataAccess.cs`
- `Eximia.Integrations/Shared/IIntegrationRequest.cs  (só neste sample)`
- `Eximia.Integrations/Shared/Postgres/PostgresAdvisoryLock.cs`
