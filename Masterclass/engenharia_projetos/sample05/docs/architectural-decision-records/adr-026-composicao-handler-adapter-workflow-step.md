# ADR-026: Composição em Modernized — Handler vs Adapter vs WorkflowCore Step

> **Escopo:** exclusivamente `modernized/GymErp/` — refina [ADR-008](./adr-008-workflowcore-sagas.md) e [ADR-004](./adr-004-vertical-slice.md).

## Contexto

Vertical Slice (ADR-004) coloca cada feature em pasta isolada com `Endpoint/Handler/Request/Response`. Mas alguns fluxos precisam compor múltiplas operações: roteamento entre sistemas, chamadas HTTP a outros bounded contexts, multi-step com compensação. Sem regra clara de quando criar Handler, Adapter ou WorkflowCore Step, código novo tende a:

- (a) Handler chamando outro Handler diretamente (acoplamento entre features quebra Vertical Slice).
- (b) Lógica de orquestração espalhada em vários Handlers em vez de centralizada.
- (c) Saga inventada à mão com `try/catch` em vez de usar WorkflowCore.

Aplica-se a toda feature em `modernized/GymErp/Domain/**/Features/`.

Evidência:

- `Domain/Orchestration/Features/EnrollmentOrchestrator/Handler.cs:6-43` — Handler que orquestra 3 colaboradores (`LegacyAdapter`, `ModernizedAdapter`, `PlanService`), com decisão de rota baseada em `PlanType`. Padrão Mediator simples.
- `Domain/Orchestration/Features/NewEnrollmentFlow/MainWorkflow.cs:12-25` — Saga WorkflowCore com `StartWith<AddEnrollmentStep>().CompensateWith<...>().Then<...>()`. Padrão Saga.
- `Domain/Orchestration/Features/EnrollmentOrchestrator/LegacyAdapter.cs:17-64` — Adapter que encapsula chamada HTTP a sistema externo (legacy) via Flurl, traduz exception em `Result.Failure`.

**Alternativas consideradas:**
1. Handler chama outro Handler via DI — rejeitada por acoplar features e quebrar Vertical Slice (ADR-004).
2. MediatR como bus interno — rejeitada por ADR-003 (sem MediatR).
3. **Adapter para chamada externa + WorkflowCore para multi-step com compensação + Handler só para lógica da própria feature** — **escolhida**.

## Decisão

Adotamos a seguinte taxonomia de colaboradores para composição em modernized:

- **Handler** (`Features/{X}/Handler.cs`): contém **apenas** lógica da própria feature. Pode chamar repository, UoW, serviceBus, aggregate factory. **Nunca** chama outro Handler de outra feature.
- **Adapter** (`Features/{X}/{Nome}Adapter.cs`, vive na própria feature de Orchestration): encapsula chamada a sistema externo via HTTP (Flurl) ou a outro bounded context. Traduz exceção em `Result<T>`. Não tem estado.
- **Serviço auxiliar** (`Features/{X}/{Nome}Service.cs`, vive na própria feature): consulta auxiliar pontual via HTTP ou DB (ex: `PlanService.GetPlanByIdAsync`). Distinto de Adapter por não orquestrar uma operação de escrita — só lê dados.
- **WorkflowCore Step** (`Features/{X}/Steps/{Nome}Step.cs`): unidade atômica de uma saga. Lança exception em falha (WorkflowCore dispara compensação). **Nunca** retorna `Result<T>` — saga não interpreta Result.
- **Compensation Step** (`Features/{X}/Steps/{Nome}CompensationStep.cs`): reverte efeito do Step correspondente. Obrigatório para todo Step que modifica estado externo.

Decisão de qual usar:

| Cenário | Use |
|---|---|
| Lógica é da própria feature, sem chamada externa | Handler |
| Feature precisa decidir entre dois sistemas baseado em regra de negócio | Handler + Adapters (ex: `EnrollmentOrchestrator`) |
| Feature precisa de consulta auxiliar HTTP/DB | Serviço auxiliar (ex: `PlanService`) |
| Fluxo é multi-step (>2 operações sequenciais) com necessidade de rollback se passo intermediário falhar | WorkflowCore Saga com Steps + CompensationSteps |
| Feature de leitura simples | Endpoint pode acessar `IDbConnectionFactory` direto (Dapper), sem Handler |

## Regras Operacionais

1. **NUNCA** injete Handler de outra feature em um Handler. *Padrão detectável:* `using GymErp.Domain.{X}.Features.{Y}` dentro de `Domain/{X}/Features/{Z}/Handler.cs` (Z ≠ Y). *Motivo:* quebra Vertical Slice; cria acoplamento entre features.
2. **SEMPRE** coloque Adapters/Serviços auxiliares na pasta da própria feature de Orchestration que os usa. *Padrão detectável:* `LegacyAdapter.cs`/`{Nome}Service.cs` em `Domain/Orchestration/Features/{X}/`. *Motivo:* Vertical Slice — toda colaboração da feature vive junto.
3. **NUNCA** retorne `Result<T>` em WorkflowCore Step. *Padrão detectável:* `StepBodyAsync` que retorna algo diferente de `ExecutionResult`. *Motivo:* WorkflowCore propaga falha via exception; Result em Step não dispara compensação.
4. **SEMPRE** crie CompensationStep para todo Step que modifica estado externo (DB, API externa, evento publicado). *Padrão detectável:* `.Then<XStep>()` sem `.CompensateWith<XCompensationStep>()` em `MainWorkflow.cs`. *Motivo:* sem compensação, falha intermediária deixa sistema em estado inconsistente.
5. **PREFIRA** Adapter a chamada HTTP inline no Handler. *Motivo:* isola tradução exception→Result e configuração de timeout/retry em um lugar.
6. **NUNCA** chame Handler de feature A a partir de uma saga ou Adapter de feature B. *Padrão detectável:* `_handler.HandleAsync(...)` dentro de `Step` ou `Adapter`. *Motivo:* saga/adapter chama HTTP, repo, ou aggregate — não outra feature.

## Exemplos

### ✅ DO

```csharp
// Adapter: chamada externa encapsulada
// Domain/Orchestration/Features/EnrollmentOrchestrator/LegacyAdapter.cs
public class LegacyAdapter(IOptions<LegacyApiConfiguration> options)
{
    public async Task<Result<Guid>> ProcessEnrollmentAsync(Request request, CancellationToken ct)
    {
        try
        {
            var response = await options.Value.BaseUrl
                .AppendPathSegment("api/enrollment/enroll")
                .PostJsonAsync(legacyRequest, cancellationToken: ct)
                .ReceiveJson<LegacyEnrollmentResponse>();
            return Result.Success(response.EnrollmentId);
        }
        catch (FlurlHttpException ex)
        {
            return Result.Failure<Guid>($"Erro no sistema legado: {ex.Message}");
        }
    }
}

// Handler usando Adapter
public class Handler(LegacyAdapter legacy, ModernizedAdapter modernized, PlanService plans)
{
    public async Task<Result<Response>> HandleAsync(Request request, CancellationToken ct)
    {
        var plan = await plans.GetPlanByIdAsync(request.PlanId, ct);
        if (plan.IsFailure) return Result.Failure<Response>(plan.Error);

        return plan.Value.Type == PlanType.Mensal
            ? await legacy.ProcessEnrollmentAsync(request, ct).Map(id => new Response(id, "Legacy"))
            : await modernized.ProcessEnrollmentAsync(request, ct).Map(id => new Response(id, "Modernized"));
    }
}

// WorkflowCore Step com compensação
// Domain/Orchestration/Features/NewEnrollmentFlow/MainWorkflow.cs
builder.Saga(saga => saga
    .StartWith<AddEnrollmentStep>().CompensateWith<AddEnrollmentCompensationStep>()
    .Then<ProcessPaymentStep>().CompensateWith<ProcessPaymentCompensationStep>())
.OnError(WorkflowErrorHandling.Retry, TimeSpan.FromSeconds(30));
```

### ❌ DON'T

```csharp
// Por que está errado: Handler de Orchestration chamando Handler de Subscriptions direto. Acopla features, quebra Vertical Slice.
public class Handler(Subscriptions.Features.AddNewEnrollment.Handler subscriptionsHandler) // ❌
{
    public async Task<Result<Response>> HandleAsync(Request req, CancellationToken ct)
    {
        var result = await subscriptionsHandler.HandleAsync(req, ct); // ❌
        // ...
    }
}

// Por que está errado: Step retornando Result em vez de lançar exception. WorkflowCore não dispara compensação.
public class AddEnrollmentStep : StepBodyAsync
{
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext ctx)
    {
        var result = await SomeOp();
        if (result.IsFailure)
            return ExecutionResult.Next(); // ❌ saga continua como se tivesse sucesso
        return ExecutionResult.Next();
    }
}
```

## Padrões Estabelecidos

### Pasta de feature em Orchestration (sub-padrão Mediator)

```
Domain/Orchestration/Features/{Feature}/
├── Endpoint.cs
├── Handler.cs                  # decide rota
├── Request.cs, Response.cs
├── LegacyAdapter.cs            # chamada externa a legacy
├── ModernizedAdapter.cs        # chamada externa a modernized (ou inicia WorkflowCore)
├── {Auxiliar}Service.cs        # consultas auxiliares
└── Configuration.cs            # IOptions<XxxApiConfiguration>
```

### Pasta de feature em Orchestration (sub-padrão Saga)

```
Domain/Orchestration/Features/{Flow}/
├── Endpoint.cs                 # POST /api/orchestrator/{recurso}/flow
├── {Flow}Data.cs               # estado da saga (mutável durante execução)
├── MainWorkflow.cs             # IWorkflow<TData> com .Saga(...)
└── Steps/
    ├── {Step}Step.cs           # StepBodyAsync
    └── {Step}CompensationStep.cs
```

## Como Verificar Conformidade

- **Grep de Handler chamando Handler**: `rg "using GymErp\.Domain\..*\.Features\." modernized/GymErp/Domain/ --type cs` — filtrar matches onde feature do `using` ≠ feature do arquivo.
- **Grep de Step sem compensação**: ler `MainWorkflow.cs` de cada saga e conferir cada `.Then<...>` ter `.CompensateWith<...>`.
- **Checklist manual**: toda chamada HTTP externa em `Domain/Orchestration/Features/{X}/` está dentro de Adapter ou Step (não em Handler diretamente).

## Relacionado

- [ADR-004](./adr-004-vertical-slice.md): Vertical Slice — fundamenta a proibição de Handler→Handler entre features.
- [ADR-008](./adr-008-workflowcore-sagas.md): WorkflowCore para sagas — esta ADR detalha quando usar Step vs Handler.
- [ADR-010](./adr-010-flurl-polly-http.md): chamadas HTTP em Adapters usam Flurl + Polly.
- [ADR-028](./adr-028-orchestration-mediator-legacy-modernized.md): Orchestration é o contexto onde Adapters/Sagas vivem.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
