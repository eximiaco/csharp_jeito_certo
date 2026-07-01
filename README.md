TESTES DE COMMMIT

# C# Jeito Certo
Repositório de exemplos e workshops do curso **C# Jeito Certo**, com foco em boas práticas, arquitetura e comunicação entre serviços em .NET.

---

## Objetivo do repositório
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

