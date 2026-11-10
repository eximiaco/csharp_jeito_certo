# GymErp `legacy/` — Harness operacional

Escopo: tudo em `legacy/`. Stack: ASP.NET MVC, EF Core (SQL Server em prod), Worker Service. Testes: xUnit + TestContainers.MsSql + Moq.

ADRs aplicáveis: **001, 017, 018, 019, 020, 021, 022, 023**.

**Princípio orientador (ADR-001):** legacy está em migração incremental para `modernized/`. Mantenha o estilo legacy consistente — **não modernize seletivamente**. Se quiser introduzir Result, FastEndpoints, Vertical Slice, etc., a feature inteira migra para `modernized/`, não fica meia-modernizada aqui.

---

## Playbook: nova feature ou bugfix em `legacy/Gymerp.*`

### 1. Estrutura em camadas (ADR-017)
```
legacy/
├── Gymerp.Api/                # MVC Controllers
├── Gymerp.Application/        # Services + Interfaces + DTOs
├── Gymerp.Domain/             # Entities + Interfaces de repositório
├── Gymerp.Infrastructure/     # EF Core, implementações de repositório
├── Gymerp.Common/
├── Gymerp.QueueConsumer/
└── Gymerp.Worker/             # Hosted Services (background)
```
Dependências: `Api → Application → Domain`; `Infrastructure → Domain`. **Nunca** referenciar `Infrastructure` direto de `Api`.

### 2. Controller (ADR-018)
```csharp
[ApiController]
[Route("api/[controller]")]
public class EnrollmentController(IFullEnrollmentService service) : ControllerBase
{
    [HttpPost("enroll")]
    public async Task<IActionResult> Enroll([FromBody] FullEnrollmentDto dto)
    {
        try
        {
            var id = await service.EnrollAsync(dto);
            return Ok(new { EnrollmentId = id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}
```
- Injete **interfaces** de `Application/` (não classes concretas).
- Catch `InvalidOperationException` → `BadRequest`. **Não** capture `Exception` genérica.
- **Sem** lógica de negócio no controller. **Sem** acesso direto a repositório.

### 3. Erros de negócio: exceção (ADR-020)
- `throw new InvalidOperationException("mensagem pt-BR")` em services para regra de negócio.
- Mensagem descritiva, sem stack trace, sem dados sensíveis.
- Controller traduz para `BadRequest`.
- **Não** use `Result<T>` — é padrão exclusivo de `modernized/`.
- **Não** retorne `null` silenciosamente — lance exceção.
- Exceções de infraestrutura (DB, rede): deixe propagar para middleware global de erro.

### 4. Service e Repository: interfaces obrigatórias (ADR-019)
- Cada service: `IEnrollmentService` + `EnrollmentService` em `Application/`.
- Cada repository: `IEnrollmentRepository` em `Domain/Interfaces/` + `EnrollmentRepository` em `Infrastructure/Repositories/`.
- Registro DI: `services.AddScoped<IEnrollmentService, EnrollmentService>()` no `Startup`/`Program.cs` da `Api`.
- Injete sempre a interface.

### 5. Persistência: SaveChanges por operação (ADR-021)
```csharp
public async Task AddAsync(Enrollment enrollment)
{
    await _context.Enrollments.AddAsync(enrollment);
    await _context.SaveChangesAsync();   // commit atômico por operação
}
```
- **Não** introduza `IUnitOfWork` — não é padrão legacy.
- DbContext fica encapsulado no repositório. **Não** exponha para `Application/`.
- Múltiplas operações em uma transação: use `IDbContextTransaction` explicitamente no repositório (raro).

### 6. Background / cron / consumer (ADR-022)
- `IHostedService` ou `BackgroundService` em projeto separado `Gymerp.Worker` ou `Gymerp.QueueConsumer`.
- **Não** registre `AddHostedService` em `Gymerp.Api/Program.cs`.

### 7. Logging: `ILogger<T>` padrão da Microsoft (sem Serilog estruturado no legacy — manter consistência com o que existe).

---

## Testes (ADR-023)

### Estrutura
- Projeto: `Gymerp.IntegrationTests/`.
- Foco: testar **camada `Application/Services/`**, não controller HTTP.

### Padrão
- `MsSqlContainer` (TestContainers) por suite, `IAsyncLifetime` para lifecycle.
- Moq para dependências externas que não rodam em container.
- **Não** use `UseInMemoryDatabase` — não valida constraints SQL.

### ⚠️ Risco crítico (ADR-023)
Banco de **teste** é SQL Server (`MsSqlContainer`). Banco de **produção** é PostgreSQL (`Gymerp.Api/Program.cs` usa `UseNpgsql`). **Divergência conhecida e ativa.**

Implicações:
- Comportamento de `Guid`/`uniqueidentifier`, `timestamp`, collation, geração de ID e migrations diverge silenciosamente.
- Testes podem passar usando SQL específico do SQL Server que quebra em PostgreSQL.
- **Antes de commit de feature legacy**: se a feature toca SQL cru, tipos de coluna ou migrations, valide manualmente contra PostgreSQL ou marque para validação no PR. ADR-023 cita esta divergência como motivador da migração para `modernized/` (que usa PostgreSQL em prod e em teste — ADR-015).

---

## DO / DON'T resumo

### DO
- MVC Controller com `[ApiController]` + `[Route("api/[controller]")]`.
- `throw new InvalidOperationException(...)` para erro de negócio.
- Interface para todo Service e Repository.
- `SaveChangesAsync()` ao fim de cada método de escrita do repositório.
- `try/catch InvalidOperationException` no controller → `BadRequest`.
- Hosted services em projeto separado (`Worker`/`QueueConsumer`).
- TestContainers.MsSql para integração.

### DON'T
- ❌ `Result<T>`, CSharpFunctionalExtensions.
- ❌ FastEndpoints, REPR, Vertical Slice.
- ❌ Aggregate com factory `static Result<T> Create(...)` — entidades legacy são anêmicas com lógica em Services.
- ❌ `IUnitOfWork` — `SaveChangesAsync` por operação.
- ❌ Domain Events / Silverback / WorkflowCore.
- ❌ Catch `Exception` genérica em controller.
- ❌ Injetar classe concreta sem interface.
- ❌ Lógica de negócio em controller.
- ❌ `AddHostedService` em `Gymerp.Api`.
- ❌ `UseInMemoryDatabase` em teste.
- ❌ Modernizar parcialmente — se for modernizar, migre a feature inteira para `modernized/`.

---

## Quando migrar uma feature para `modernized/`

Critério (decisão arquitetural, não código):
1. Feature tem mudança grande pendente (refactor > 30% das linhas).
2. OU: feature ganha novo requisito que pede multi-tenancy / mensageria / saga.
3. OU: time decide pela migração explícita.

**Procedimento**: criar a versão em `modernized/GymErp/Domain/{Contexto}/Features/{Feature}/` seguindo `modernized/CLAUDE.md`, manter legacy intocado até cutover, registrar decisão em ADR nova.

---

## Cleanup pendente (bugs conhecidos — não copie)

(Nenhum identificado na varredura inicial. Atualizar conforme aparecem.)

---

## Gaps de ADR

(Nenhum gap legacy identificado na varredura inicial. Padrões legacy estão bem cobertos por ADR-017..023.)
