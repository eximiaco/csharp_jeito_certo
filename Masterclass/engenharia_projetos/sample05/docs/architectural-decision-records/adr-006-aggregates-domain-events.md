# ADR-006: Aggregates com Factory Method Estático e Domain Events

> **Escopo:** exclusivamente `modernized/GymErp/Domain/` — **não se aplica** ao código em `legacy/`.

## Contexto

O contexto `Subscriptions` possui entidades de domínio com estado mutável e regras de negócio (transições de estado de matrícula). Aplica-se a todas as entidades em `Domain/Subscriptions/Aggreates/`.

Evidência: `Enrollment.cs` é `sealed`, tem construtor `private`, factory method `static Result<Enrollment> Create(...)`, métodos `Activate()`, `Cancel()`, `Suspend()` retornando `Result`, e chama `AddDomainEvent(new EnrollmentCreatedEvent(...))`. A classe herda de `Aggregate` (em `Common/Aggregate.cs`).

## Decisão

Adotamos o padrão de Aggregate com construtor privado e factory method estático que retorna `Result<T>`. Toda mutação de estado é feita via método de domínio (nunca por setter público). Domain events são disparados dentro do aggregate via `AddDomainEvent()` herdado de `Aggregate`. O padrão State (`IEnrollmentState`) encapsula as transições válidas por estado.

## Regras Operacionais

1. **SEMPRE** declare o construtor do aggregate como `private`. *Padrão detectável:* `private {NomeAggregate}()` em aggregates. *Motivo:* garante que a criação passe pela validação do factory method.
2. **SEMPRE** exponha criação via `static Result<T> Create(...)` retornando `Result`. *Padrão detectável:* `public static Result<` em `Aggreates/`. *Motivo:* falhas de criação são casos de negócio, não exceções.
3. **NUNCA** use `set` público em propriedades de aggregate. *Padrão detectável:* `{ get; set; }` (sem `private`) em classes que herdam de `Aggregate`. *Motivo:* mutações devem passar pelas regras de negócio nos métodos do aggregate.
4. **SEMPRE** dispare domain events dentro do aggregate via `AddDomainEvent()`. *Padrão detectável:* `AddDomainEvent(new ` em aggregates. *Motivo:* o aggregate é o único que sabe quando o evento ocorreu.
5. **PREFIRA** o padrão State (interface `IEnrollmentState`) para aggregates com múltiplos estados e transições. *Motivo:* evita `if/switch` de estado espalhados pelos métodos do aggregate.

## Exemplos

### ✅ DO

```csharp
// modernized/GymErp/Domain/Subscriptions/Aggreates/Enrollments/Enrollment.cs
public sealed class Enrollment : Aggregate
{
    private Enrollment() { }  // para EF Core

    private Enrollment(Guid id, Client client, DateTime requestDate, EState state)
    {
        Id = id;
        Client = client;
        RequestDate = requestDate;
        State = state;
        _state = EnrollmentStateFactory.CreateState(state);
    }

    public Guid Id { get; private set; }
    public EState State { get; private set; }

    public static Result<Enrollment> Create(Client client)
    {
        var validation = ValidateClient(client);
        if (validation.IsFailure) return Result.Failure<Enrollment>(validation.Error);

        var enrollment = new Enrollment(Guid.NewGuid(), client, DateTime.UtcNow, EState.Suspended);
        enrollment.AddDomainEvent(new EnrollmentCreatedEvent(enrollment.Id));
        return enrollment;
    }

    public Result Cancel()
    {
        var result = _state.Cancel(this);
        if (result.IsSuccess)
            AddDomainEvent(new EnrollmentCanceledEvent(Id, DateTime.UtcNow));
        return result;
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: construtor público, setter aberto, sem domain event
public class Enrollment
{
    public Guid Id { get; set; }
    public EState State { get; set; }

    public Enrollment(Guid id, EState state)
    {
        Id = id;
        State = state;
    }
}
```

## Benefícios

- **Invariantes garantidas** — nenhum código externo cria aggregate inválido.
- **Rastreabilidade** — domain events registram o que aconteceu, não apenas o estado final.
- **Estado seguro** — padrão State elimina transições inválidas.

## Relacionado

- [ADR-005](./adr-005-result-pattern.md): factory method usa Result.
- [ADR-009](./adr-009-silverback-kafka.md): domain events publicados via IServiceBus.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
