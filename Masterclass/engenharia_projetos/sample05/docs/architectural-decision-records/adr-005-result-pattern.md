# ADR-005: Result Pattern via CSharpFunctionalExtensions para Erros de Negócio

> **Escopo:** exclusivamente `modernized/GymErp/` — **não se aplica** ao código em `legacy/`.

## Contexto

O projeto modernizado precisa de uma estratégia para representar falhas de negócio sem lançar exceções. Aplica-se a Handlers e Aggregates em `modernized/GymErp/`.

Evidência: `Handler.cs` em `AddNewEnrollment/` retorna `Task<Result<Guid>>`; `Enrollment.Create()` retorna `Result<Enrollment>` com mensagens de erro como "CPF inválido", "Nome deve ter pelo menos 10 caracteres". PackageReference `CSharpFunctionalExtensions 3.6.0` em `GymErp.csproj`.

**Alternativas consideradas:**
1. Exceções de domínio — rejeitadas por uso indevido de exceções para fluxo de negócio e overhead de stack trace.
2. Retorno de tuple `(T value, string error)` — rejeitado por falta de semântica e ausência de padrão de composição.
3. `Result<T>` via CSharpFunctionalExtensions — **escolhido** por API expressiva, composição fluente e sem dependência de exceção.

## Decisão

Adotamos `Result<T>` e `Result` da biblioteca `CSharpFunctionalExtensions` para representar sucesso ou falha em operações de negócio. Handlers retornam `Task<Result<T>>` ou `Task<Result>`. Aggregates expõem factory methods e operações que retornam `Result<T>`. O Endpoint converte `IsFailure` em resposta HTTP de erro sem lançar exceção.

## Regras Operacionais

1. **SEMPRE** retorne `Result<T>` ou `Result` em métodos de Handler e em factory methods de Aggregates. *Padrão detectável:* `Task<Result<` em `Handler.cs` e `static Result<` em aggregates. *Motivo:* erros de negócio são previsíveis e não devem ser exceções.
2. **NUNCA** lance `throw` para erros de validação de negócio dentro de Handlers ou Aggregates. *Padrão detectável:* `throw new` em `Handler.cs` ou nos métodos de domínio. *Motivo:* exceções são para erros inesperados de infraestrutura.
3. **SEMPRE** verifique `result.IsFailure` no Endpoint antes de chamar `result.Value`. *Padrão detectável:* `if (result.IsFailure)` em `Endpoint.cs`. *Motivo:* acessar `.Value` em resultado de falha lança exceção interna da biblioteca.
4. **PREFIRA** `Result.Failure<T>("mensagem descritiva")` com mensagem de negócio legível. *Motivo:* a mensagem é propagada para o cliente via `SendErrorsAsync`.

## Exemplos

### ✅ DO

```csharp
// modernized/GymErp/Domain/Subscriptions/Aggreates/Enrollments/Enrollment.cs
public static Result<Enrollment> Create(Client client)
{
    var validationResult = ValidateClient(client);
    if (validationResult.IsFailure)
        return Result.Failure<Enrollment>(validationResult.Error);

    var enrollment = new Enrollment(Guid.NewGuid(), client, DateTime.UtcNow, EState.Suspended);
    enrollment.AddDomainEvent(new EnrollmentCreatedEvent(enrollment.Id));
    return enrollment;  // implicit conversion to Result.Success<Enrollment>
}

// modernized/GymErp/Domain/Subscriptions/Features/AddNewEnrollment/Handler.cs
public async Task<Result<Guid>> HandleAsync(Request request)
{
    var enrollmentResult = Enrollment.Create(...);
    if (enrollmentResult.IsFailure)
        return Result.Failure<Guid>(enrollmentResult.Error);

    await repository.AddAsync(enrollmentResult.Value, cancellationToken);
    await unitOfWork.Commit(cancellationToken);
    return Result.Success(enrollmentResult.Value.Id);
}
```

### ❌ DON'T

```csharp
// Por que está errado: usa exceção para validação de negócio previsível
public static Enrollment Create(Client client)
{
    if (string.IsNullOrWhiteSpace(client.Cpf))
        throw new ArgumentException("CPF não pode ser vazio");
    // ...
    return new Enrollment(...);
}
```

## Benefícios

- **Fluxo explícito** — o compilador força o caller a tratar o caso de falha.
- **Sem overhead de exceção** — sem stack trace para erros de validação esperados.
- **Composição funcional** — `Result.Map`, `Result.Bind` disponíveis para pipelines complexos.

## Relacionado

- [ADR-003](./adr-003-fastendpoints.md): Endpoint converte Result em resposta HTTP.
- [ADR-006](./adr-006-aggregates-domain-events.md): Aggregates usam Result em factory methods.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
