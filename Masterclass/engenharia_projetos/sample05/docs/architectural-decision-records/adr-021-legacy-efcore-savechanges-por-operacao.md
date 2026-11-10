# ADR-021: EF Core com SaveChangesAsync por Operação em Repositório (sem UnitOfWork)

> **Escopo:** exclusivamente `legacy/Gymerp.Infrastructure/Repositories/` — **não se aplica** ao código em `modernized/` (que usa IUnitOfWork — ADR-011).

## Contexto

O projeto legado usa EF Core para persistência e cada repositório chama `SaveChangesAsync()` ao final de cada operação de escrita. Não existe UnitOfWork explícito. Aplica-se a `legacy/Gymerp.Infrastructure/Repositories/`.

Evidência: `EnrollmentRepository.AddAsync()` e `UpdateAsync()` chamam `_context.SaveChangesAsync()` diretamente. O mesmo padrão está em todos os repositórios do projeto legado.

**Alternativas consideradas:**
1. UnitOfWork — adotado no modernizado; não introduzido no legado para não criar refatoração desnecessária em código em modo manutenção.
2. `SaveChangesAsync` centralizado no serviço — rejeitado pois exigiria expor o DbContext para a camada de Application.
3. `SaveChangesAsync` por operação no repositório — **escolhido** pela simplicidade e por ser o padrão já estabelecido no legado.

## Decisão

Adotamos o padrão de chamar `SaveChangesAsync()` dentro do repositório, ao final de cada método de escrita (`AddAsync`, `UpdateAsync`, `DeleteAsync`). Cada operação é uma transação atômica independente. O `ApplicationDbContext` é injetado diretamente nos repositórios via construtor.

## Regras Operacionais

1. **SEMPRE** chame `await _context.SaveChangesAsync()` no final de cada método de escrita do repositório. *Padrão detectável:* `SaveChangesAsync()` em `Infrastructure/Repositories/*Repository.cs`. *Motivo:* convenção do legado — cada operação é auto-suficiente.
2. **NUNCA** exponha `ApplicationDbContext` diretamente para `Application/Services/`. *Padrão detectável:* `ApplicationDbContext` como parâmetro de construtor em `Application/Services/`. *Motivo:* services de aplicação operam via interfaces de repositório.
3. **NUNCA** adicione `IUnitOfWork` ao projeto legado. *Motivo:* introduziria abstração nova em código em modo manutenção sem benefício proporcional ao risco.

## Exemplos

### ✅ DO

```csharp
// legacy/Gymerp.Infrastructure/Repositories/EnrollmentRepository.cs
public async Task AddAsync(Enrollment enrollment)
{
    await _context.Enrollments.AddAsync(enrollment);
    await _context.SaveChangesAsync();  // auto-commit por operação
}

public async Task UpdateAsync(Enrollment enrollment)
{
    _context.Enrollments.Update(enrollment);
    await _context.SaveChangesAsync();
}
```

### ❌ DON'T

```csharp
// Por que está errado: expõe DbContext para a camada de Application
public class FullEnrollmentService(ApplicationDbContext context, ...)
{
    public async Task<Guid> EnrollAsync(FullEnrollmentDto dto)
    {
        // manipulação direta do DbContext na camada de Application
        context.Students.Add(student);
        await context.SaveChangesAsync();
    }
}
```

## Riscos e Limitações

- **Risco:** sem UnitOfWork, duas operações em sequência (ex.: criar enrollment + criar assessment) não são atômicas — se a segunda falhar, a primeira já foi persistida. *Mitigação:* o `FullEnrollmentService` trata a falha lançando exceção; possível inconsistência de dados. Este é um dos motivadores da migração para o modernizado com sagas (ADR-008).

## Relacionado

- [ADR-011](./adr-011-efcore-dapper.md): o modernizado usa IUnitOfWork para atomicidade.
- [ADR-008](./adr-008-workflowcore-sagas.md): sagas no modernizado resolvem o problema de atomicidade entre operações.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
