# ADR-011: EF Core (Npgsql) para Writes e Dapper para Reads

> **Escopo:** exclusivamente `modernized/GymErp/` — **não se aplica** ao código em `legacy/`.

## Contexto

O projeto modernizado usa PostgreSQL como banco de dados. A escolha de estratégia de acesso a dados precisa equilibrar mapeamento objeto-relacional para writes com performance e controle para queries de leitura. Aplica-se a todo acesso a dados em `modernized/GymErp/`.

Evidência: `Domain/Subscriptions/Infrastructure/SubscriptionsDbContext.cs` herda de `DbContext` (EF Core). `GymErp.csproj` referencia `Microsoft.EntityFrameworkCore 9.0.5`, `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4` e `Dapper 2.1.66`. `Common/IDbConnectionFactory.cs` e `Common/DatabaseConfiguration.cs` suportam Dapper.

**Alternativas consideradas:**
1. EF Core puro para tudo — rejeitado para queries complexas onde o SQL gerado é menos eficiente.
2. Dapper puro — rejeitado pela ausência de change tracking e mapeamento de aggregates complexos.
3. EF Core para writes + Dapper para reads — **escolhido** pelo melhor dos dois mundos.

## Decisão

Adotamos EF Core com Npgsql para operações de escrita (insert, update, delete) via DbContext e repositórios. Dapper com `IDbConnectionFactory` é usado para queries de leitura complexas ou relatórios onde controle fino do SQL é necessário. O schema do banco é gerenciado via EF Core migrations ou `EnsureCreated` (em testes).

## Regras Operacionais

1. **SEMPRE** use `DbContext` para operações de write (Add, Update, Delete) em aggregates. *Padrão detectável:* `_dbContext.{DbSet}.Add(` em repositórios. *Motivo:* EF Core gerencia change tracking e unit of work.
2. **PREFIRA** Dapper via `IDbConnectionFactory` para queries de leitura com múltiplos joins ou projeções customizadas. *Padrão detectável:* `_dbConnectionFactory.CreateConnection()` em handlers de query. *Motivo:* SQL explícito é mais previsível e performático para reads.
3. **SEMPRE** isole o `DbContext` em `Domain/{Contexto}/Infrastructure/` e nunca compartilhe entre contextos. *Padrão detectável:* `SubscriptionsDbContext` referenciado apenas dentro de `Domain/Subscriptions/`. *Motivo:* cada contexto tem seu próprio schema e ciclo de migração.
4. **SEMPRE** use `IUnitOfWork` para confirmar writes, nunca chame `SaveChangesAsync()` diretamente no handler. *Padrão detectável:* `unitOfWork.Commit(` em `Handler.cs`. *Motivo:* abstrai o commit e facilita testes com mock.

## Exemplos

### ✅ DO

```csharp
// Write via EF Core — Handler.cs
public async Task<Result<Guid>> HandleAsync(Request request)
{
    var result = Enrollment.Create(...);
    if (result.IsFailure) return Result.Failure<Guid>(result.Error);

    await repository.AddAsync(result.Value, cancellationToken);  // EF Core
    await unitOfWork.Commit(cancellationToken);                   // SaveChangesAsync interno
    return Result.Success(result.Value.Id);
}

// Read via Dapper — handler de query
public async Task<EnrollmentSummary?> GetSummaryAsync(Guid id)
{
    using var connection = _dbConnectionFactory.CreateConnection();
    return await connection.QueryFirstOrDefaultAsync<EnrollmentSummary>(
        "SELECT e.id, c.name, e.state FROM enrollments e JOIN clients c ON c.id = e.client_id WHERE e.id = @id",
        new { id });
}
```

### ❌ DON'T

```csharp
// Por que está errado: SaveChangesAsync direto no handler, bypassa IUnitOfWork
public async Task HandleAsync(Request request)
{
    _dbContext.Enrollments.Add(enrollment);
    await _dbContext.SaveChangesAsync();  // direto no handler
}
```

## Benefícios

- **Writes confiáveis** — EF Core change tracking garante consistência do aggregate.
- **Reads eficientes** — Dapper executa SQL otimizado sem overhead de materialização.
- **Abstração de commit** — `IUnitOfWork` facilita testes e transações.

## Relacionado

- [ADR-006](./adr-006-aggregates-domain-events.md): repositórios persistem aggregates.
- [ADR-007](./adr-007-autofac-modulos.md): DbContext registrado com `InstancePerLifetimeScope`.
- [ADR-015](./adr-015-testcontainers.md): PostgreSqlContainer para testes de integração.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
