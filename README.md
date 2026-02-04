# C# Jeito Certo

Repositório de exemplos e workshops do curso **C# Jeito Certo**, com foco em boas práticas, arquitetura e comunicação entre serviços em .NET.

---

## Objetivo do repositório

- Servir como material de apoio e referência dos conceitos abordados no curso.
- Oferecer projetos práticos que ilustram diferentes estilos de arquitetura (em camadas, Clean Architecture, Ports and Adapters, Vertical Slice).
- Demonstrar padrões de integração entre serviços: orquestração síncrona e coreografia com mensageria (Kafka, RabbitMQ), incluindo padrão Outbox.
- Contrastar abordagens que aumentam o custo de manutenção com alternativas mais sustentáveis (ex.: controller-based vs vertical slice).

Os exemplos usam domínios como **FinanceManager** (contas, transações, orçamento) e **GymErp** (matrículas, assinaturas, financeiro) para manter o contexto didático consistente.

---

## Estrutura dos projetos

### Módulo 01

| Pasta | Descrição |
|-------|-----------|
| **Workshop 03** | **FinanceManager** — aplicação de controle financeiro em duas versões: **Controller-Based** (estrutura tradicional com controllers e handlers separados) e **Vertical Slice** (organização por feature/vertical slice). Inclui autenticação, cache, mensageria, background services e tratamento global de erros. |

---

### Módulo 02

| Pasta | Descrição |
|-------|-----------|
| **Workshop 03** | Integração entre serviços com o domínio **GymErp** (matrículas, cancelamento, suspensão, financeiro). |
| **Orquestracao Sincrona** | Comunicação entre serviços via chamadas HTTP síncronas (orquestração). |
| **Coreografia - RabbitMq** | Coreografia com **RabbitMQ** (MassTransit). |
| **Coreografia - RabbitMq - Native** | Coreografia com **RabbitMQ** usando cliente nativo. |
| **Coreografia - Kafka** | Coreografia com **Kafka** (MassTransit). |
| **Coreografia - Kafka - Native** | Coreografia com **Kafka** usando cliente nativo. |
| **Coreografia + Outbox - Kafka** | Coreografia com **Kafka** e padrão **Outbox** para consistência entre persistência e publicação de mensagens. |

---

### Módulo 03

| Pasta | Descrição |
|-------|-----------|
| **Workshop 01** | Mesmo domínio **GymErp** (matrículas, assinaturas, cobranças) implementado em diferentes arquiteturas para comparação. |
| **Layered Architecture** | Arquitetura em camadas (API, Application, Domain, Infrastructure, CrossCutting). |
| **Clean Architecture** | Clean Architecture com camadas bem definidas e use cases explícitos. |
| **Ports And Adapters** | Arquitetura Hexagonal (Ports and Adapters). |
| **Vertical Slice Architecture** | Vertical Slice: organização por feature/vertical, com Domain, Application e endpoints por caso de uso. |

---

## Como usar

- Cada pasta de projeto contém uma ou mais soluções (`.sln`). Abra a solução desejada no Visual Studio ou via `dotnet sln`.
- Projetos com mensageria (Kafka/RabbitMQ) podem exigir Docker ou instâncias locais dos brokers; verifique `docker-compose` ou `README` dentro da pasta quando existir.
- Os projetos do **GymErp** costumam incluir testes unitários e de integração em pastas `GymErp.UnitTests` e `GymErp.IntegrationTests`.

---

## Tecnologias

- **.NET** (ASP.NET Core, Entity Framework Core)
- **Mensageria:** MassTransit, Kafka, RabbitMQ
- **Persistência:** Entity Framework Core, SQL Server / PostgreSQL
- **Testes:** xUnit, testes de integração com cenários de domínio

---

*Material do curso C# Jeito Certo — exemplos para estudo e referência.*
