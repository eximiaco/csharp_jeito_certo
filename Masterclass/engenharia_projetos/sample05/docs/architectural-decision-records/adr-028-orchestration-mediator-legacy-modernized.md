# ADR-028: Orchestration como Mediator Pattern entre Legacy e Modernized (Routing + Data Integration)

> **Escopo:** exclusivamente `modernized/GymErp/Domain/Orchestration/` — refina [ADR-001](./adr-001-separacao-legacy-modernized.md), [ADR-002](./adr-002-dominios-por-contexto.md) e [ADR-008](./adr-008-workflowcore-sagas.md).

## Contexto

ADR-001 estabelece migração incremental entre `legacy/` e `modernized/`. Durante a migração, surge demanda recorrente para:

- (a) **Routing**: a mesma funcionalidade existe nos dois sistemas e a escolha depende de regra de negócio (ex: plano `Mensal` ainda é processado no legado; demais planos no modernized).
- (b) **Data Integration**: um fluxo modernized precisa criar/atualizar dados também no legado (ex: matrícula criada em modernized precisa replicar para o legado para que módulos legados de cobrança continuem funcionando).

Sem regra clara, cada contexto modernized (`Subscriptions`, `Financial`, etc.) pode acabar fazendo chamada HTTP direta para legacy — espalhando conhecimento de migração por todo lugar e travando o cutover.

Aplica-se a toda feature de `modernized/GymErp/Domain/` que cruza a fronteira legacy↔modernized.

Evidência:

- `Domain/Orchestration/Features/EnrollmentOrchestrator/Handler.cs:20` — decisão de rota: `var useLegacySystem = plan.Type == PlanType.Mensal;`. Despacha para `LegacyAdapter.ProcessEnrollmentAsync` (Flurl HTTP para legacy) ou `ModernizedAdapter.ProcessEnrollmentAsync` (inicia WorkflowCore).
- `Domain/Orchestration/Features/EnrollmentOrchestrator/LegacyAdapter.cs:47-51` — chamada HTTP: `await _configuration.BaseUrl.AppendPathSegment("api/enrollment/enroll").PostJsonAsync(...)`.
- `Domain/Orchestration/Features/NewEnrollmentFlow/MainWorkflow.cs:14-23` — saga: `StartWith<AddEnrollmentStep>` (modernized) → `Then<AddLegacyEnrollmentStep>` (HTTP para legacy) → `Then<ProcessPaymentStep>` → `Then<ScheduleEvaluationStep>`, com compensação em cada passo.
- `Domain/Orchestration/Features/NewEnrollmentFlow/Steps/AddLegacyEnrollmentStep.cs:22-30` — Step que chama legacy via HTTP com Polly retry e lança exception em falha (dispara compensação WorkflowCore).
- Pasta `Domain/Orchestration/Infrastructure/OrchestrationModule.cs` — Autofac module registra todos os colaboradores da Orchestration.

Motivação: sem mediator centralizado, cada contexto modernized precisa conhecer endpoints, autenticação, retry e fallback do legado — anti-DRY, dificulta retirar o legado quando a migração completar.

**Alternativas consideradas:**
1. Cada contexto faz chamada HTTP direta para legacy quando precisa — rejeitada por espalhar conhecimento de migração.
2. Biblioteca compartilhada `GymErp.Common.LegacyClient` — rejeitada por virar acoplamento global; toda feature passaria a depender da biblioteca.
3. **Bounded context dedicado `Orchestration/` como mediator** — **escolhida**, isola toda integração legacy↔modernized em um lugar; quando legacy for descontinuado, basta deletar `Orchestration/`.

## Decisão

Adotamos `modernized/GymErp/Domain/Orchestration/` como **único bounded context autorizado a integrar legacy e modernized**. Toda feature que (a) decide entre legacy e modernized, ou (b) precisa orquestrar dados entre os dois sistemas, vive em `Orchestration/Features/`.

Outros contextos modernized (`Subscriptions`, `Financial`, `Scheduling`, etc.) **não fazem** chamadas HTTP para `legacy/`. Se um contexto precisa de dado do legado, expõe a operação como feature de `Orchestration/` que faz a integração, e o contexto consome o resultado.

Dois sub-padrões coexistem dentro de `Orchestration/`:

### Sub-padrão A — Mediator simples (Handler + Adapters)

Use quando a decisão é **routing puro** (escolher legacy ou modernized) sem multi-step. Estrutura:

```
Domain/Orchestration/Features/{Feature}/
├── Endpoint.cs                # POST /api/orchestrator/{recurso}
├── Handler.cs                 # decide rota baseado em regra de negócio
├── Request.cs, Response.cs
├── LegacyAdapter.cs           # Flurl HTTP → legacy API
├── ModernizedAdapter.cs       # chamada interna OU inicia WorkflowCore
├── {Auxiliar}Service.cs       # consultas auxiliares HTTP (ex: PlanService)
└── Configuration.cs           # IOptions<XxxApiConfiguration>
```

Template real: `EnrollmentOrchestrator/`.

### Sub-padrão B — Saga com integração legacy (WorkflowCore)

Use quando o fluxo é **multi-step com compensação E precisa tocar legacy** em pelo menos um passo. Estrutura:

```
Domain/Orchestration/Features/{Flow}/
├── Endpoint.cs                # POST /api/orchestrator/{recurso}/flow
├── {Flow}Data.cs              # estado da saga
├── MainWorkflow.cs            # IWorkflow<TData> com .Saga(...)
└── Steps/
    ├── {Step}Step.cs          # StepBodyAsync — pode chamar legacy via HTTP
    └── {Step}CompensationStep.cs
```

Template real: `NewEnrollmentFlow/` (saga com `AddLegacyEnrollmentStep` chamando legacy via HTTP + compensação).

## Regras Operacionais

1. **SEMPRE** features que cruzam fronteira legacy↔modernized vivem em `modernized/GymErp/Domain/Orchestration/Features/`. *Padrão detectável:* feature em qualquer contexto ≠ `Orchestration` que importa de `legacy/` ou faz HTTP para URL do legacy. *Motivo:* concentra conhecimento de migração em um lugar; legacy pode ser desligado deletando `Orchestration/`.
2. **NUNCA** faça chamada HTTP para legacy a partir de outro contexto modernized. *Padrão detectável:* `Flurl` ou `HttpClient` apontando para `LegacyApiConfiguration` em `Domain/{X}/` onde `X != Orchestration`. *Motivo:* viola o princípio de mediator único.
3. **SEMPRE** endpoints de Orchestration usam rota `/api/orchestrator/{recurso}[/{flow}]` e `Tags("Orchestration")`. *Padrão detectável:* `Post("/api/orchestrator/...")` em `Domain/Orchestration/Features/**/Endpoint.cs`. *Motivo:* contrato HTTP explicita que aquela rota cruza sistemas.
4. **SEMPRE** Adapter encapsula chamada externa via Flurl + Polly (ADR-010) e traduz `FlurlHttpException` em `Result.Failure<T>` com mensagem pt-BR. *Padrão detectável:* `try { ... PostJsonAsync ... } catch (FlurlHttpException ex) { return Result.Failure<T>(...); }` em `*Adapter.cs`. *Motivo:* domínio não lida com exceção HTTP; recebe `Result`.
5. **SEMPRE** todo Step de saga que modifica estado em legacy tem `CompensationStep` correspondente registrado no `MainWorkflow`. *Padrão detectável:* `.Then<XLegacyStep>()` sem `.CompensateWith<XLegacyCompensationStep>()` em `MainWorkflow.cs`. *Motivo:* falha em passo posterior precisa reverter efeito no legado para evitar inconsistência cross-system.
6. **NUNCA** chame Handler ou Service de outro contexto modernized a partir de Orchestration. *Padrão detectável:* `using GymErp.Domain.{X}` (X ≠ Orchestration ≠ Subscriptions.Aggreates.Plans para `PlanType`) em `Domain/Orchestration/Features/**/`. *Motivo:* mesmo em modernized, Orchestration chama via HTTP/WorkflowCore para manter desacoplamento (exceção: pode importar tipos de Aggregate como `PlanType` para regra de decisão).
6a. **PODE** iniciar uma Saga WorkflowCore irmã (outra feature dentro de `Orchestration/`) a partir de Adapter via `IWorkflowHost.StartWorkflow("nome-do-workflow", data)`. *Padrão detectável:* `_workflowHost.StartWorkflow(...)` em `*Adapter.cs`. *Motivo:* essa é exatamente a integração prevista entre Sub-padrão A (Mediator) e Sub-padrão B (Saga) — `ModernizedAdapter` em `EnrollmentOrchestrator/` inicia `NewEnrollmentFlow` saga (ver `ModernizedAdapter.cs:48`). Não conflita com regra 6 porque ambas features vivem em `Orchestration/`.
7. **PREFIRA** Sub-padrão A (Mediator) quando a decisão é roteamento sem multi-step. **PREFIRA** Sub-padrão B (Saga) quando há multi-step com necessidade de compensação. *Motivo:* WorkflowCore tem custo (estado persistente, mais infra) — não use para fluxo simples.
8. **SEMPRE** configure `LegacyApiConfiguration`/`SubscriptionsApiConfiguration` via `IOptions<T>` em `Configuration.cs` da feature. *Padrão detectável:* `IOptions<XxxConfiguration>` em construtor de Adapter. *Motivo:* permite swap de endpoint por ambiente sem recompilar.

## Exemplos

### ✅ DO

```csharp
// Sub-padrão A: Mediator com Handler + Adapters
// Domain/Orchestration/Features/EnrollmentOrchestrator/Handler.cs
public class Handler(LegacyAdapter legacy, ModernizedAdapter modernized, PlanService plans)
{
    public async Task<Result<Response>> HandleAsync(Request request, CancellationToken ct)
    {
        var planResult = await plans.GetPlanByIdAsync(request.PlanId, ct);
        if (planResult.IsFailure)
            return Result.Failure<Response>(planResult.Error);

        var useLegacy = planResult.Value.Type == PlanType.Mensal;
        var enrollment = useLegacy
            ? await legacy.ProcessEnrollmentAsync(request, ct)
            : await modernized.ProcessEnrollmentAsync(request, ct);

        return enrollment.Map(id => new Response(id, useLegacy ? "Legacy" : "Modernized"));
    }
}

// Sub-padrão B: Saga com Step que chama legacy
// Domain/Orchestration/Features/NewEnrollmentFlow/MainWorkflow.cs
public class MainWorkflow : IWorkflow<NewEnrollmentFlowData>
{
    public void Build(IWorkflowBuilder<NewEnrollmentFlowData> builder)
    {
        builder
            .Saga(saga => saga
                .StartWith<AddEnrollmentStep>()
                    .CompensateWith<AddEnrollmentCompensationStep>()
                .Then<AddLegacyEnrollmentStep>()                    // chama legacy via HTTP
                    .CompensateWith<AddLegacyEnrollmentCompensationStep>()
                .Then<ProcessPaymentStep>()
                    .CompensateWith<ProcessPaymentCompensationStep>())
            .OnError(WorkflowErrorHandling.Retry, TimeSpan.FromSeconds(30));
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: feature de Subscriptions chamando legacy direto. Espalha conhecimento de migração.
// Domain/Subscriptions/Features/AddNewEnrollment/Handler.cs
public class Handler(EnrollmentRepository repo, IUnitOfWork uow, IOptions<LegacyApiConfiguration> legacy) // ❌
{
    public async Task<Result<Guid>> HandleAsync(Request req, CancellationToken ct)
    {
        // ...
        await legacy.Value.BaseUrl.AppendPathSegment("api/enrollment").PostJsonAsync(...); // ❌
    }
}
```

## Estrutura de Arquivos

```
modernized/GymErp/Domain/Orchestration/
├── Infrastructure/
│   └── OrchestrationModule.cs                          # Autofac registrations
└── Features/
    ├── EnrollmentOrchestrator/                         # Sub-padrão A
    │   ├── Endpoint.cs, Handler.cs
    │   ├── Request.cs, Response.cs
    │   ├── LegacyAdapter.cs, ModernizedAdapter.cs
    │   ├── PlanService.cs, Configuration.cs
    ├── NewEnrollmentFlow/                              # Sub-padrão B
    │   ├── Endpoint.cs, Request.cs
    │   ├── NewEnrollmentFlowData.cs
    │   ├── MainWorkflow.cs
    │   └── Steps/
    │       ├── AddEnrollmentStep.cs
    │       ├── AddEnrollmentCompensationStep.cs
    │       ├── AddLegacyEnrollmentStep.cs              # ← integração com legacy
    │       ├── AddLegacyEnrollmentCompensationStep.cs
    │       └── ...
    └── CancelEnrollmentFlow/                           # Sub-padrão B
        └── ...
```

## Comparação de Abordagens

| Critério | Mediator (A) | Saga (B) |
|---|---|---|
| Multi-step | ❌ (1 step) | ✅ |
| Compensação automática | ❌ (manual) | ✅ (WorkflowCore) |
| Estado persistente entre steps | ❌ | ✅ (WorkflowCore store) |
| Custo de infra | Baixo | Médio (WorkflowCore precisa store) |
| Quando usar | Routing puro | Multi-step + compensação |

## Benefícios

- **Migração reversível** — quando legacy for descontinuado, deletar `Orchestration/` remove toda a ponte; nenhum outro contexto precisa mudar.
- **Conhecimento concentrado** — endpoints, autenticação, retry e fallback do legado vivem em um único lugar.
- **Testes focados** — testes de integração de Orchestration validam o contrato legacy↔modernized; outros contextos não precisam mockar legacy.
- **Contrato HTTP explícito** — rota `/api/orchestrator/...` deixa claro para o cliente que aquela operação pode acionar legacy.

## Riscos e Limitações

- **Risco**: feature urgente pode ser tentada a fazer chamada direta para legacy "só dessa vez". *Mitigação:* code review obrigatório verifica regra 1; CI grep do anti-padrão (regra 2).
- **Risco**: WorkflowCore (Sub-padrão B) exige store persistente; quebra de schema do `{Flow}Data.cs` invalida sagas em andamento. *Mitigação:* versionar workflow (`Version => 2`) ao mudar shape do Data.
- **Risco**: Adapter pode duplicar lógica de retry/timeout. *Mitigação:* usar `Polly.HttpRetryPolicy` compartilhada (ver `Common/`).

## Como Verificar Conformidade

- **Grep de chamada externa fora de Orchestration**: `rg "LegacyApiConfiguration|api/enrollment/enroll" modernized/GymErp/Domain/ | rg -v "Orchestration"`.
- **Grep de Step sem compensação**: ler cada `MainWorkflow.cs` em `Domain/Orchestration/Features/*/`.
- **Checklist manual**: toda nova rota `/api/orchestrator/...` tem Tag `Orchestration` no Endpoint; todo Adapter retorna `Result<T>`.

## Próximos Passos

1. Auditar `Domain/Subscriptions/`, `Domain/Financial/`, `Domain/Scheduling/` para detectar chamadas HTTP a legacy fora de Orchestration.
2. Adicionar grep do anti-padrão no CI como check de não-regressão.
3. Documentar no `modernized/CLAUDE.md` §13 (já feito).

## Relacionado

- [ADR-001](./adr-001-separacao-legacy-modernized.md): separação legacy/modernized — esta ADR define a ponte entre elas.
- [ADR-002](./adr-002-dominios-por-contexto.md): Orchestration é um bounded context dedicado.
- [ADR-008](./adr-008-workflowcore-sagas.md): WorkflowCore usado no Sub-padrão B.
- [ADR-010](./adr-010-flurl-polly-http.md): Adapters usam Flurl + Polly.
- [ADR-026](./adr-026-composicao-handler-adapter-workflow-step.md): taxonomia Handler/Adapter/Step que esta ADR aplica ao caso legacy↔modernized.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
