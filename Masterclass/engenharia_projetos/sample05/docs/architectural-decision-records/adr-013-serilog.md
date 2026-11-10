# ADR-013: Serilog para Logging Estruturado

> **Escopo:** exclusivamente `modernized/GymErp/` — **não se aplica** ao código em `legacy/`.

## Contexto

O projeto modernizado precisa de logging estruturado com suporte a enriquecimento de contexto (thread ID, nome da aplicação, versão). Aplica-se a todo logging em `modernized/GymErp/`.

Evidência: `GymErp.csproj` referencia `Serilog.AspNetCore 9.0.0`, `Serilog.Enrichers.Thread 4.0.0` e `Serilog.Extensions.Hosting 9.0.0`. `Program.cs` chama `builder.Host.UseSerilog()` e usa `Log.ForContext("ApplicationName", serviceName)` antes de qualquer log. `ServiceExtensions.cs` contém `AddLogs()` que configura o Serilog.

## Decisão

Adotamos Serilog como provedor de logging. A configuração é feita em `Bootstrap/ServiceExtensions.cs` via método de extensão `AddLogs()`. O host usa `UseSerilog()` para substituir o logging padrão do ASP.NET Core. Logs são enriquecidos com thread ID e nome da aplicação por padrão.

## Regras Operacionais

1. **SEMPRE** use `ILogger<T>` do `Microsoft.Extensions.Logging` (não `Serilog.ILogger` diretamente) no domínio. *Padrão detectável:* `ILogger<T>` em construtores de handlers e steps. *Motivo:* abstração permite trocar o provider sem mudar o domínio.
2. **SEMPRE** configure Serilog via `AddLogs()` em `ServiceExtensions.cs`, não inline em `Program.cs`. *Padrão detectável:* `.AddLogs(builder.Configuration)` em `Program.cs`. *Motivo:* centraliza a configuração de logging.
3. **SEMPRE** use `Log.ForContext("ApplicationName", serviceName)` antes de logs críticos em `Program.cs`. *Padrão detectável:* `Log.ForContext(` em `Program.cs`. *Motivo:* identifica a origem em ambientes com múltiplos serviços.
4. **NUNCA** use `Console.WriteLine` para logging de aplicação. *Padrão detectável:* `Console.WriteLine` fora de testes. *Motivo:* logs não estruturados impossibilitam filtragem e correlação.

## Benefícios

- **Logging estruturado** — logs como objetos JSON, não strings, permitem filtragem por campos.
- **Enriquecimento automático** — thread ID, app name adicionados sem código extra.
- **Configuração externalizada** — nível de log configurável via `appsettings.json`.

## Relacionado

- [ADR-002](./adr-002-dominios-por-contexto.md): `ServiceExtensions.cs` em `Bootstrap/` configura logging.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
