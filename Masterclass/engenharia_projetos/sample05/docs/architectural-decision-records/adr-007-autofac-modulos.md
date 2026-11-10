# ADR-007: Autofac como Container de DI com Módulos por Domínio

> **Escopo:** exclusivamente `modernized/GymErp/` — **não se aplica** ao código em `legacy/`.

## Contexto

O projeto modernizado precisa de injeção de dependência com escopo de vida por request HTTP e capacidade de organizar registros por contexto de domínio. Aplica-se a todo código em `modernized/GymErp/`.

Evidência: `GymErp.csproj` referencia `Autofac 8.3.0` e `Autofac.Extensions.DependencyInjection 10.0.0`. `Program.cs` chama `UseServiceProviderFactory(new AutofacServiceProviderFactory())` e registra `CommonModule`, `SubscriptionsModule`, `OrchestrationModule`, `FinancialModule`. Cada módulo herda de `Autofac.Module` e registra os tipos do seu contexto.

**Alternativas consideradas:**
1. Microsoft DI nativo — rejeitado pela ausência de módulos nativos e necessidade de concentrar todos os registros em `Program.cs`.
2. Scrutor (Microsoft DI + assembly scanning) — rejeitado pela falta de controle explícito e por mascarar o que está registrado.
3. Autofac com módulos — **escolhido** por encapsulamento de registros por domínio e suporte a `InstancePerLifetimeScope`.

## Decisão

Adotamos Autofac como container de DI. Cada contexto de domínio possui um módulo (`{Contexto}Module : Module`) que registra todos os tipos do contexto. O `Program.cs` registra apenas os módulos, sem conhecer os tipos internos de cada domínio. Handlers e Endpoints são registrados com `InstancePerLifetimeScope` para isolamento por request.

## Regras Operacionais

1. **SEMPRE** crie um `{Contexto}Module.cs` em `Domain/{Contexto}/Infrastructure/` para registrar os tipos do contexto. *Padrão detectável:* `class *Module : Module` em `Infrastructure/`. *Motivo:* `Program.cs` não deve conhecer tipos internos de cada contexto.
2. **SEMPRE** registre Handlers e Endpoints com `.InstancePerLifetimeScope()`. *Padrão detectável:* `.InstancePerLifetimeScope()` nos `RegisterType<Handler>` e `RegisterType<Endpoint>`. *Motivo:* handlers têm dependências com DbContext e repositório com escopo de request.
3. **NUNCA** registre tipos de um contexto no módulo de outro contexto. *Padrão detectável:* `using GymErp.Domain.{ContextoA}` dentro de `{ContextoB}Module.cs`. *Motivo:* viola boundaries de domínio.
4. **SEMPRE** adicione o módulo no `Program.cs` dentro de `builder.Host.ConfigureContainer<ContainerBuilder>`. *Padrão detectável:* `builder.RegisterModule(new {Contexto}Module())` em `Program.cs`. *Motivo:* ponto único de composição da aplicação.

## Exemplos

### ✅ DO

```csharp
// modernized/GymErp/Domain/Subscriptions/Infrastructure/SubscriptionsModule.cs
public class SubscriptionsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();
        builder.RegisterType<SubscriptionsDbContext>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<EnrollmentRepository>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<AddNewEnrollment.Handler>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<AddNewEnrollment.Endpoint>().AsSelf().InstancePerLifetimeScope();
    }
}

// modernized/GymErp/Program.cs
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule(new CommonModule());
    container.RegisterModule(new SubscriptionsModule());
    container.RegisterModule(new OrchestrationModule());
    container.RegisterModule(new FinancialModule());
});
```

### ❌ DON'T

```csharp
// Por que está errado: todos os registros em Program.cs, sem separação por contexto
builder.Services.AddScoped<EnrollmentRepository>();
builder.Services.AddScoped<AddNewEnrollment.Handler>();
builder.Services.AddScoped<CancelEnrollment.Handler>();
// ... 50 linhas de registros misturados
```

## Benefícios

- **Encapsulamento de domínio** — cada contexto controla seus próprios registros.
- **Program.cs enxuto** — apenas módulos, não tipos individuais.
- **Testabilidade** — módulos podem ser instanciados isoladamente em testes.

## Relacionado

- [ADR-002](./adr-002-dominios-por-contexto.md): módulo reside em `Infrastructure/` do contexto.
- [ADR-004](./adr-004-vertical-slice.md): Handler e Endpoint registrados pelo módulo.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
