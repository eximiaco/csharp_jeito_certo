# ADR-016: Estrutura de Testes Espelhando a Estrutura do Domínio

> **Escopo:** exclusivamente `modernized/GymErp.UnitTests/` e `modernized/GymErp.IntegrationTests/` — **não se aplica** a `legacy/`.

## Contexto

Os projetos `GymErp.UnitTests` e `GymErp.IntegrationTests` precisam de uma convenção de organização de pastas e nomenclatura que torne fácil localizar os testes de uma feature e entender o que está sendo testado. Aplica-se a toda a estrutura de pastas dos projetos de teste em `modernized/`.

Evidência: `GymErp.IntegrationTests/Subscriptions/AddNewEnrollment/HandlerTests.cs` e `CancelEnrollment/HandlerTests.cs` espelham `Domain/Subscriptions/Features/AddNewEnrollment/` e `Features/CancelEnrollment/`. Da mesma forma, `GymErp.UnitTests/Orchestration/EnrollmentOrchestrator/HandlerTests.cs` espelha `Domain/Orchestration/Features/EnrollmentOrchestrator/`.

## Decisão

Adotamos a convenção de que a estrutura de pastas nos projetos de teste espelha a estrutura de `Domain/` no projeto principal. O namespace do teste é `GymErp.{Tipo}Tests.{Contexto}.{Feature}`. Arquivos de teste são nomeados `{Artefato}Tests.cs` (ex.: `HandlerTests.cs`, `EndpointTests.cs`, `WorkflowTests.cs`). Helpers de setup específicos de uma feature ficam em `{Feature}/TestHelpers/`.

## Regras Operacionais

1. **SEMPRE** crie a pasta de teste sob o mesmo caminho do domínio: `{Contexto}/{Feature}/` em vez de pastas planas. *Padrão detectável:* `Subscriptions/AddNewEnrollment/HandlerTests.cs` espelhando `Domain/Subscriptions/Features/AddNewEnrollment/Handler.cs`. *Motivo:* localização trivial — dado o artefato de produção, o teste está na mesma hierarquia.
2. **SEMPRE** nomeie o arquivo de teste como `{Artefato}Tests.cs`. *Padrão detectável:* `HandlerTests.cs`, `EndpointTests.cs`, `WorkflowTests.cs`. *Motivo:* indica exatamente o que está sendo testado sem ambiguidade.
3. **SEMPRE** coloque helpers de setup de dados em `{Feature}/TestHelpers/TestDataBuilder.cs`. *Padrão detectável:* `TestHelpers/TestDataBuilder.cs` ao lado dos testes de uma feature. *Motivo:* separa a construção de dados de teste da lógica de asserção.
4. **NUNCA** coloque múltiplas features na mesma classe de teste. *Padrão detectável:* métodos com prefixo de features diferentes em uma classe `*Tests.cs`. *Motivo:* viola o princípio de responsabilidade única nos testes.

## Estrutura de Arquivos

```
GymErp.IntegrationTests/
├── Infrastructure/
│   ├── IntegrationTestBase.cs
│   └── EfDbContextAccessor.cs
├── Subscriptions/
│   ├── AddNewEnrollment/
│   │   └── HandlerTests.cs
│   ├── CancelEnrollment/
│   │   └── HandlerTests.cs
│   └── EnrollmentStateTests.cs
├── Orchestration/
│   ├── NewEnrollmentFlow/
│   │   ├── SimpleWorkflowTests.cs
│   │   ├── EndpointTests.cs
│   │   ├── TestHelpers/TestDataBuilder.cs
│   │   └── StepTests/AddLegacyEnrollmentStepTests.cs
│   ├── EnrollmentOrchestrator/
│   │   ├── HandlerTests.cs
│   │   ├── EndpointTests.cs
│   │   └── ModernizedAdapterTests.cs
│   └── CancelEnrollmentFlow/
│       └── WorkflowTests.cs
└── Financial/
    └── ProcessCharging/
        └── HandlerTests.cs

GymErp.UnitTests/
├── Subscriptions/
│   └── EnrollmentTests.cs
└── Orchestration/
    └── EnrollmentOrchestrator/
        ├── HandlerTests.cs
        ├── EndpointTests.cs
        ├── LegacyAdapterTests.cs
        └── ModernizedAdapterTests.cs
```

## Padrões Estabelecidos

### Nomenclatura de métodos de teste

- **Formato:** `{Método}_{Cenário}_{ResultadoEsperado}`
- **Exemplo:** `HandleAsync_WithValidRequest_ShouldCreateEnrollment`
- **Exemplo:** `Create_WithInvalidCpf_ShouldReturnFailure`

## Benefícios

- **Navegação trivial** — de `Handler.cs` no domínio, jump para `HandlerTests.cs` no mesmo caminho relativo.
- **Cobertura visível** — ausência de pasta de teste revela feature sem cobertura.
- **Contexto preservado** — `TestHelpers/` próximo ao teste que usa.

## Relacionado

- [ADR-014](./adr-014-xunit-fluentassertions-moq.md): stack de testes.
- [ADR-015](./adr-015-testcontainers.md): base de integração em `Infrastructure/`.
- [ADR-004](./adr-004-vertical-slice.md): estrutura de domínio que os testes espelham.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
