# ADR-004: Vertical Slice Architecture — Feature/Operação com REPR

> **Escopo:** exclusivamente `modernized/GymErp/` — **não se aplica** ao código em `legacy/`.

## Contexto

O projeto modernizado precisa de uma organização de código que elimine camadas horizontais (Services/, Repositories/, Controllers/) em favor de fatias verticais por caso de uso. Aplica-se a todo código de negócio em `modernized/GymErp/Domain/`.

Evidência: `Domain/Subscriptions/Features/AddNewEnrollment/` contém `Endpoint.cs`, `Handler.cs`, `Request.cs`, `Response.cs` — quatro arquivos, um caso de uso completo. O mesmo padrão se repete em `CancelEnrollment/`, `SuspendEnrollment/`, `GetPlanById/`, etc.

**Alternativas consideradas:**
1. Layered Architecture (Controller/Service/Repository) — usada no legado; rejeitada por acoplamento horizontal e dificuldade de navegação.
2. CQRS com MediatR — rejeitado pela complexidade de registro e indireção desnecessária para este tamanho de projeto.
3. Vertical Slice com REPR (Request-Endpoint-Response-Pattern) — **escolhido** por coesão máxima e zero dependência entre features.

## Decisão

Adotamos Vertical Slice Architecture no padrão REPR. Cada caso de uso reside em sua própria pasta `Features/{NomeFeature}/` dentro do contexto de domínio. A pasta contém obrigatoriamente `Endpoint.cs` e `Handler.cs`; opcionalmente `Request.cs`, `Response.cs` e `Command.cs` quando aplicável.

O Handler é injetado diretamente no Endpoint via construtor. Não há Service Layer — o Handler é o ponto único de lógica de negócio da operação.

## Regras Operacionais

1. **SEMPRE** crie uma pasta `Features/{NomeFeature}/` para cada novo caso de uso. *Padrão detectável:* `Domain/{Contexto}/Features/`. *Motivo:* a operação completa deve ser localizável em um único diretório.
2. **SEMPRE** nomeie os arquivos `Endpoint.cs`, `Handler.cs`, `Request.cs`, `Response.cs` — sem prefixo do nome da feature. *Padrão detectável:* arquivos com nome igual ao padrão dentro de `Features/{Feature}/`. *Motivo:* nomes curtos + namespace qualifica o contexto.
3. **NUNCA** chame o Handler de uma feature a partir do Handler de outra feature diretamente. *Padrão detectável:* `using GymErp.Domain.{Contexto}.Features.{OutraFeature}` dentro de um Handler. *Motivo:* features são independentes; coordenação deve ir por domínio ou orquestração.
4. **NUNCA** crie uma classe `Service` genérica que agrega múltiplos casos de uso. *Padrão detectável:* `class *Service` em `Features/`. *Motivo:* viola o princípio de fatia vertical isolada.
5. **PREFIRA** `record` para `Request` e `Response`. *Motivo:* imutabilidade e igualdade estrutural sem boilerplate.

## Exemplos

### ✅ DO

```csharp
// modernized/GymErp/Domain/Subscriptions/Features/AddNewEnrollment/Request.cs
public record Request(
    string Name, string Email, string Phone,
    string Document, DateTime BirthDate, string Gender, string Address);

// modernized/GymErp/Domain/Subscriptions/Features/AddNewEnrollment/Handler.cs
public class Handler(EnrollmentRepository repository, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
{
    public async Task<Result<Guid>> HandleAsync(Request request)
    {
        var result = Enrollment.Create(request.Name, request.Email, ...);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error);
        await repository.AddAsync(result.Value, cancellationToken);
        await unitOfWork.Commit(cancellationToken);
        return Result.Success(result.Value.Id);
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: Service genérico agrupa múltiplos casos de uso em uma classe
public class EnrollmentService(IEnrollmentRepository repo)
{
    public Task<Guid> AddAsync(AddRequest req) { ... }
    public Task CancelAsync(CancelRequest req) { ... }
    public Task SuspendAsync(SuspendRequest req) { ... }
}
```

## Estrutura de Arquivos

```
Domain/Subscriptions/Features/
├── AddNewEnrollment/
│   ├── Endpoint.cs
│   ├── Handler.cs
│   ├── Request.cs
│   └── Response.cs
├── CancelEnrollment/
│   ├── Endpoint.cs
│   ├── Handler.cs
│   ├── Request.cs
│   └── Response.cs
└── SuspendEnrollment/
    ├── Endpoint.cs
    ├── SuspendEnrollmentCommand.cs   ← Command quando aplicável
    └── SuspendEnrollmentHandler.cs
```

## Benefícios

- **Coesão máxima** — tudo relacionado a um caso de uso está junto.
- **Navegação trivial** — nome da feature → pasta → arquivo.
- **Sem acoplamento entre features** — cada fatia pode evoluir independentemente.

## Relacionado

- [ADR-003](./adr-003-fastendpoints.md): Endpoint.cs herda de FastEndpoints.
- [ADR-005](./adr-005-result-pattern.md): Handler retorna Result<T>.
- [ADR-007](./adr-007-autofac-modulos.md): Handler e Endpoint registrados no módulo Autofac.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
