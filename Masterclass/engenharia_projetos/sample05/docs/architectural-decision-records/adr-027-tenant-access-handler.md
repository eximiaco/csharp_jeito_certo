# ADR-027: Acesso a Tenant em Handler via Factory (sem `tenantId` explícito)

> **Escopo:** exclusivamente `modernized/GymErp/` — refina [ADR-012](./adr-012-multitenancy-header.md).

## Contexto

ADR-012 estabelece que tenant é resolvido por header HTTP no middleware. Mas não documenta **como** Handlers acessam dados tenant-scoped. Sem regra, código novo pode:

- (a) Receber `tenantId` no Request (vaza concern de transporte para domínio).
- (b) Injetar `IHttpContextAccessor` no Handler (acopla domínio a HTTP).
- (c) Ler `TenantAccessor` direto e passar para queries manualmente.

Aplica-se a todo `Handler.cs` e `Endpoint.cs` que toca dados tenant-scoped em `modernized/GymErp/Domain/`.

Evidência:

- `Tenant/TenantAccessorRegisterMiddleware.cs:18-19` — middleware lê header, resolve tenant via `tenantLocator.Get(...)` e registra em `tenantAccessor.Register(tenant)`.
- `Tenant/TenantDbConnectionFactory.cs:7` — `IDbConnectionFactory` injeta `TenantAccessor` e cria `NpgsqlConnection` com `tenantAccessor.Tenant.ConnectionString`.
- DbContexts dos contextos (ex: `Domain/Subscriptions/Infrastructure/SubscriptionsDbContext`) recebem connection string tenant-aware via factory.
- Nenhum Handler em `Domain/**/Features/**/Handler.cs` recebe `tenantId` como parâmetro — todos resolvem implicitamente via repository ou factory.

**Alternativas consideradas:**
1. `tenantId` no Request — rejeitada: cliente nunca deve enviar tenant; vazaria multi-tenancy para contrato HTTP.
2. `IHttpContextAccessor` no Handler — rejeitada: acopla domínio a HTTP; quebra testabilidade fora de request HTTP (ex: worker, consumer Kafka).
3. **Tenant resolvido implicitamente via `IDbConnectionFactory` (Dapper) e DbContext tenant-aware (EF)** — **escolhida**, mantém domínio agnóstico de transporte.

## Decisão

Adotamos a regra de que **Handler e Endpoint nunca recebem `tenantId` explícito**. Tenant é resolvido pelo middleware (`TenantAccessorRegisterMiddleware`) e injetado transparentemente em:

- **`IDbConnectionFactory.Create(ct)`** (Dapper) — retorna conexão para o banco do tenant atual.
- **DbContext do contexto** (EF Core) — construído com connection string tenant-aware via factory; queries respeitam tenant automaticamente.
- **`TenantAccessor`** — disponível para componentes de infraestrutura (factories, providers). **Não** deve ser injetado em Handler de feature.

Para fluxos fora de requisição HTTP (ex: consumer Silverback de evento Kafka), o middleware equivalente que registra tenant precisa rodar antes do Handler (escopo da feature definir).

## Regras Operacionais

1. **NUNCA** declare `tenantId` (ou `Guid TenantId`) no `Request.cs`/`Endpoint.cs`/`Handler.HandleAsync`. *Padrão detectável:* propriedade/parâmetro com nome `TenantId` ou `tenantId` em `Domain/**/Features/**/`. *Motivo:* vazaria multi-tenancy para o contrato HTTP/cliente.
2. **NUNCA** injete `IHttpContextAccessor` em Handler. *Padrão detectável:* `IHttpContextAccessor` em construtor de `Domain/**/Features/**/Handler.cs`. *Motivo:* acopla domínio a HTTP; quebra testabilidade.
3. **NUNCA** injete `TenantAccessor` em Handler. *Padrão detectável:* `TenantAccessor` em construtor de `Domain/**/Features/**/Handler.cs`. *Motivo:* tenant deve ser transparente para domínio; uso de TenantAccessor fica em factories de infraestrutura.
4. **SEMPRE** acesse dados tenant-scoped via `IDbConnectionFactory.Create(ct)` (Dapper) ou via DbContext injetado (EF). *Padrão detectável:* `IDbConnectionFactory` ou `{Contexto}DbContext` em construtores de Handler/Endpoint. *Motivo:* factories já são tenant-aware; nenhum trabalho manual de filtro por tenant.
5. **SEMPRE** garanta que fluxos não-HTTP (consumer Kafka, worker, saga) executem registro de tenant equivalente antes do Handler rodar. *Motivo:* `TenantAccessor` retorna tenant não registrado em contexto sem middleware.

## Exemplos

### ✅ DO

```csharp
// Domain/Subscriptions/Features/GetPlanById/Endpoint.cs
public class Endpoint(IDbConnectionFactory connectionFactory) : Endpoint<Request, Response>
{
    public override void Configure() { Get("/api/plans/{id}"); }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        using var connection = await connectionFactory.Create(ct); // tenant resolvido implicitamente
        await connection.OpenAsync(ct);
        var plan = await connection.QueryFirstOrDefaultAsync<Response>(sql, new { Id = req.Id });
        if (plan is null) { await SendNotFoundAsync(ct); return; }
        await SendOkAsync(plan, ct);
    }
}

// Handler com EF (DbContext tenant-aware via DI)
public class Handler(EnrollmentRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> HandleAsync(Request request, CancellationToken ct)
    {
        // repository usa SubscriptionsDbContext, que recebeu connection string do tenant via factory
        await repository.AddAsync(enrollment, ct);
        await unitOfWork.Commit(ct);
        return Result.Success(enrollment.Id);
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: tenantId no Request vaza multi-tenancy para contrato HTTP.
public record Request
{
    public Guid TenantId { get; init; }        // ❌
    public string Name { get; init; } = "";
}

// Por que está errado: TenantAccessor injetado em Handler. Domínio passa a saber sobre multi-tenancy explicitamente.
public class Handler(EnrollmentRepository repo, IUnitOfWork uow, TenantAccessor tenant) // ❌
{
    public async Task<Result<Guid>> HandleAsync(Request req, CancellationToken ct)
    {
        var tenantId = tenant.Tenant.Id;        // ❌
        // ...
    }
}
```

## Como Verificar Conformidade

- **Grep de `TenantId` em Request**: `rg "TenantId" modernized/GymErp/Domain/ --type cs | rg -v "Infrastructure|Tenant/"`.
- **Grep de `TenantAccessor`/`IHttpContextAccessor` em Handler**: `rg "TenantAccessor|IHttpContextAccessor" modernized/GymErp/Domain/**/Features/`.
- **Checklist manual**: novo Handler que toca DB recebe `IDbConnectionFactory` ou `{Contexto}DbContext`, nunca `TenantAccessor`.

## Próximos Passos

1. Quando implementar consumer Kafka (Silverback) que precise de tenant, definir mecanismo equivalente ao middleware HTTP (provável: header da mensagem Kafka + handler de pipeline Silverback que registra tenant). Documentar em ADR nova ou estender ADR-009.

## Relacionado

- [ADR-012](./adr-012-multitenancy-header.md): tenant resolvido por header HTTP no middleware. Esta ADR refina o consumo do tenant pelo domínio.
- [ADR-011](./adr-011-efcore-dapper.md): factory de conexão Dapper é tenant-aware.
- [ADR-024](./adr-024-cancellation-token-flow.md): `IDbConnectionFactory.Create(ct)` recebe CT por esta ADR.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
