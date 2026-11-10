# Arquitetura .NET em produção: 7 erros que comprometem a confiabilidade

Projeto de referência da masterclass. É uma plataforma de integração entre sistemas, construída em .NET 10, com API, consumidor Kafka e um agente de tarefas em background.

## Como o material está organizado

O código evolui **por branches**. Cada branch é o sistema completo num momento da história, e cada uma nasce da anterior:

| Branch | O que acontece nela |
|---|---|
| `masterclass/arquitetura-dotnet/00-baseline` | O sistema como foi entregue, com os 7 problemas |
| `masterclass/arquitetura-dotnet/01-sync-over-async` | Código assíncrono que bloqueia threads e esgota o pool |
| `masterclass/arquitetura-dotnet/02-tasks-soltas` | Fire-and-forget, `async void` e paralelismo sem controle |
| `masterclass/arquitetura-dotnet/03-cancellation-token` | O token de cancelamento que não chega onde importa |
| `masterclass/arquitetura-dotnet/04-worker-service` | Tipo de host e ciclo de vida do `BackgroundService` |
| `masterclass/arquitetura-dotnet/05-dependencias` | Tempo de vida das dependências e validação do container |
| `masterclass/arquitetura-dotnet/06-shutdown` | Encerramento seguro: cancelamento não é falha |
| `masterclass/arquitetura-dotnet/07-mensageria` | Processamento confiável de mensagens |
| `masterclass/arquitetura-dotnet/08-vertical-slice` | O mesmo sistema, organizado por caso de uso |

Para acompanhar, troque de branch (`git switch <branch>`) e leia o documento daquela etapa em [docs/](docs/). Cada etapa acrescenta o seu documento, explicando o problema e a solução.

## A solução

```
Eximia.Integrations.sln
└── src/
    ├── Eximia.Integrations                   biblioteca com o domínio, roteamento, outbox e telemetria
    ├── Eximia.Integrations.HttpApi           API que recebe os eventos das origens
    ├── Eximia.Integrations.EventDispatcher   consumidor Kafka que entrega os eventos aos destinos
    └── Eximia.Integration.BackgroundAgent    tarefas em background
```

Para compilar: `dotnet build Eximia.Integrations.sln` (SDK do .NET 10). A masterclass é de leitura de código: não é preciso executar nada.
