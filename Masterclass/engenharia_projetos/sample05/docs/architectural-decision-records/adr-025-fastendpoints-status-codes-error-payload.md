# ADR-025: Status Codes e Payload de Erro em FastEndpoints (`AddError` + `SendErrorsAsync` + `SendNotFoundAsync`)

> **Escopo:** exclusivamente `modernized/GymErp/` — refina [ADR-003](./adr-003-fastendpoints.md).

## Contexto

Endpoints em `modernized/GymErp/Domain/**/Features/**/Endpoint.cs` precisam mapear `Result<T>.IsFailure` para resposta HTTP com payload de erro útil ao cliente. Aplica-se a todo Endpoint FastEndpoints do modernized.

Evidência (bug ativo no repo):

- `Domain/Subscriptions/Features/AddNewEnrollment/Endpoint.cs:25` — `await SendErrorsAsync(cancellation: ct)` sem `AddError(result.Error)` antes. FastEndpoints serializa `ValidationFailures` da instância do Endpoint; como nenhum erro foi adicionado, o cliente recebe `400` com `errors: []` vazio.
- `Domain/Subscriptions/Features/CancelEnrollment/Endpoint.cs:21` — mesmo padrão.
- `Domain/Orchestration/Features/EnrollmentOrchestrator/Endpoint.cs:22` — mesmo padrão.
- `Domain/Subscriptions/Features/GetPlanById/Endpoint.cs:42` — `await SendNotFoundAsync(ct)` quando consulta Dapper retorna `null`. Comportamento correto para 404.

Motivação: bug silencioso. Toda falha de negócio chega ao cliente como `400` com payload vazio — frontend não consegue exibir mensagem; logs do cliente perdem o `Result.Error` original; debugging em produção depende de log server-side.

**Alternativas consideradas:**
1. `SendStringAsync(result.Error, 400, ct)` — rejeitada por não respeitar o shape padrão FastEndpoints (`{errors: [...]}`).
2. Exceção custom + global exception handler que serializa — rejeitada por contradizer ADR-005 (Result pattern, sem throw para negócio).
3. **`AddError(result.Error)` + `SendErrorsAsync(cancellation: ct)`** — **escolhida**, respeita o shape FastEndpoints e mantém Result pattern.

## Decisão

Adotamos o seguinte mapeamento obrigatório de `Result<T>` para resposta HTTP em todo Endpoint de `modernized/`:

- **Sucesso** (`result.IsSuccess`): `await SendOkAsync(result.Value, ct)` → 200.
- **Falha de negócio** (`result.IsFailure`): `AddError(result.Error); await SendErrorsAsync(cancellation: ct)` → 400 com payload `{errors:[{...}]}`.
- **Recurso não encontrado**: `await SendNotFoundAsync(ct)` → 404 (sem body). Use quando a operação é leitura e o ID não existe; **não** use para regra de negócio que rejeitou criação.
- **Conflito de estado** (recurso já existe / transição inválida): `AddError(result.Error); await SendErrorsAsync(StatusCodes.Status409Conflict, cancellation: ct)` → 409.
- **Erro de infraestrutura**: deixe exceção propagar para middleware global; **não** capture em Endpoint.

Documentação Swagger (`Summary(s => s.Response(...))`) deve declarar **todos** os status codes que o Endpoint pode retornar.

## Regras Operacionais

1. **SEMPRE** chame `AddError(result.Error)` antes de `SendErrorsAsync`. *Padrão detectável:* `SendErrorsAsync` sem `AddError` precedente em `Domain/**/Features/**/Endpoint.cs`. *Motivo:* sem isso o cliente recebe payload vazio e perde a mensagem de negócio.
2. **SEMPRE** use `SendNotFoundAsync(ct)` quando recurso de leitura não existe. *Padrão detectável:* retorno `null` de query Dapper/EF em Endpoint ou Handler. *Motivo:* 404 é o status semântico correto; 400 mascara causa.
3. **NUNCA** capture exceção de infraestrutura no Endpoint. *Padrão detectável:* `try/catch` em `Endpoint.HandleAsync`. *Motivo:* infra deve subir para middleware global; Endpoint só mapeia `Result<T>` de negócio.
4. **SEMPRE** declare status codes no `Summary`: `.Response<TResponse>(200, ...).Response(400, ...).Response(404, ...).Response(500, ...)`. *Padrão detectável:* `Configure()` sem chamada a `Summary` em Endpoint que retorna Response. *Motivo:* contrato HTTP fica visível em Swagger para consumidores.
5. **PREFIRA** 409 (Conflict) a 400 quando a falha indica conflito de estado (recurso duplicado, transição inválida). *Motivo:* semântica HTTP correta facilita retry/idempotência do cliente.

## Exemplos

### ✅ DO

```csharp
// Domain/Subscriptions/Features/AddNewEnrollment/Endpoint.cs
public class Endpoint(Handler handler) : Endpoint<Request, Guid>
{
    public override void Configure()
    {
        Post("/api/enrollments");
        AllowAnonymous();
        Summary(s => s
            .Summary("Criar nova matrícula")
            .Response<Guid>(200, "Matrícula criada")
            .Response(400, "Dados de entrada inválidos")
            .Response(409, "Cliente já possui matrícula ativa")
            .Response(500, "Erro interno"));
        Tags("Subscriptions");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var result = await handler.HandleAsync(req, ct);
        if (result.IsFailure)
        {
            AddError(result.Error);                       // ⚠️ obrigatório
            await SendErrorsAsync(cancellation: ct);      // 400 com payload de erros
            return;
        }
        await SendOkAsync(result.Value, ct);
    }
}

// Endpoint de leitura com 404
public override async Task HandleAsync(Request req, CancellationToken ct)
{
    var plan = await connection.QueryFirstOrDefaultAsync<Response>(sql, parameters);
    if (plan == null)
    {
        await SendNotFoundAsync(ct);                      // 404 sem body
        return;
    }
    await SendOkAsync(plan, ct);
}
```

### ❌ DON'T

```csharp
// Por que está errado: SendErrorsAsync sem AddError envia errors:[] vazio. Cliente perde a mensagem.
public override async Task HandleAsync(Request req, CancellationToken ct)
{
    var result = await handler.HandleAsync(req, ct);
    if (result.IsFailure)
    {
        await SendErrorsAsync(cancellation: ct);          // payload {errors: []}
        return;
    }
    await SendOkAsync(result.Value, ct);
}
```

## Como Verificar Conformidade

- **Grep de anti-padrão**: `rg -B 2 "SendErrorsAsync" modernized/GymErp/Domain/ | rg -v "AddError"` (heurística).
- **Checklist manual**: todo Endpoint que recebe `Result<T>` do Handler precisa ter `AddError(result.Error);` antes de `SendErrorsAsync`.
- **Swagger**: abrir `/swagger` e validar que todos os status codes do Endpoint estão listados.

## Próximos Passos

1. Auditar todos `Domain/**/Features/**/Endpoint.cs` e adicionar `AddError(result.Error)` ausente (Cleanup C4 em `modernized/CLAUDE.md`).
2. Preencher `Summary(...)` ausente nos endpoints existentes (Cleanup C5).

## Relacionado

- [ADR-003](./adr-003-fastendpoints.md): refina o mapeamento de status codes não explicitado.
- [ADR-005](./adr-005-result-pattern.md): `Result<T>.Error` é a fonte da mensagem que vai em `AddError`.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
