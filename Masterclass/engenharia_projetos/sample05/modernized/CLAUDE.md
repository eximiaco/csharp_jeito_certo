# GymErp `modernized/` — Harness operacional

Escopo: tudo em `modernized/`. Stack: .NET 9, FastEndpoints, CSharpFunctionalExtensions, EF Core (Npgsql) + Dapper, Autofac, Silverback+Kafka, WorkflowCore, Serilog. Testes: xUnit + FluentAssertions + Moq + TestContainers.

ADRs aplicáveis: **001, 002, 003, 004, 005, 006, 007, 008, 009, 010, 011, 012, 013, 014, 015, 016**.

---

## Playbook: nova feature em `modernized/GymErp/Domain/{Contexto}/Features/{NovaFeature}/`

Siga na ordem. Cite a ADR ao desviar.

### 1. Crie a pasta da feature (ADR-004 — Vertical Slice)
Estrutura mínima:
```
Features/{NovaFeature}/
├── Endpoint.cs       # FastEndpoints
├── Handler.cs        # class Handler (não prefixe com nome da feature)
├── Request.cs        # record (positional ou com init)
└── Response.cs       # record (omitir se retorno é só Guid/primitivo)
```
**Template fonte**: `Domain/Subscriptions/Features/AddNewEnrollment/` — copie a estrutura, **não** copie o construtor com `CancellationToken` (é bug, ver Cleanup §C1).

### 2. Handler retorna `Result<T>` (ADR-005)
- Sempre `Task<Result<T>>` ou `Task<Result>`. **Nunca** `Task<bool>`, `Task<T?>` ou `throw` para regra de negócio.
- `Result.Failure<T>("mensagem em pt-BR descritiva")` para falha.
- Falha de validação do `Request.IsValid()` → `Result.Failure<T>(...)`, não `return false`.
- Use `.Bind()` / `.Map()` ao compor múltiplas operações.

### 3. `CancellationToken` flui pelo método, não pelo construtor (decisão deste harness)
**Correto**:
```csharp
public class Handler(EnrollmentRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> HandleAsync(Request request, CancellationToken ct)
    {
        // ...
        await repository.AddAsync(enrollment, ct);
        await unitOfWork.Commit(ct);
        return Result.Success(enrollment.Id);
    }
}
```
**Errado** (bug ativo em handlers existentes — não copie):
```csharp
public class Handler(EnrollmentRepository repo, IUnitOfWork uow, CancellationToken cancellationToken) // ❌
```
Endpoint propaga: `await _handler.HandleAsync(req, ct)`.

### 4. Endpoint (ADR-003)

**Padrão prescrito para código NOVO** (varia do existente — ver Cleanup §C4):
```csharp
public class Endpoint(Handler handler) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/api/{recurso}");                // plural, kebab-case
        AllowAnonymous();                      // ou política explícita
        Summary(s => s
            .Summary("Frase curta pt-BR")
            .Description("Detalhe pt-BR")
            .ExampleRequest(new Request { /* ... */ })
            .Response<Response>(200, "Sucesso")
            .Response(400, "Dados inválidos")
            .Response(404, "Recurso não encontrado")     // se aplicável
            .Response(500, "Erro interno"));
        Tags("Subscriptions");                 // ou Financial, Scheduling, Orchestration
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var result = await handler.HandleAsync(req, ct);
        if (result.IsFailure)
        {
            AddError(result.Error);                    // ⚠️ obrigatório — sem isso payload de erro vai vazio
            await SendErrorsAsync(cancellation: ct);   // 400 com payload de erros
            return;
        }
        await SendOkAsync(result.Value, ct);           // 200
    }
}
```

**O que diverge entre os endpoints atuais** (escolha o padrão prescrito acima):
- Construtor: alguns usam tradicional (`AddNewEnrollment/Endpoint.cs`), outros primary ctor (`CancelEnrollment/Endpoint.cs`). **Use primary ctor**.
- `Summary(...)`: ADR-003 manda preencher; código atual quase nunca preenche. **Preencha em código novo**.
- `Tags(...)`: parcialmente preenchido. **Sempre preencha em código novo**.
- `AddError(result.Error)` antes de `SendErrorsAsync`: **nenhum** endpoint atual faz, então o cliente recebe 400 com `errors:[]` vazio. Isto é bug — sempre adicione.

**Casos especiais**:
- 404 explícito: `await SendNotFoundAsync(ct)` quando recurso não existe (ver `GetPlanById/Endpoint.cs`).
- Endpoint de leitura simples pode acessar `IDbConnectionFactory` direto, sem handler (padrão de `GetPlanById`). Use só quando não há regra de negócio.
- **Não** lance exceção dentro do endpoint para erro de negócio.

### 5. Aggregate (ADR-006)
- Construtor `private`.
- Factory `public static Result<T> Create(...)` com validação interna.
- Propriedades `{ get; private set; }`.
- Eventos via `AddDomainEvent(new {Coisa}Event(...))`.
- Eventos são `public record {Coisa}Event(...) : IDomainEvent;` — sufixo `Event`, herdar `IDomainEvent` de `GymErp.Common`.

### 6. Persistência (ADR-011)
- **Write**: EF Core via `{Aggregate}Repository` → `IUnitOfWork.Commit(ct)`. **Nunca** `_context.SaveChangesAsync()` direto no handler.
- **Read complexo**: Dapper via `IDbConnectionFactory` (resolve tenant automaticamente — ver §Multi-tenancy).
- DbContext fica em `Domain/{Contexto}/Infrastructure/{Contexto}DbContext.cs`. Não acesse DbContext de fora desse contexto.

### 7. DI (ADR-007)
- Registre em `Domain/{Contexto}/Infrastructure/{Contexto}Module.cs` (Autofac Module).
- Default: `InstancePerLifetimeScope` para handlers, repositórios, DbContext.
- **Não** crie interface só para DI (ex: `IHandler`, `IEnrollmentService`). Injete a classe concreta.

### 8. Mensageria Kafka (ADR-009)
- Publicar evento de domínio: `await serviceBus.PublishAsync(domainEvent, ct)` — `ServiceBusExtension` cuida do envelope.
- Consumir: handler em `Domain/{Contexto}/Features/{Reacao}/Handler.cs` recebe o evento como parâmetro do `HandleAsync` (não usa `Request` wrapper):
```csharp
public class Handler(...)
{
    public async Task HandleAsync(EnrollmentCreatedEvent message, CancellationToken ct) { ... }
}
```
Registro do consumer fica no Module do contexto.

### 9. HTTP externo (ADR-010): Flurl + Polly. Nunca `HttpClient` cru.

### 10. Saga / orquestração multi-step (ADR-008): WorkflowCore com step + compensação. Não simule saga com handler chamando handler.

### 11. Multi-tenancy (ADR-012)
- Tenant resolvido no middleware (`Tenant/TenantAccessorRegisterMiddleware.cs`) via header HTTP.
- Handlers **não** recebem `tenantId` explícito.
- `IDbConnectionFactory` (Dapper) e DbContext (EF, via tenant connection string) já são tenant-aware. Apenas injete e use.

### 12. Logging (ADR-013): Serilog estruturado. `_logger.LogInformation("Coisa {Id} ocorreu", id)` — nunca interpolação.

### 13. Orchestration: mediador legacy↔modernized (padrão crítico — futura ADR-028)

`Domain/Orchestration/` é **a porta de entrada obrigatória** para qualquer funcionalidade que:
- (a) Possa ser servida por legacy **ou** modernized dependendo de regra de negócio (ex: `PlanType.Mensal` vai para legacy, demais para modernized — ver `EnrollmentOrchestrator/Handler.cs:20`), **ou**
- (b) Precise orquestrar dados entre legacy e modernized dentro do mesmo fluxo (ex: `NewEnrollmentFlow/Steps/AddLegacyEnrollmentStep.cs` chama legacy via HTTP dentro de saga modernized).

#### Dois sub-padrões em uso

**Sub-padrão A — Mediator simples (Handler + Adapters)**
Use quando a decisão é routing puro, sem multi-step:
```
Domain/Orchestration/Features/{Feature}/
├── Endpoint.cs            # POST /api/orchestrator/{recurso}
├── Handler.cs             # decide rota baseado em regra de negócio
├── Request.cs, Response.cs
├── LegacyAdapter.cs       # Flurl HTTP → legacy API
├── ModernizedAdapter.cs   # chama handler interno OU inicia WorkflowCore
├── PlanService.cs         # consultas auxiliares HTTP (Flurl)
└── Configuration.cs       # IOptions<XxxApiConfiguration>
```
Template real: `EnrollmentOrchestrator/`.

**Sub-padrão B — Saga com integração legacy (WorkflowCore)**
Use quando fluxo é multi-step com compensação E precisa tocar legacy:
```
Domain/Orchestration/Features/{Flow}/
├── Endpoint.cs              # POST /api/orchestrator/{recurso}/flow
├── {Flow}Data.cs            # estado da saga
├── MainWorkflow.cs          # IWorkflow<TData> com .Saga(...)
└── Steps/
    ├── {Step}Step.cs        # StepBodyAsync — pode chamar legacy via HTTP
    └── {Step}CompensationStep.cs
```
Template real: `NewEnrollmentFlow/` (inclui `AddLegacyEnrollmentStep` que chama legacy API com Polly retry).

#### Regras Orchestration
- **DO**: Endpoints públicos com tag `Tags("Orchestration")` e rota `/api/orchestrator/{...}`.
- **DO**: Chamadas HTTP a legacy ou a outros contextos via **Flurl + Polly** (ADR-010). Configure timeout/retry via `IOptions<ApiConfiguration>`.
- **DO**: Adapters retornam `Result<T>` traduzindo `FlurlHttpException` em `Result.Failure` com mensagem pt-BR.
- **DO**: Steps de saga lançam exception em falha — WorkflowCore dispara compensação (NÃO use `Result<T>` em Step).
- **DO**: Compensação para **toda** ação que modifica estado em legacy (ex: `AddLegacyEnrollmentCompensationStep` reverte enrollment criado no legacy).
- **DON'T**: NÃO chame Handler de outro contexto (`Subscriptions.Handler`) direto dentro de Orchestration — chame via HTTP através do Adapter, mesmo que o destino seja o próprio modernized. Mantém o desacoplamento que permite a migração incremental.
- **DON'T**: NÃO coloque lógica de roteamento (legacy vs modernized) em `Subscriptions/`, `Financial/`, etc. Roteamento **só em Orchestration**.
- **DON'T**: NÃO duplique regra de decisão entre features — extraia para serviço auxiliar (ex: `PlanService`) dentro da própria feature de Orchestration.

#### Decisão: quando criar feature em Orchestration vs em outro contexto?

| Cenário | Onde criar |
|---|---|
| Feature só existe em modernized (sem equivalente legacy) | `Subscriptions/`, `Financial/`, etc. — não passa por Orchestration |
| Feature existe em ambos e decisão depende de regra de negócio | **Orchestration** (sub-padrão A) |
| Feature é fluxo multi-step com compensação e toca legacy | **Orchestration** (sub-padrão B) |
| Feature é fluxo multi-step modernized puro (sem legacy) | `Subscriptions/` ou contexto dono, usando WorkflowCore localmente (raro) |

---

## Testes (ADRs 014, 015, 016)

### Estrutura
```
GymErp.IntegrationTests/{Contexto}/{Feature}/
├── HandlerTests.cs
├── EndpointTests.cs              # quando há comportamento HTTP-específico
└── TestHelpers/
    └── {Feature}TestDataBuilder.cs
```
Espelha `Domain/{Contexto}/Features/{Feature}/`.

### Padrão de teste de integração (preferido)
- Herde de `IntegrationTestBase` (TestContainers PostgreSQL + Kafka reais).
- **Não** mocke `EnrollmentRepository`, `IUnitOfWork`, `{Contexto}DbContext` — use os reais.
- Use `IIntegrationSpy` (Silverback.Testing) para verificar publicação Kafka — não consuma do tópico.
- Nomeação: `{Método}_{Cenário}_{ResultadoEsperado}` (ex: `HandleAsync_WithValidRequest_ShouldReturnSuccess`).
- Asserções: FluentAssertions (`.Should().BeTrue()`, `.Should().HaveCount(2)`). **Nunca** `Assert.Equal`.

### Mock Moq: só em UnitTests
- Use Moq apenas para isolar lógica pura em `GymErp.UnitTests/`.
- Em integração, mock é proibido para componentes containerizados.

---

## DO / DON'T resumo

### DO
- `Result<T>` em todo handler.
- `record` para Request e Response.
- `CancellationToken` como parâmetro do `HandleAsync`, propagado a repo/uow.
- `SendErrorsAsync` para falha de negócio, `SendNotFoundAsync` para 404, `SendOkAsync` para sucesso.
- Vertical Slice estrita — cada feature em pasta própria.
- EF para write + Dapper para read.
- TestContainers + infra real em testes de integração.
- Citar ADR ao decidir.

### DON'T
- ❌ `Task<bool>` ou `Task<T?>` em handler.
- ❌ `throw new Exception(...)` para regra de negócio.
- ❌ `CancellationToken` no construtor do handler.
- ❌ MediatR ou qualquer dispatcher entre Endpoint e Handler.
- ❌ Handler chamando outro Handler (use composição via classes auxiliares ou WorkflowCore).
- ❌ Interface só para "facilitar mock" ou para DI.
- ❌ `_context.SaveChangesAsync()` direto no handler.
- ❌ MVC Controller, `[ApiController]`, `ControllerBase`.
- ❌ Mock de repository/UoW/DbContext em integração.
- ❌ Banco InMemory.
- ❌ `Assert.Equal` (use FluentAssertions).

---

## Cleanup pendente (bugs conhecidos — não copie)

- **C1**: `Domain/Subscriptions/Features/AddNewEnrollment/Handler.cs:7` — `CancellationToken` injetado no construtor. Mover para parâmetro do `HandleAsync`.
- **C2**: `Domain/Subscriptions/Features/SuspendEnrollment/SuspendEnrollmentHandler.cs` — retorna `Task<bool>` descartando `Result.Failure(error)`. Migrar para `Result<Unit>` ou `Result`. Renomear arquivo para `Handler.cs` (class já é `Handler`).
- **C3**: Verificar todos handlers de `modernized/GymErp/Domain/**/Features/**/Handler.cs` para o mesmo padrão de CT no construtor — provável epidemia.
- **C4**: Endpoints atuais chamam `SendErrorsAsync(cancellation: ct)` sem `AddError(result.Error)` antes — payload de erro vai vazio para o cliente. Auditar todos `Domain/**/Features/**/Endpoint.cs` e adicionar `AddError`.
- **C5**: Endpoints atuais raramente preenchem `Summary(...)` apesar do ADR-003 mandar — auditar e preencher quando tocar o arquivo.

---

## Gaps de ADR (a formalizar)

Padrões em uso real mas sem ADR. Não invente alternativa — use o que está prescrito acima até a ADR existir:

| # | Gap | Status |
|---|---|---|
| G1 | Fluxo de CancellationToken Endpoint→Handler→Repo | Prescrito aqui; **candidato a ADR-024** |
| G2 | Status codes implícitos em FastEndpoints (`SendErrorsAsync`/`SendNotFoundAsync`) e shape de payload de erro | Prescrito aqui parcialmente; **candidato a ADR-025** |
| G3 | Composição: handler vs adapter vs WorkflowCore step | Prescrito parcialmente em §13; **candidato a ADR-026** |
| G6 | Orchestration como mediador legacy↔modernized (routing + data integration) | Prescrito em §13; **candidato a ADR-028** (alta prioridade) |
| G4 | Acesso a tenant em handler (sempre via factory, nunca `tenantId` explícito) | Prescrito aqui; **candidato a ADR-027** |
| G5 | Convenção de evento Kafka (envelope, retry, nomeação `*Event`) | Parcial em ADR-009; **atualizar ADR-009** |
