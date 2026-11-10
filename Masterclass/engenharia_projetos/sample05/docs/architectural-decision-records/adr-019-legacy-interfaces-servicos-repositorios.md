# ADR-019: Interface para Todo Serviço e Repositório

> **Escopo:** exclusivamente `legacy/` — **não se aplica** ao código em `modernized/` (que usa injeção direta de concretos via Autofac).

## Contexto

O projeto legado define uma interface para cada serviço de aplicação e cada repositório de domínio. Aplica-se a `legacy/Gymerp.Application/Interfaces/` e `legacy/Gymerp.Domain/Interfaces/`.

Evidência: `Application/Interfaces/` contém `IEnrollmentService`, `IFullEnrollmentService`, `IPaymentService`, `IProcessPaymentService`, `IScheduleAssessmentService`, `IAttendanceCheckService`, `INotificationService`. `Domain/Interfaces/` contém `IEnrollmentRepository`, `IStudentRepository`, `IPlanRepository`, `IPaymentRepository`, `IPersonalRepository`, `IPhysicalAssessmentRepository`, `IAccessRecordRepository`, `IScheduledClassRepository`. Cada interface tem uma única implementação concreta.

## Decisão

Adotamos interfaces para todos os serviços de aplicação e repositórios de domínio. As interfaces de serviço vivem em `Gymerp.Application/Interfaces/`; as de repositório em `Gymerp.Domain/Interfaces/`. Os registros no DI nativo do ASP.NET Core usam `AddScoped<IInterface, Implementacao>()`.

## Regras Operacionais

1. **SEMPRE** crie uma interface `I{Nome}Service` em `Application/Interfaces/` antes de implementar um novo serviço. *Padrão detectável:* `I*Service.cs` em `Application/Interfaces/`. *Motivo:* permite mocking nos testes sem subir infraestrutura.
2. **SEMPRE** crie uma interface `I{Nome}Repository` em `Domain/Interfaces/` antes de implementar um novo repositório. *Padrão detectável:* `I*Repository.cs` em `Domain/Interfaces/`. *Motivo:* Domain define o contrato; Infrastructure implementa — inversão de dependência.
3. **SEMPRE** registre no DI como `AddScoped<IInterface, Concreto>()` em `Program.cs`. *Padrão detectável:* `AddScoped<I*,` em `Program.cs`. *Motivo:* consistência com o ciclo de vida por request e injetabilidade.
4. **NUNCA** injete diretamente a implementação concreta em controllers ou services do legado. *Padrão detectável:* parâmetros de construtor com tipo concreto `*Service` ou `*Repository` (sem prefixo `I`) em controllers e application services. *Motivo:* acoplamento direto impede mocking.

## Benefícios

- **Mocking fácil** — `new Mock<IEnrollmentService>()` nos testes sem subir banco.
- **Desacoplamento** — `Domain` não depende de `Infrastructure`.

## Relacionado

- [ADR-017](./adr-017-legacy-layered-architecture.md): interfaces são o mecanismo de inversão de dependência entre camadas.
- [ADR-023](./adr-023-legacy-xunit-testcontainers-mssql.md): interfaces são mockadas nos testes de integração.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
