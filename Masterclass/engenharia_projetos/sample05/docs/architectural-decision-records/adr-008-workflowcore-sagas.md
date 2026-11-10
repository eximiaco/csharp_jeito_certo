# ADR-008: WorkflowCore para Orquestração de Sagas com Compensação

> **Escopo:** exclusivamente `modernized/GymErp/Domain/Orchestration/` — **não se aplica** ao código em `legacy/`.

## Contexto

O processo de nova matrícula envolve múltiplos passos distribuídos (criar matrícula, adicionar ao sistema legado, processar pagamento, agendar avaliação) que precisam de compensação em caso de falha parcial. Aplica-se ao contexto `Orchestration` em `modernized/GymErp/Domain/Orchestration/`.

Evidência: `NewEnrollmentFlow/MainWorkflow.cs` implementa `IWorkflow<NewEnrollmentFlowData>` e usa `.Saga().StartWith<AddEnrollmentStep>().CompensateWith<AddEnrollmentCompensationStep>()`. Passos em `Steps/` implementam `StepBody`. PackageReference `WorkflowCore 3.13.0`.

**Alternativas consideradas:**
1. Saga manual com `try/catch` e rollback explícito — rejeitado pela dificuldade de manter compensações consistentes em fluxos longos.
2. MassTransit Saga — rejeitado por adicionar dependência extra e complexidade de state machine.
3. WorkflowCore — **escolhido** por suporte nativo a saga com `.CompensateWith<>()`, integração com DI e sem necessidade de infraestrutura extra de mensageria para orquestração local.

## Decisão

Adotamos WorkflowCore para orquestração de sagas no contexto Orchestration. Cada fluxo é um `IWorkflow<TData>` com dados tipados em `{Flow}Data`. Cada passo é um `StepBody` em pasta `Steps/`. Compensações são `StepBody` separados registrados com `.CompensateWith<>()`. O host do workflow é iniciado em `Program.cs` e os workflows são registrados antes do `app.Run()`.

## Regras Operacionais

1. **SEMPRE** implemente cada passo do workflow como `StepBody` separado em `Features/{Flow}/Steps/`. *Padrão detectável:* `class *Step : StepBody` em `Steps/`. *Motivo:* passos isolados são testáveis individualmente.
2. **SEMPRE** crie um `*CompensationStep` correspondente para cada passo que modifica estado externo. *Padrão detectável:* `*CompensationStep.cs` ao lado de `*Step.cs`. *Motivo:* toda operação reversível precisa de compensação explícita.
3. **SEMPRE** use `.Saga()` no `Build()` do workflow quando houver compensação. *Padrão detectável:* `.Saga(saga =>` em `MainWorkflow.cs`. *Motivo:* o contexto de saga gerencia o rollback automático dos passos compensáveis.
4. **SEMPRE** tipifique os dados do workflow em uma classe `{Flow}Data`. *Padrão detectável:* `IWorkflow<{Flow}Data>` e `class {Flow}Data` em `Features/{Flow}/`. *Motivo:* dados fortemente tipados evitam uso de `Dictionary<string, object>`.
5. **SEMPRE** registre o workflow no `Program.cs` antes de `app.Run()`. *Padrão detectável:* `host?.RegisterWorkflow<MainWorkflow, FlowData>()` em `Program.cs`. *Motivo:* WorkflowCore exige registro explícito antes de iniciar o host.

## Exemplos

### ✅ DO

```csharp
// modernized/GymErp/Domain/Orchestration/Features/NewEnrollmentFlow/MainWorkflow.cs
public class MainWorkflow : IWorkflow<NewEnrollmentFlowData>
{
    public string Id => "new-enrollment-workflow";
    public int Version => 1;

    public void Build(IWorkflowBuilder<NewEnrollmentFlowData> builder)
    {
        builder
            .Saga(saga => saga
                .StartWith<AddEnrollmentStep>()
                    .CompensateWith<AddEnrollmentCompensationStep>()
                .Then<AddLegacyEnrollmentStep>()
                    .CompensateWith<AddLegacyEnrollmentCompensationStep>()
                .Then<ProcessPaymentStep>()
                    .CompensateWith<ProcessPaymentCompensationStep>()
                .Then<ScheduleEvaluationStep>()
                    .CompensateWith<ScheduleEvaluationCompensationStep>())
            .OnError(WorkflowErrorHandling.Retry, TimeSpan.FromSeconds(30));
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: compensação manual com try/catch aninhado e sem passos isolados
public async Task ProcessNewEnrollmentAsync(Request req)
{
    Guid? enrollmentId = null;
    try
    {
        enrollmentId = await AddEnrollmentAsync(req);
        await AddLegacyEnrollmentAsync(req);
        await ProcessPaymentAsync(req);
    }
    catch
    {
        if (enrollmentId.HasValue) await CancelEnrollmentAsync(enrollmentId.Value);
        throw;
    }
}
```

## Estrutura de Arquivos

```
Domain/Orchestration/Features/NewEnrollmentFlow/
├── MainWorkflow.cs
├── NewEnrollmentFlowData.cs
├── Endpoint.cs
├── Request.cs
└── Steps/
    ├── AddEnrollmentStep.cs
    ├── AddEnrollmentCompensationStep.cs
    ├── ProcessPaymentStep.cs
    ├── ProcessPaymentCompensationStep.cs
    ├── ScheduleEvaluationStep.cs
    └── ScheduleEvaluationCompensationStep.cs
```

## Benefícios

- **Compensação declarativa** — `.CompensateWith<>()` deixa o rollback explícito no mesmo lugar que o passo.
- **Passos testáveis** — cada `StepBody` é uma classe injetável por DI.
- **Resiliência** — `OnError(Retry)` reprocessa automaticamente falhas transitórias.

## Relacionado

- [ADR-002](./adr-002-dominios-por-contexto.md): orquestração reside em `Domain/Orchestration/`.
- [ADR-007](./adr-007-autofac-modulos.md): passos registrados em `OrchestrationModule`.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
