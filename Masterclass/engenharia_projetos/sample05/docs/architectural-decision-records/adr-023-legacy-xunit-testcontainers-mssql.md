# ADR-023: xUnit + TestContainers.MsSql + Moq nos Testes de Integração

> **Escopo:** exclusivamente `legacy/Gymerp.IntegrationTests/` — **não se aplica** a `modernized/` (que usa PostgreSqlContainer — ADR-015).

## Contexto

Os testes de integração do projeto legado precisam de banco de dados real para testar a cadeia `Service → Repository → DbContext`. Aplica-se a `legacy/Gymerp.IntegrationTests/`.

Evidência: `FullEnrollmentServiceTests.cs` usa `MsSqlContainer` (SQL Server) via `Testcontainers.MsSql` e `UseSqlServer()` no DbContext. O projeto de produção (`Gymerp.Api/Program.cs`) usa `UseNpgsql()` (PostgreSQL).

> ⚠️ **Desvio identificado:** os testes de integração usam **SQL Server** (`MsSqlContainer`) mas a aplicação em produção usa **PostgreSQL** (`UseNpgsql`). Comportamentos específicos de banco (tipos de dado, collation, geração de ID, migrations) podem divergir silenciosamente. Este risco deve ser considerado na migração para o modernizado.

## Decisão

Adotamos xUnit como test runner, `TestContainers.MsSql` para banco efêmero e Moq para dependências externas (ex.: `IPaymentRepository`, `INotificationService`) que não têm infraestrutura containerizada. Os testes exercitam a camada de `Application/Services/` diretamente, sem subir o endpoint HTTP.

## Regras Operacionais

1. **SEMPRE** use `MsSqlContainer` (não banco em memória) nos testes de integração do legado. *Padrão detectável:* `new MsSqlBuilder()` em `Gymerp.IntegrationTests`. *Motivo:* InMemory provider não valida constraints e behaviors SQL reais.
2. **SEMPRE** implemente `IAsyncLifetime` para gerenciar o ciclo de vida do container. *Padrão detectável:* `InitializeAsync` + `DisposeAsync` em classes de teste. *Motivo:* xUnit chama esses métodos antes/depois de cada test class.
3. **PREFIRA** testar na camada de `Application/Services/` em vez de controller HTTP no legado. *Padrão detectável:* instâncias de `*Service` construídas diretamente nos testes, sem `WebApplicationFactory`. *Motivo:* controller do legado é delgado — o valor está nos services.
4. **NUNCA** use banco InMemory do EF Core como substituto de banco real nos testes de integração. *Padrão detectável:* `UseInMemoryDatabase` em `Gymerp.IntegrationTests`. *Motivo:* InMemory não valida foreign keys, constraints nem comportamento de transação.

## Riscos e Limitações

- **Risco crítico:** banco de teste (SQL Server) diverge do banco de produção (PostgreSQL). Comportamentos como geração de `GUID`, `timestamp`, collation e alguns tipos de coluna são diferentes. *Mitigação:* este risco é um dos motivadores da migração para o modernizado, que usa PostgreSQL também nos testes (ADR-015).

## Exemplos

### ✅ DO

```csharp
// legacy/Gymerp.IntegrationTests/Services/FullEnrollmentServiceTests.cs
public class FullEnrollmentServiceTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await _context.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task EnrollAsync_WithValidData_ShouldCreateEnrollment()
    {
        var enrollmentId = await _service.EnrollAsync(dto);
        Assert.NotNull(await _enrollmentRepository.GetByIdAsync(enrollmentId));
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: InMemory não valida constraints — testes passam mas produção falha
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase("TestDb")
    .Options;
```

## Relacionado

- [ADR-015](./adr-015-testcontainers.md): o modernizado usa PostgreSqlContainer, alinhado com produção.
- [ADR-019](./adr-019-legacy-interfaces-servicos-repositorios.md): interfaces permitem Moq de dependências externas nos testes.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
