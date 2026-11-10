# Arquitetura .NET em produção: erros que comprometem a confiabilidade

Projeto de referência da masterclass. É uma plataforma de integração entre sistemas, construída em .NET 10, com uma API, um consumidor Kafka e um agente de tarefas em background.

## Como o material está organizado

O código evolui **por branches**. Cada branch é o sistema completo num momento da história, e cada uma nasce da anterior:

| Branch | O que acontece nela |
|---|---|
| `masterclass/dotnet-producao/00-baseline` | O sistema como foi entregue, com todos os problemas |
| `masterclass/dotnet-producao/01-sync-over-async` | Código assíncrono bloqueado: `.Result`, `.Wait()` e ThreadPool starvation |
| `masterclass/dotnet-producao/02-cancellation-token` | O token que não chega às queries, às chamadas HTTP e às operações filhas |
| `masterclass/dotnet-producao/03-tempo-de-vida` | Singleton desnecessário, captive dependency (`DbContext` preso em handler e em serviço persistente) e container sem validação |
| `masterclass/dotnet-producao/04-shutdown` | Encerramento: cancelamento não é falha, e conexões fechadas do jeito certo |
| `masterclass/dotnet-producao/05-background-service` | `BackgroundService` não é um `while(true)`: ciclo de vida e escolha do host |
| `masterclass/dotnet-producao/06-tasks-soltas` | `Task.Run` solto, `async void` e exceções que desaparecem |

Para acompanhar, troque de branch (`git switch <branch>`) e leia o documento daquela etapa em [docs/](docs/). Para ver só o que uma etapa mudou: `git diff <branch anterior>..<branch>`.

## A solução

```
Eximia.Integrations.sln
└── src/
    ├── Eximia.Integrations                   biblioteca com o domínio, roteamento e outbox
    ├── Eximia.Integrations.HttpApi           API que recebe os eventos das origens
    ├── Eximia.Integrations.EventDispatcher   consumidor Kafka que entrega os eventos aos destinos
    └── Eximia.Integration.BackgroundAgent    tarefas em background
```

Para compilar: `dotnet build Eximia.Integrations.sln` (SDK do .NET 10). A masterclass é de leitura de código: não é preciso executar nada.

## Documentos

| Documento | Assunto |
|---|---|
| [00-baseline](docs/00-baseline.md) | O sistema e o fluxo de uma integração |
| [01-sync-over-async](docs/01-sync-over-async.md) | Threads bloqueadas esperando I/O |
| [02-cancellation-token](docs/02-cancellation-token.md) | O token até o banco e até o HTTP, e onde não usá-lo |
| [03-tempo-de-vida](docs/03-tempo-de-vida.md) | Tempo de vida, escopo e validação do container |
| [04-shutdown](docs/04-shutdown.md) | Encerramento seguro e recursos fechados na ordem certa |
| [05-background-service](docs/05-background-service.md) | Ciclo de vida do `BackgroundService` e tipo de host |
| [06-tasks-soltas](docs/06-tasks-soltas.md) | Fire-and-forget, `async void` e paralelismo |

Cada documento está na branch da sua etapa e nas seguintes.
