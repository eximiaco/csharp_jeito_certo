# ADR-020: Exceções (InvalidOperationException) como Mecanismo de Erros de Negócio

> **Escopo:** exclusivamente `legacy/Gymerp.Application/` — **não se aplica** ao código em `modernized/` (que usa Result pattern — ADR-005).

## Contexto

O projeto legado precisa comunicar falhas de negócio (plano não encontrado, personal indisponível, pagamento recusado) de serviços de aplicação para controllers. Aplica-se a `legacy/Gymerp.Application/Services/`.

Evidência: `FullEnrollmentService.EnrollAsync()` lança `throw new InvalidOperationException("Plano não encontrado")`, `throw new InvalidOperationException("Personal não encontrado para avaliação física")`, etc. `EnrollmentController` captura com `catch (InvalidOperationException ex)` e retorna `BadRequest(new { Error = ex.Message })`.

**Alternativas consideradas:**
1. Result pattern (CSharpFunctionalExtensions) — adotado no modernizado (ADR-005); não introduzido no legado para não criar dependência nova.
2. Custom exception types por domínio — rejeitado pela complexidade adicional no legado em modo manutenção.
3. `InvalidOperationException` como convenção — **escolhido** por ser nativo do .NET e já estar em uso no código existente.

## Decisão

Adotamos `InvalidOperationException` como exceção padrão para erros de negócio previsíveis nos services do projeto legado. Cada service lança `InvalidOperationException` com mensagem descritiva em português quando uma regra de negócio é violada. Controllers capturam essa exceção e retornam `BadRequest`.

## Regras Operacionais

1. **SEMPRE** use `throw new InvalidOperationException("mensagem descritiva")` para erros de negócio em services do legado. *Padrão detectável:* `throw new InvalidOperationException` em `Application/Services/`. *Motivo:* convenção estabelecida; outros tipos de exceção indicam erro de infraestrutura.
2. **SEMPRE** capture `InvalidOperationException` no controller e retorne `BadRequest`. *Padrão detectável:* `catch (InvalidOperationException ex)` → `return BadRequest(` em `Controllers/`. *Motivo:* erros de negócio não devem resultar em 500; `BadRequest` comunica validação falhou.
3. **NUNCA** capture `Exception` genérica nos controllers. *Padrão detectável:* `catch (Exception` em `Controllers/`. *Motivo:* mascara erros reais de infraestrutura que devem resultar em 500.
4. **NUNCA** use `InvalidOperationException` para erros de infraestrutura (ex.: conexão recusada, timeout). *Motivo:* essa exceção indica violação de regra de negócio — erros de infra devem propagar.

## Exemplos

### ✅ DO

```csharp
// legacy/Gymerp.Application/Services/FullEnrollmentService.cs
var plan = await planRepository.GetByIdAsync(dto.PlanId);
if (plan == null)
    throw new InvalidOperationException("Plano não encontrado");

// legacy/Gymerp.Api/Controllers/EnrollmentController.cs
try
{
    var enrollmentId = await fullEnrollmentService.EnrollAsync(dto);
    return Ok(new { EnrollmentId = enrollmentId });
}
catch (InvalidOperationException ex)
{
    return BadRequest(new { Error = ex.Message });
}
```

### ❌ DON'T

```csharp
// Por que está errado: retorna null silenciosamente — controller não tem como distinguir sucesso de falha
public async Task<Guid?> EnrollAsync(FullEnrollmentDto dto)
{
    var plan = await planRepository.GetByIdAsync(dto.PlanId);
    if (plan == null) return null;  // null não carrega a razão da falha
    // ...
}
```

## Riscos e Limitações

- **Risco:** exceções são custosas para fluxo de controle. *Mitigação:* no legado, em modo manutenção, o custo é aceitável; o modernizado usa Result pattern.
- **Risco:** mensagem de erro exposta no body da resposta HTTP. *Mitigação:* mensagens são em português, sem dados sensíveis.

## Relacionado

- [ADR-005](./adr-005-result-pattern.md): o modernizado substitui este padrão por Result<T>.
- [ADR-018](./adr-018-legacy-mvc-controllers.md): controllers implementam o try/catch correspondente.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
