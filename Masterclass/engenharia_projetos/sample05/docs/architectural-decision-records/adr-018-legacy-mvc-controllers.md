# ADR-018: ASP.NET MVC Controllers como Framework HTTP

> **Escopo:** exclusivamente `legacy/Gymerp.Api/` — **não se aplica** ao código em `modernized/` (ver ADR-003).

## Contexto

O projeto legado usa ASP.NET MVC Controllers como camada HTTP. Aplica-se a todo endpoint em `legacy/Gymerp.Api/`.

Evidência: `Gymerp.Api/Controllers/EnrollmentController.cs` herda de `ControllerBase` com `[ApiController]` e `[Route("api/[controller]")]`. `Program.cs` registra `AddControllers()` e chama `MapControllers()`.

## Decisão

Adotamos ASP.NET MVC Controllers (`ControllerBase`) como framework HTTP. Cada controller agrupa múltiplos endpoints relacionados ao mesmo recurso. A rota base é definida por `[Route("api/[controller]")]` no controller; cada ação tem seu próprio atributo HTTP (`[HttpPost("create")]`, etc.). Erros de negócio são capturados via `try/catch` e retornados como `BadRequest`.

## Regras Operacionais

1. **SEMPRE** decore controllers com `[ApiController]` e `[Route("api/[controller]")]`. *Padrão detectável:* `[ApiController]` + `[Route(` em `Controllers/*.cs`. *Motivo:* `[ApiController]` habilita model binding automático e validação de `ModelState`.
2. **SEMPRE** injete serviços de `Application/Interfaces/` no controller via construtor. *Padrão detectável:* parâmetros de construtor tipados com `I*Service` em controllers. *Motivo:* controller não acessa repositórios diretamente — apenas services de aplicação.
3. **SEMPRE** capture `InvalidOperationException` com `try/catch` e retorne `BadRequest` com a mensagem. *Padrão detectável:* `catch (InvalidOperationException ex)` → `return BadRequest(new { Error = ex.Message })` em controllers. *Motivo:* padrão de tratamento de erro de negócio do projeto legado (ver ADR-020).
4. **NUNCA** adicione lógica de negócio diretamente no controller. *Padrão detectável:* chamadas a repositórios ou lógica de domínio dentro de métodos de controller. *Motivo:* controller é responsável apenas por HTTP — orquestração pertence ao Application Service.

## Exemplos

### ✅ DO

```csharp
// legacy/Gymerp.Api/Controllers/EnrollmentController.cs
[ApiController]
[Route("api/[controller]")]
public class EnrollmentController(
    IFullEnrollmentService fullEnrollmentService,
    IEnrollmentService enrollmentService) : ControllerBase
{
    [HttpPost("enroll")]
    public async Task<IActionResult> Enroll([FromBody] FullEnrollmentDto dto)
    {
        try
        {
            var enrollmentId = await fullEnrollmentService.EnrollAsync(dto);
            return Ok(new { EnrollmentId = enrollmentId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: controller acessa repositório diretamente, bypassa Application
[HttpPost("enroll")]
public async Task<IActionResult> Enroll([FromBody] FullEnrollmentDto dto)
{
    var student = await _studentRepository.GetByDocumentAsync(dto.Student.Document);
    // lógica de negócio aqui...
}
```

## Benefícios

- **Agrupamento por recurso** — todos os endpoints de matrícula em um único controller.
- **Scaffolding** — geração automática de controllers via `dotnet-aspnet-codegenerator`.

## Relacionado

- [ADR-003](./adr-003-fastendpoints.md): o modernizado substitui Controllers por FastEndpoints.
- [ADR-020](./adr-020-legacy-excecoes-erros-negocio.md): padrão de tratamento de erro via exceções.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
