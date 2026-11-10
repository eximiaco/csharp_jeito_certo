# ADR-024: Fluxo de CancellationToken — Endpoint → Handler.HandleAsync → Repository/UnitOfWork

> **Escopo:** exclusivamente `modernized/GymErp/` — **não se aplica** a `legacy/`.

## Contexto

Handlers em `modernized/GymErp/Domain/**/Features/**/Handler.cs` precisam propagar cancelamento HTTP até as camadas de persistência e mensageria para liberar recursos quando o cliente desiste da requisição. Aplica-se a todo `Handler.cs` de feature, incluindo handlers de evento Silverback.

Evidência (anti-padrão ativo no repo):

- `Domain/Subscriptions/Features/AddNewEnrollment/Handler.cs:7` — `CancellationToken cancellationToken` injetado no construtor: `public class Handler(EnrollmentRepository repository, IUnitOfWork unitOfWork, CancellationToken cancellationToken)`. Autofac não resolve `CancellationToken` como serviço; o valor recebido é `default` (não cancela nunca).
- `Domain/Subscriptions/Features/SuspendEnrollment/SuspendEnrollmentHandler.cs:6` — mesmo padrão.
- `Domain/Subscriptions/Features/AddNewEnrollment/Endpoint.cs:22` — endpoint chama `await _handler.HandleAsync(req)` sem propagar `ct` recebido em `HandleAsync(Request req, CancellationToken ct)`.

Motivação: bug silencioso. Cancelamento HTTP não chega ao banco/Kafka; queries longas continuam após cliente desconectar; testes de timeout falham aleatoriamente.

**Alternativas consideradas:**
1. CT no construtor do Handler resolvido por adapter Autofac — rejeitada por exigir scope custom e mascarar o lifetime real do token.
2. `IHttpContextAccessor.HttpContext.RequestAborted` dentro do Handler — rejeitada por acoplar domínio a HTTP.
3. **CT como parâmetro do método `HandleAsync`, propagado a repo/UoW/serviceBus** — **escolhida**.

## Decisão

Adotamos `CancellationToken` como parâmetro explícito do método `HandleAsync` de todo Handler em `modernized/GymErp/`. O Endpoint recebe `ct` do FastEndpoints e o passa ao Handler; o Handler propaga para `repository.{Op}Async(.., ct)`, `unitOfWork.Commit(ct)`, `serviceBus.PublishAsync(evt, ct)` e qualquer outro `Async` que aceite token. Construtor do Handler recebe **apenas dependências resolvíveis pelo container** (repositórios, UoW, serviços) — nunca `CancellationToken`.

## Regras Operacionais

1. **SEMPRE** declare CT como último parâmetro de `HandleAsync`: `public async Task<Result<T>> HandleAsync(Request req, CancellationToken ct)`. *Padrão detectável:* assinatura de `Domain/**/Features/**/Handler.cs`. *Motivo:* token é parte da invocação, não do lifetime do Handler.
2. **NUNCA** declare `CancellationToken` no construtor do Handler. *Padrão detectável:* `class Handler(.., CancellationToken` em `Domain/**/Features/**/Handler.cs`. *Motivo:* Autofac não resolve CT; valor recebido é `default`, cancelamento nunca dispara.
3. **SEMPRE** propague `ct` para todo método `Async` chamado dentro do Handler que aceite token: `repo.AddAsync(x, ct)`, `unitOfWork.Commit(ct)`, `serviceBus.PublishAsync(evt, ct)`, `connection.QueryAsync(.., ct)` (Dapper aceita via `CommandDefinition`). *Padrão detectável:* ausência de `, ct` em chamadas `Async` dentro do Handler. *Motivo:* token só protege se atravessar toda a stack.
4. **SEMPRE** propague `ct` no Endpoint: `await handler.HandleAsync(req, ct)`. *Padrão detectável:* `_handler.HandleAsync(req)` (sem `ct`) em `Domain/**/Features/**/Endpoint.cs`. *Motivo:* Endpoint é o ponto que conecta o CT do FastEndpoints ao Handler.
5. **NUNCA** use `CancellationToken.None` como atalho para "não preciso de CT". *Padrão detectável:* `CancellationToken.None` em código de produção em `Domain/`. *Motivo:* mascara o cancelamento real.

## Exemplos

### ✅ DO

```csharp
// Domain/Subscriptions/Features/AddNewEnrollment/Handler.cs
public class Handler(EnrollmentRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> HandleAsync(Request request, CancellationToken ct)
    {
        var enrollmentResult = Enrollment.Create(/* ... */);
        if (enrollmentResult.IsFailure)
            return Result.Failure<Guid>(enrollmentResult.Error);

        var enrollment = enrollmentResult.Value;
        await repository.AddAsync(enrollment, ct);
        await unitOfWork.Commit(ct);
        return Result.Success(enrollment.Id);
    }
}

// Domain/Subscriptions/Features/AddNewEnrollment/Endpoint.cs
public override async Task HandleAsync(Request req, CancellationToken ct)
{
    var result = await handler.HandleAsync(req, ct);
    // ...
}
```

### ❌ DON'T

```csharp
// Por que está errado: CancellationToken injetado no construtor resolve para default; ct do Endpoint nunca chega ao repo/UoW.
public class Handler(EnrollmentRepository repository, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
{
    public async Task<Result<Guid>> HandleAsync(Request request)
    {
        await repository.AddAsync(enrollment, cancellationToken); // cancellationToken == default
        await unitOfWork.Commit(cancellationToken);
        return Result.Success(enrollment.Id);
    }
}
```

## Como Verificar Conformidade

- **Grep de anti-padrão construtor**: `rg "class Handler\([^)]*CancellationToken" modernized/GymErp/Domain/`.
- **Grep de Endpoint não propagando**: `rg "_handler\.HandleAsync\(req\)" modernized/GymErp/Domain/`.
- **Grep de `CancellationToken.None` em produção**: `rg "CancellationToken\.None" modernized/GymErp/Domain/`.

## Próximos Passos

1. Refatorar `AddNewEnrollment/Handler.cs`, `SuspendEnrollment/SuspendEnrollmentHandler.cs` e auditar todos `Domain/**/Features/**/Handler.cs` (Cleanup C1/C3 em `modernized/CLAUDE.md`).
2. Atualizar `Endpoint.cs` dos mesmos features para propagar `ct`.
3. Adicionar grep no CI como check de não-regressão.

## Relacionado

- [ADR-003](./adr-003-fastendpoints.md): FastEndpoints fornece `ct` em `Endpoint.HandleAsync`.
- [ADR-005](./adr-005-result-pattern.md): Handler retorna `Result<T>` — esta ADR padroniza a assinatura completa.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
