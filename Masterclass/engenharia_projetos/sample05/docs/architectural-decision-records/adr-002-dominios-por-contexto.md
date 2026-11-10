# ADR-002: Domínios Organizados por Contexto sob Domain/

> **Escopo:** exclusivamente `modernized/GymErp/` — **não se aplica** ao código em `legacy/`.

## Contexto

O projeto `modernized/GymErp` precisa de uma estrutura de pastas que expresse explicitamente os bounded contexts sem impor camadas horizontais (Controllers/, Services/, Repositories/). Aplica-se a todo código em `modernized/GymErp/`.

Evidência: `modernized/GymErp/Domain/` contém `Subscriptions/`, `Orchestration/` e `Financial/`, cada um com sub-pastas `Features/`, `Aggreates/` (sic) e `Infrastructure/`.

## Decisão

Adotamos a organização: `Domain/{Contexto}/` como raiz de cada bounded context. Cada contexto tem três sub-pastas fixas:
- `Features/` — casos de uso (um subdiretório por feature).
- `Aggreates/` — entidades de domínio, value objects, repositórios.
- `Infrastructure/` — módulo Autofac, DbContext, adaptadores de infraestrutura do contexto.

Código transversal (interfaces de IUnitOfWork, IServiceBus, tenant, etc.) reside em `Common/`.

## Regras Operacionais

1. **SEMPRE** crie novos contextos de domínio como `Domain/{NomeContexto}/` com as três sub-pastas. *Padrão detectável:* `modernized/GymErp/Domain/`. *Motivo:* facilita localização e delimita responsabilidades.
2. **NUNCA** coloque lógica de feature diretamente em `Domain/{Contexto}/` sem passar pela sub-pasta `Features/`. *Padrão detectável:* arquivos `Handler.cs` ou `Endpoint.cs` soltos em `Domain/{Contexto}/`. *Motivo:* mantém a estrutura Vertical Slice previsível.
3. **SEMPRE** coloque o módulo Autofac do contexto em `Domain/{Contexto}/Infrastructure/{Contexto}Module.cs`. *Padrão detectável:* `*Module.cs` em `Infrastructure/`. *Motivo:* bootstrap centralizado em `Program.cs` espera este padrão.
4. **NUNCA** referencie namespace de outro contexto dentro de `Features/` ou `Aggreates/` sem passar por uma abstração em `Common/`. *Motivo:* preserva boundaries entre bounded contexts.

## Estrutura de Arquivos

```
modernized/GymErp/
├── Common/                        ← interfaces e infra transversal
│   └── Infrastructure/
│       └── CommonModule.cs
├── Domain/
│   ├── Subscriptions/
│   │   ├── Aggreates/Enrollments/
│   │   ├── Features/AddNewEnrollment/
│   │   └── Infrastructure/SubscriptionsModule.cs
│   ├── Orchestration/
│   │   ├── Features/NewEnrollmentFlow/
│   │   └── Infrastructure/OrchestrationModule.cs
│   └── Financial/
│       ├── Features/ProcessCharging/
│       └── Infrastructure/FinancialModule.cs
├── Tenant/
└── Program.cs
```

## Benefícios

- **Navegação intuitiva** — dado o contexto, encontra-se qualquer artefato em ≤3 cliques.
- **Fronteiras explícitas** — impossível usar um repositório de outro contexto sem intencionalidade.
- **Módulos Autofac independentes** — cada contexto registra seus próprios tipos.

## Relacionado

- [ADR-001](./adr-001-separacao-legacy-modernized.md): escopo desta ADR é apenas `modernized/`.
- [ADR-004](./adr-004-vertical-slice.md): estrutura interna de cada Feature.
- [ADR-007](./adr-007-autofac-modulos.md): módulos Autofac por contexto.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
