# Architecture Decision Records — GymErp

ADRs documentam decisões arquiteturais do projeto GymErp. São a fundação de regras seguidas por humanos e IAs (Cursor, Claude Code). Cada ADR tem contexto, decisão, regras operacionais e exemplos DO/DON'T.

**Escopo por ADR:** cada ADR declara explicitamente se se aplica a `modernized/`, `legacy/` ou ao repositório como um todo. Verifique o bloco `> **Escopo:**` no cabeçalho de cada ADR antes de aplicar as regras.

Para criar novas ADRs use o template em [`TEMPLATE.md`](./TEMPLATE.md) ou a skill `/create-adr`.

---

## Índice

### Grupo A — Estrutura e Organização

| # | Título | Status |
|---|--------|--------|
| [ADR-001](./adr-001-separacao-legacy-modernized.md) | Separação legacy/modernized como Estratégia de Migração Incremental | Ativa |
| [ADR-002](./adr-002-dominios-por-contexto.md) | Domínios Organizados por Contexto sob Domain/ | Ativa |

### Grupo B — Engenharia e Padrões de Código

| # | Título | Status |
|---|--------|--------|
| [ADR-003](./adr-003-fastendpoints.md) | FastEndpoints como Framework HTTP (sem MVC Controllers) | Ativa |
| [ADR-004](./adr-004-vertical-slice.md) | Vertical Slice Architecture — Feature/Operação com REPR | Ativa |
| [ADR-005](./adr-005-result-pattern.md) | Result Pattern via CSharpFunctionalExtensions para Erros de Negócio | Ativa |
| [ADR-006](./adr-006-aggregates-domain-events.md) | Aggregates com Factory Method Estático e Domain Events | Ativa |
| [ADR-007](./adr-007-autofac-modulos.md) | Autofac como Container de DI com Módulos por Domínio | Ativa |
| [ADR-008](./adr-008-workflowcore-sagas.md) | WorkflowCore para Orquestração de Sagas com Compensação | Ativa |
| [ADR-009](./adr-009-silverback-kafka.md) | Silverback + Kafka para Mensageria Assíncrona | Ativa |
| [ADR-010](./adr-010-flurl-polly-http.md) | Flurl + Polly para Chamadas HTTP Externas com Retry | Ativa |
| [ADR-011](./adr-011-efcore-dapper.md) | EF Core (Npgsql) para Writes e Dapper para Reads | Ativa |
| [ADR-012](./adr-012-multitenancy-header.md) | Multi-tenancy via Header HTTP com Resolução em Middleware | Ativa |
| [ADR-013](./adr-013-serilog.md) | Serilog para Logging Estruturado | Ativa |
| [ADR-024](./adr-024-cancellation-token-flow.md) | Fluxo de CancellationToken — Endpoint → Handler.HandleAsync → Repository/UnitOfWork | Ativa |
| [ADR-025](./adr-025-fastendpoints-status-codes-error-payload.md) | Status Codes e Payload de Erro em FastEndpoints (`AddError` + `SendErrorsAsync` + `SendNotFoundAsync`) | Ativa |
| [ADR-026](./adr-026-composicao-handler-adapter-workflow-step.md) | Composição em Modernized — Handler vs Adapter vs WorkflowCore Step | Ativa |
| [ADR-027](./adr-027-tenant-access-handler.md) | Acesso a Tenant em Handler via Factory (sem `tenantId` explícito) | Ativa |
| [ADR-028](./adr-028-orchestration-mediator-legacy-modernized.md) | Orchestration como Mediator Pattern entre Legacy e Modernized (Routing + Data Integration) | Ativa |

### Grupo C — Qualidade e Processo (modernized/)

| # | Título | Status |
|---|--------|--------|
| [ADR-014](./adr-014-xunit-fluentassertions-moq.md) | xUnit + FluentAssertions + Moq como Stack de Testes | Ativa |
| [ADR-015](./adr-015-testcontainers.md) | TestContainers (PostgreSQL + Kafka) para Testes de Integração com Infra Real | Ativa |
| [ADR-016](./adr-016-estrutura-testes.md) | Estrutura de Testes Espelhando a Estrutura do Domínio | Ativa |

---

## Projeto legacy/

### Grupo D — Estrutura e Organização (legacy/)

| # | Título | Status |
|---|--------|--------|
| [ADR-017](./adr-017-legacy-layered-architecture.md) | Layered Architecture — Api / Application / Domain / Infrastructure | Ativa |

### Grupo E — Engenharia e Padrões de Código (legacy/)

| # | Título | Status |
|---|--------|--------|
| [ADR-018](./adr-018-legacy-mvc-controllers.md) | ASP.NET MVC Controllers como Framework HTTP | Ativa |
| [ADR-019](./adr-019-legacy-interfaces-servicos-repositorios.md) | Interface para Todo Serviço e Repositório | Ativa |
| [ADR-020](./adr-020-legacy-excecoes-erros-negocio.md) | Exceções (InvalidOperationException) como Mecanismo de Erros de Negócio | Ativa |
| [ADR-021](./adr-021-legacy-efcore-savechanges-por-operacao.md) | EF Core com SaveChangesAsync por Operação em Repositório (sem UnitOfWork) | Ativa |
| [ADR-022](./adr-022-legacy-worker-service-background.md) | Worker Service (.NET IHostedService) para Tarefas em Background | Ativa |

### Grupo F — Qualidade e Processo (legacy/)

| # | Título | Status |
|---|--------|--------|
| [ADR-023](./adr-023-legacy-xunit-testcontainers-mssql.md) | xUnit + TestContainers.MsSql + Moq nos Testes de Integração ⚠️ banco de teste ≠ produção | Ativa |
