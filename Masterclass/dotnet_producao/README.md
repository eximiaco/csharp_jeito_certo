# Arquitetura .NET em produção: samples da masterclass

Quatro samples do mesmo sistema (a **Eximia Integration Platform**, .NET 10), no formato de uma solution por tema.
A aula é de **leitura de código**: nada precisa ser executado. Cada sample compila com
`dotnet build Eximia.Integrations.slnx`.

| Sample | Temas | O que tem |
|---|---|---|
| [sample01](sample01/README.md) | 1️⃣ Bloquear código assíncrono · 2️⃣ `CancellationToken` que não chega | Só os problemas 1 e 2 |
| [sample02](sample02/README.md) | 5️⃣ Tempo de vida das dependências · 6️⃣ Shutdown | Só os problemas 5 e 6 |
| [sample03](sample03/README.md) | 3️⃣ Tasks soltas · 4️⃣ `BackgroundService` e host (.NET 10) | Só os problemas 3 e 4, e um consumer Kafka com problemas de thread |
| [sample04](sample04/README.md) | Tudo corrigido | A solução completa |

Os samples 1 a 3 são o sample 4 **com só os problemas do seu tema plantados**. Por isso
`diff -rq sampleNN/src sample04/src -x bin -x obj` mostra exatamente as correções daquele tema, sem ruído dos outros.

## O sistema

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

1. **Registro (HttpApi).** A origem envia o evento. A API grava o payload no storage, descobre as rotas aplicáveis e
   cria um evento de integração por rota. Os eventos e as mensagens para o Kafka são gravados na mesma transação (outbox).
2. **Publicação.** O `OutboxWorker` do Silverback lê o outbox e publica no Kafka.
3. **Entrega (EventDispatcher).** O consumer marca o evento como `Processing`, baixa o payload e faz o POST no
   destino, com retry e autenticação JWT quando a rota exige.
4. **Callback.** Quando a rota exige resposta, o evento fica em `WaitingCallback`. O destino chama
   `POST /api/integrations/callback`, que registra um evento de volta para a origem e finaliza o original.
5. **Callback de erro.** Se a entrega falha sem possibilidade de retry, o dispatcher publica um comando no tópico de
   callback e um segundo consumer devolve o erro à origem, pela mesma API.
6. **Background (BackgroundAgent).** Um worker devolve para a fila os eventos que ficaram parados, e um consumer
   (client puro da Confluent) monta a linha do tempo de cada integração a partir dos eventos da plataforma.

Cada sample tem a mesma estrutura: `Eximia.Integrations.slnx`, `src/` e `sql/`.

| Projeto | O que é |
|---|---|
| `Eximia.Integrations` | Biblioteca: casos de uso, acesso a dados, roteamento, dispatchers, outbox |
| `Eximia.Integrations.HttpApi` | Host HTTP: recebe os eventos e os callbacks |
| `Eximia.Integrations.EventDispatcher` | Host consumidor (Silverback): entrega ao destino |
| `Eximia.Integration.BackgroundAgent` | Host de background: reprocessamento e consumer de eventos da plataforma |

> O que é conclusão de leitura de código (e não comportamento observado rodando) está marcado como tal nos READMEs:
> por exemplo, o efeito do fallback do Polly sobre o cancelamento e o fechamento das conexões do Npgsql no shutdown.

## Nomes dos tópicos Kafka

Os nomes são **fixos**, sem fábrica e sem prefixo de ambiente: vêm do arquivo de configuração de cada host
(`Kafka:Events:Topic`, `Kafka:Callback:Topic` e `Kafka:Consumer:Inbounds[].Topics`). O tópico de entrega de cada rota
é o `TopicName` cadastrado na rota ou, na falta dele, `destino-entidade` em minúsculas
(`JsonSchemaService.CreateFrom`).

## Fora do escopo

Defeitos de **mensageria** do fluxo principal (consumidor não idempotente, retry sem limite, republicação fora do
outbox) e Vertical Slice ficam de fora, de propósito.
