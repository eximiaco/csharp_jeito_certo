# Sample 1 · Código assíncrono bloqueado e CancellationToken que não chega

**Temas:** 1️⃣ Bloquear código assíncrono (`.Result`, `.Wait()`, sync-over-async e ThreadPool starvation) ·
2️⃣ Deixar o `CancellationToken` pelo caminho (token que não chega às queries, ao HTTP e ao publisher).

Este sample é o sistema do sample 4 com **só os problemas 1 e 2** plantados. O resto (tempo de vida das
dependências, encerramento, tarefas soltas, hosts) está correto.

Para compilar: `dotnet build Eximia.Integrations.slnx`. Nada aqui precisa ser executado: a leitura do código basta.

## Problema 1 · Código assíncrono bloqueado

**Por que é um problema (vale para os nove pontos).** `.Result` e `.GetAwaiter().GetResult()` seguram uma thread
do `ThreadPool` até a rede ou o banco responderem. O consumer processa várias mensagens em paralelo e a API
atende várias requisições ao mesmo tempo, então basta o serviço externo ficar lento para todas bloquearem threads
juntas. O pool começa com poucas threads (o mínimo é o número de núcleos) e só cria novas aos poucos, cerca de uma
a cada 500 ms. O resultado é *task starvation*: a latência explode, a CPU fica baixa e o processo parece travado
sem estar fazendo nada. Em todos os pontos o método que chama **já é `async`**, então o `.Result` não evita nada:
só troca a liberação da thread por um bloqueio.

| Fluxo | Arquivo | Linha | Chamada | Por que pesa aqui |
|---|---|---|---|---|
| Entrega (consumidor) | `Eximia.Integrations/Routing/RoutingService.cs` | 20 | `DownloadAsync(...).Result`, download do payload | Roda em **toda** entrega: cada mensagem em andamento segura uma thread até o storage responder |
| Entrega (consumidor) | `Eximia.Integrations/Routing/Dispatchers/HttpJsonWithJwtDispatcher.cs` | 38 | `GetTokenAsync(route).GetAwaiter().GetResult()` | Roda em toda entrega a destino com JWT, e mais ainda quando o provedor de identidade está lento |
| Renovação do token no 401 | `Eximia.Integrations/Routing/Dispatchers/PolicyFactory.cs` | 23 | `GetResponseStringAsync().Result` | Callback `onRetry` **síncrono**. Quando o token do destino expira, **todas** as entregas em andamento tomam 401 juntas e entram aqui ao mesmo tempo |
| Renovação do token no 401 | `Eximia.Integrations/Routing/Dispatchers/PolicyFactory.cs` | 27 | `GetTokenAsync(...).GetAwaiter().GetResult()` | Mesmo gatilho: a renovação bloqueia uma thread por entrega, esperando justamente o provedor de identidade que está lento |
| Caminho de erro | `Eximia.Integrations/IntegrationEvents/Application/Routing/HandleRoutingErrorService.cs` | 34-35 | `NeedRetryAsync(...).GetAwaiter().GetResult()` | Consulta o banco. Com o destino fora do ar, **todas** as mensagens caem aqui ao mesmo tempo: o sistema piora justamente quando mais precisa responder |
| Callback de erro | `Eximia.Integrations/IntegrationEvents/Services/SendIntegrationEventService.cs` | 30 | `GetStringAsync().Result` | Leitura do corpo da resposta ao devolver o erro à origem, também em caminho de falha |
| Callback de erro | `Eximia.Integrations/IntegrationEvents/Services/SendIntegrationEventService.cs` | 37 | `GetResponseStringAsync().Result` | Idem, no tratamento da exceção HTTP |
| Registro (API) | `Eximia.Integrations/IntegrationEvents/Application/Register/RegisterMultipleRoutesIntegrationEventApplication.cs` | 36 | `GetRoutesByOriginAndEntityAsync(...).GetAwaiter().GetResult()` | Segura a thread da requisição durante a ida ao banco, em **toda** requisição da API |
| Registro (API) | `Eximia.Integrations/IntegrationEvents/Application/Register/RegisterMultipleRoutesIntegrationEventApplication.cs` | 63 | `UploadAsync(...).Result` | Segura a thread da requisição durante o upload ao storage, que é lento por natureza. A vazão da API passa a ser limitada pelo pool, e não pela capacidade real |

**Cache de token que não funciona** (`Eximia.Integrations/Routing/Authentication/TokenServiceProvider.cs`)
- Linha 26: `FromMilliseconds(expires_in)`.
  **Por quê:** o padrão OAuth2 define `expires_in` como duração em **segundos**. Tratado como milissegundos, um token
  de 3600 s vira um cache de 3,6 s. Além disso, `JwtBearerToken.Expired` trata o mesmo valor como instante absoluto
  em segundos, então o token sempre parece vencido e o método **pede um token novo a cada chamada**: o cache não
  cumpre função nenhuma.
- Linhas 28 e 33: devolve a variável local `token`.
  **Por quê:** ela só é preenchida dentro do *factory* do cache. Quando o valor vem do cache, o método devolve `null`,
  a requisição sai sem `Authorization`, toma 401 e a política busca outro token.
- Sem single-flight. **Por quê:** N mensagens simultâneas disparam N pedidos de token ao mesmo tempo (*stampede*)
  no provedor de identidade.

## Problema 2 · Token que não chega onde importa

O token entra pelo consumer e nunca é entregue ao driver, ao HTTP nem ao publisher. Não há `WaitAsync` em lugar
nenhum: simplesmente ninguém repassa o token.

**Por que é um problema (vale para todo o grupo).** O cancelamento só interrompe o que recebeu o token. No
shutdown ou num timeout, a query continua rodando no Postgres (visível em `pg_stat_activity`), a conexão do pool
continua ocupada, a chamada HTTP só termina pelo timeout da rota, e o processo espera ou é morto com trabalho
pendurado. Para quem lê o código, "está cancelável". Para o banco e para a rede, não está.

**Queries que aceitam o token e não o repassam** (`Eximia.Integrations/IntegrationEvents/IntegrationEventsDataAccess.cs`)
- `InsertAsync` (linha 13) recebe o token, mas o `ExecuteScalarAsync` (linha 104) não.
- `GetAndSetEventToDispatch` (linha 138), com `QueryFirstOrDefaultAsync` sem token (linha 151).
- `GetAsync` (linha 237), idem (linha 242).
- `GetDestinyAsync` (linha 294), idem (linha 305).
- `FinalizeOriginalIntegrationEventsForCallback` (linha 224), idem.
- **Por quê:** isto é **pior** do que não ter o parâmetro. A assinatura promete que o método respeita o
  cancelamento, e quem revisa o código acredita nisso.

**Métodos que nem recebem o token** (`IntegrationEventsDataAccess.cs`)
- `UpdateIntegrationProcess` (linha 202).
- `UpdateKafkaOffsetAsync` (linha 255).
- **Por quê:** não há como cancelá-los, por mais que o chamador tenha um token.

**Outros acessos a dados sem o token**
- `Eximia.Integrations/DestinationsErrorsConfiguration/DataAccess/DestinationErrorConfigurationDataAccess.cs`: o método recebe o token (linha 19) e o `QueryAsync` não (linha 34).
- `Eximia.Integrations/Routing/RoutingDataAccess.cs`: linhas 31, 97, 162 e 226 (consultas de rota sem token).
- **Por quê:** mesmo padrão. Aqui o token também precisa chegar ao *factory* do cache, que pode rodar em nome de várias requisições.

**Publisher sem o token**
- `HandleRoutingErrorService.cs`: linha 52 (`PublishAsync(callbackMessage)`).
- `RegisterMultipleRoutesIntegrationEventApplication.cs`: linha 92 (`PublishAsync(RouteIntegrationEventRequestCommand.Create(...))`).
- **Por quê:** o `PublishAsync` aceita o token e ele não é passado. O envio não pode ser interrompido.

**Política de resiliência executada sem o token**
- `Eximia.Integrations/Routing/Dispatchers/HttpJsonDispatcher.cs`: `policy.ExecuteAsync` (linha 29) e `PostStringAsync(payload)` (linha 37).
- `Eximia.Integrations/Routing/Dispatchers/HttpJsonWithJwtDispatcher.cs`: linhas 33 e 49.
- **Por quê:** é o caso mais traiçoeiro, porque não há nenhum `CancellationToken.None` escrito: o token simplesmente
  não entrou na política, e tudo que nasce depois herda "nunca cancele". Três efeitos:
  - A chamada HTTP só termina pelo timeout da rota.
  - As esperas entre as tentativas do retry (`2^n` segundos) são operações filhas criadas pela política e **não
    podem ser canceladas**. Um destino fora do ar prende o consumer por mais de um minuto, cancelado ou não.
  - A obtenção do token JWT, dentro de cada tentativa, também fica sem cancelamento.

**Chamadas do consumer que não repassam o token** (`Eximia.Integrations/IntegrationEvents/Application/Routing/RouteIntegrationEventApplication.cs`)
- Linha 37: `UpdateKafkaOffsetAsync`.
- Linha 44: `GetRouteWithoutRules`.
- Linha 63: `UpdateIntegrationProcess` no retry.
- Linha 75: `UpdateIntegrationProcess` depois da entrega, sem token próprio e curto.
- **Por quê (as três primeiras):** a cadeia continua sem token, mesmo ele estando disponível no método.
- **Por quê (a última):** é o ponto sutil. O destino **já recebeu** o evento e agora se grava o resultado. Sem prazo,
  a gravação pode ficar pendurada. E a correção **não** é passar o token do consumer: um shutdown nesse instante
  abortaria a gravação, o evento ficaria "não confirmado" e a reentrega do Kafka duplicaria a entrega ao destino.
  O certo é um token **próprio e curto**, só para esse passo.

## O que NÃO é problema neste sample

- `WaitAsync` não é usado. Ele só **para de esperar**: a operação continua rodando. Passar o token adiante é o que
  cancela de verdade.
- O consumer de eventos da plataforma (`PlatformEventsConsumer`) usa I/O **síncrono** numa thread dedicada, de
  propósito, e não tem `.Result` nem `GetResult()`. Ele é igual nos samples 1, 2 e 4 e aparece com problemas de
  thread no sample 3.

## Como ver só o que mudou

```
diff -rq sample01/src sample04/src -x bin -x obj
```

Os arquivos abaixo são os que diferem do sample 4. Tudo que não está na lista é idêntico: o sample foi montado a
partir do sample 4 com **só os problemas deste tema** plantados.

- `Eximia.Integration.BackgroundAgent/Workers/PlatformEventsConsumer.cs`
- `Eximia.Integration.BackgroundAgent/Workers/ReprocessStuckEventsWorker.cs`
- `Eximia.Integrations/DependencyInjection.cs`
- `Eximia.Integrations/DestinationsErrorsConfiguration/DataAccess/DestinationErrorConfigurationDataAccess.cs`
- `Eximia.Integrations/IntegrationEvents/Application/Register/RegisterMultipleRoutesIntegrationEventApplication.cs`
- `Eximia.Integrations/IntegrationEvents/Application/Routing/HandleRoutingErrorService.cs`
- `Eximia.Integrations/IntegrationEvents/Application/Routing/RouteIntegrationEventApplication.cs`
- `Eximia.Integrations/IntegrationEvents/IntegrationEventsDataAccess.cs`
- `Eximia.Integrations/IntegrationEvents/Services/SendIntegrationEventService.cs`
- `Eximia.Integrations/Routing/Authentication/JwtBearerToken.cs`
- `Eximia.Integrations/Routing/Authentication/Providers/ClientCredentialsProvider.cs`
- `Eximia.Integrations/Routing/Authentication/TokenCache.cs  (só no sample 4)`
- `Eximia.Integrations/Routing/Authentication/TokenServiceProvider.cs`
- `Eximia.Integrations/Routing/Dispatchers/HttpJsonDispatcher.cs`
- `Eximia.Integrations/Routing/Dispatchers/HttpJsonWithJwtDispatcher.cs`
- `Eximia.Integrations/Routing/Dispatchers/PolicyFactory.cs`
- `Eximia.Integrations/Routing/RoutingDataAccess.cs`
- `Eximia.Integrations/Routing/RoutingService.cs`
