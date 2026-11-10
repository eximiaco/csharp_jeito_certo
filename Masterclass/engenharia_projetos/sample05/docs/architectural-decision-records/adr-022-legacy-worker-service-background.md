# ADR-022: Worker Service (.NET IHostedService) para Tarefas em Background

> **Escopo:** exclusivamente `legacy/Gymerp.Worker/` — **não se aplica** ao código em `modernized/`.

## Contexto

O projeto legado precisa executar tarefas periódicas em background (verificação de presença, cobrança recorrente). Aplica-se a `legacy/Gymerp.Worker/`.

Evidência: `Gymerp.Worker/Program.cs` registra `services.AddHostedService<AttendanceCheckWorker>()`. `Gymerp.Worker/Services/AttendanceCheckService.cs` implementa a lógica de verificação de presença. O projeto `Gymerp.Worker` é um executável separado do `Gymerp.Api`.

## Decisão

Adotamos um projeto separado `Gymerp.Worker` como .NET Worker Service para tarefas em background. Cada tarefa periódica é implementada como `IHostedService` (ou `BackgroundService`). O worker compartilha as mesmas interfaces de repositório e serviço do `Gymerp.Application` e `Gymerp.Domain`, registrando suas próprias implementações via `ConfigureServices`.

## Regras Operacionais

1. **SEMPRE** implemente tarefas em background como `IHostedService` (ou herança de `BackgroundService`) no projeto `Gymerp.Worker`. *Padrão detectável:* `AddHostedService<*Worker>()` em `Gymerp.Worker/Program.cs`. *Motivo:* separação de processo — o worker não compartilha ciclo de vida com a API HTTP.
2. **SEMPRE** registre dependências do worker em `Gymerp.Worker/Program.cs` via `ConfigureServices`. *Padrão detectável:* `services.AddScoped<I*Repository,` em `Gymerp.Worker/Program.cs`. *Motivo:* o worker tem seu próprio host e precisa de registros próprios.
3. **NUNCA** adicione lógica de tarefa periódica diretamente em `Gymerp.Api`. *Padrão detectável:* `AddHostedService` em `Gymerp.Api/Program.cs`. *Motivo:* tarefas de background não fazem parte do ciclo de vida HTTP da API.

## Exemplos

### ✅ DO

```csharp
// legacy/Gymerp.Worker/Program.cs
Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<AttendanceCheckWorker>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IAttendanceCheckService, AttendanceCheckService>();
    });
```

### ❌ DON'T

```csharp
// Por que está errado: tarefa de background registrada no host HTTP da API
// legacy/Gymerp.Api/Program.cs
builder.Services.AddHostedService<AttendanceCheckWorker>();  // acoplado ao ciclo de vida da API
```

## Benefícios

- **Deploy independente** — worker pode ser escalado ou reiniciado sem afetar a API.
- **Isolamento de falha** — crash no worker não derruba a API.

## Relacionado

- [ADR-017](./adr-017-legacy-layered-architecture.md): Worker Service usa as mesmas interfaces de Domain e Application.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
