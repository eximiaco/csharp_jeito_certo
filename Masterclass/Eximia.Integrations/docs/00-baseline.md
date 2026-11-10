# 00 · Baseline: o sistema como foi entregue

Esta branch é o ponto de partida. O sistema funciona no dia a dia, mas carrega sete decisões que comprometem a confiabilidade em produção. Cada branch seguinte corrige uma delas.

## O fluxo de uma integração

```
Origem ──POST /api/integrations──► HttpApi ──(evento + outbox na mesma transação)──► Postgres
                                      │                                                  │
                                 202 Accepted                                   OutboxWorker
                                                                                         ▼
                                                                    Kafka (RouteIntegrationEventRequestCommand)
                                                                                         │
                                                       EventDispatcher (consumer) ◄──────┘
                                                              │ entrega via HTTP
                                                              ▼
                                                           Destino ──POST /api/integrations/callback──► HttpApi ──► … ──► Origem
```

1. **Registro (HttpApi).** A origem envia o evento. A API grava o payload no storage, descobre as rotas aplicáveis e cria um evento de integração por rota. Os eventos e as mensagens para o Kafka são gravados na mesma transação (outbox).
2. **Publicação.** O `OutboxWorker` lê o outbox e publica no Kafka. Ao confirmar, marca os eventos como `InQueue`.
3. **Entrega (EventDispatcher).** O consumer marca o evento como `Processing`, baixa o payload e faz o POST no destino, com retry e autenticação JWT quando a rota exige.
4. **Callback.** Quando a rota exige resposta, o evento fica em `WaitingCallback`. O destino chama `POST /api/integrations/callback`, que registra um evento de volta para a origem e finaliza o original.
5. **Callback de erro.** Se a entrega falha sem possibilidade de retry, o dispatcher publica um comando no tópico de callback e um segundo consumer devolve o erro à origem, pela mesma API.
6. **Background (BackgroundAgent).** Um worker devolve para a fila os eventos que ficaram parados.

## Onde olhar

| Parte do fluxo | Arquivo |
|---|---|
| Entrada HTTP | [IntegrationEventsController.cs](../src/Eximia.Integrations.HttpApi/Controllers/IntegrationEventsController.cs) |
| Registro do evento | [RegisterMultipleRoutesIntegrationEventApplication.cs](../src/Eximia.Integrations/IntegrationEvents/Application/Register/RegisterMultipleRoutesIntegrationEventApplication.cs) |
| Outbox | [OutboxTransaction.cs](../src/Eximia.Integrations/Outbox/OutboxTransaction.cs), [EximiaOutboxReader.cs](../src/Eximia.Integrations/Outbox/EximiaOutboxReader.cs) |
| Consumo e entrega | [RouteIntegrationEventApplication.cs](../src/Eximia.Integrations/IntegrationEvents/Application/Routing/RouteIntegrationEventApplication.cs), [HttpJsonWithJwtDispatcher.cs](../src/Eximia.Integrations/Routing/Dispatchers/HttpJsonWithJwtDispatcher.cs), [PolicyFactory.cs](../src/Eximia.Integrations/Routing/Dispatchers/PolicyFactory.cs) |
| Token do destino | [TokenServiceProvider.cs](../src/Eximia.Integrations/Routing/Authentication/TokenServiceProvider.cs) |
| Acesso a dados | [IntegrationEventsDataAccess.cs](../src/Eximia.Integrations/IntegrationEvents/IntegrationEventsDataAccess.cs) |
| Callback de erro | [ProcessIntegrationEventCallbackApplication.cs](../src/Eximia.Integrations/IntegrationEvents/Application/ProcessIntegrationEventCallbackApplication.cs) |
| Hosts | `Program.cs` de cada projeto em [src/](../src/) |
| Worker de reprocessamento | [ReprocessStuckEventsWorker.cs](../src/Eximia.Integration.BackgroundAgent/Workers/ReprocessStuckEventsWorker.cs) |

## O que vem a seguir

Os problemas estão espalhados pelo fluxo acima, onde aconteceriam num sistema real. Antes de trocar de branch, vale ler os arquivos da tabela e tentar encontrá-los.
