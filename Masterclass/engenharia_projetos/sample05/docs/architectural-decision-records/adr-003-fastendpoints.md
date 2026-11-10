# ADR-003: FastEndpoints como Framework HTTP (sem MVC Controllers)

> **Escopo:** exclusivamente `modernized/GymErp/` — **não se aplica** ao código em `legacy/`.

## Contexto

O projeto `modernized/GymErp` é uma API ASP.NET Core. A escolha do framework HTTP determina como endpoints são declarados, validados e organizados. Aplica-se a todo endpoint HTTP em `modernized/GymErp/`.

Evidência: `GymErp.csproj` referencia `FastEndpoints 6.1.0` e `FastEndpoints.Swagger 6.1.0`. Todos os arquivos `Endpoint.cs` em `Domain/*/Features/*/` herdam de `Endpoint<TRequest, TResponse>`. O projeto legado usa `Gymerp.Api/Controllers/EnrollmentController.cs` (ASP.NET MVC).

**Alternativas consideradas:**
1. ASP.NET MVC Controllers — usado no legado; rejeitado por acoplar lógica à infraestrutura HTTP e dificultar Vertical Slice.
2. Minimal APIs — rejeitadas por ausência de convenções de organização integradas e validação nativa.
3. FastEndpoints — **escolhido** por suporte nativo a REPR pattern, validação fluente, Swagger integrado e baixo acoplamento.

## Decisão

Adotamos FastEndpoints como o único framework HTTP no projeto modernizado. Cada operação de API tem seu próprio `Endpoint.cs` que herda de `Endpoint<TRequest, TResponse>`. O handler de negócio é injetado diretamente no endpoint via construtor — sem MediatR, sem dispatcher intermediário.

## Regras Operacionais

1. **SEMPRE** declare endpoints herdando de `Endpoint<TRequest, TResponse>` ou `Endpoint<TRequest>`. *Padrão detectável:* `class Endpoint : Endpoint<` em `Features/**/Endpoint.cs`. *Motivo:* garante tipagem forte de request/response e integração com Swagger automático.
2. **NUNCA** use `[ApiController]` ou `ControllerBase` no projeto modernizado. *Padrão detectável:* grep `ControllerBase\|ApiController` em `modernized/`. *Motivo:* MVC e FastEndpoints não coexistem no mesmo projeto sem configuração extra.
3. **NUNCA** use MediatR como dispatcher entre Endpoint e Handler. *Padrão detectável:* `using MediatR` em `modernized/`. *Motivo:* chamadas diretas ao handler são mais simples, rastreáveis e não exigem registro extra.
4. **SEMPRE** configure a rota no método `Configure()` do endpoint. *Padrão detectável:* método `public override void Configure()` em `Endpoint.cs`. *Motivo:* centraliza metadados de rota, autenticação e autorização no endpoint.
5. **PREFIRA** mapear erros de `Result.IsFailure` para `SendErrorsAsync()` a lançar exceções. *Motivo:* mantém consistência com o Result pattern (ADR-005).

## Exemplos

### ✅ DO

```csharp
// modernized/GymErp/Domain/Subscriptions/Features/AddNewEnrollment/Endpoint.cs
public class Endpoint : Endpoint<Request, Guid>
{
    private readonly Handler _handler;

    public Endpoint(Handler handler) => _handler = handler;

    public override void Configure()
    {
        Post("/api/enrollments");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var result = await _handler.HandleAsync(req);
        if (result.IsFailure)
        {
            await SendErrorsAsync(cancellation: ct);
            return;
        }
        await SendOkAsync(result.Value, ct);
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: usa ControllerBase e MediatR, padrão do projeto legado
[ApiController]
[Route("api/enrollments")]
public class EnrollmentController : ControllerBase
{
    private readonly IMediator _mediator;
    public EnrollmentController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Request req)
        => Ok(await _mediator.Send(req));
}
```

## Benefícios

- **Vertical Slice natural** — cada endpoint é um arquivo isolado na pasta da feature.
- **Sem indireção desnecessária** — handler chamado diretamente, sem bus de comandos.
- **Swagger nativo** — FastEndpoints gera documentação sem atributos extras.

## Relacionado

- [ADR-004](./adr-004-vertical-slice.md): estrutura Endpoint+Handler+Request+Response.
- [ADR-005](./adr-005-result-pattern.md): como tratar erros retornados pelo handler.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
