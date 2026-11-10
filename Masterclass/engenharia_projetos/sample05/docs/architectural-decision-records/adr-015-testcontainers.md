# ADR-015: TestContainers (PostgreSQL + Kafka) para Testes de Integração com Infra Real

> **Escopo:** exclusivamente `modernized/GymErp.IntegrationTests/` — **não se aplica** a `legacy/Gymerp.IntegrationTests/`.

## Contexto

Os testes de integração precisam validar a cadeia completa: Handler → Repository → DbContext → PostgreSQL e Handler → ServiceBus → Kafka. Usar mocks para infraestrutura nos testes de integração mascararia bugs de mapeamento, SQL e serialização de mensagens. Aplica-se a `modernized/GymErp.IntegrationTests/`.

Evidência: `Infrastructure/IntegrationTestBase.cs` cria `PostgreSqlContainer` e `KafkaContainer` via TestContainers. `GymErp.IntegrationTests.csproj` referencia `Testcontainers 4.0.0`, `Testcontainers.PostgreSql 4.0.0`, `Testcontainers.Kafka 4.0.0`. A classe base implementa `IAsyncLifetime` do xUnit para start/stop dos containers.

**Alternativas consideradas:**
1. Mock de `IUnitOfWork` e `IServiceBus` — rejeitado; esconde bugs de integração real.
2. Banco de dados compartilhado de test — rejeitado por acoplamento entre testes e estado compartilhado.
3. TestContainers com containers efêmeros por suite — **escolhido** por isolamento total e infra idêntica à produção.

## Decisão

Adotamos TestContainers para subir PostgreSQL e Kafka em containers Docker efêmeros durante os testes de integração. A classe base `IntegrationTestBase : IAsyncLifetime` gerencia o ciclo de vida dos containers (start no `InitializeAsync`, stop no `DisposeAsync`). O schema é criado com `EnsureCreatedAsync()` a cada execução.

## Regras Operacionais

1. **SEMPRE** herde testes de integração de `IntegrationTestBase` para ter PostgreSQL e Kafka disponíveis. *Padrão detectável:* `class *Tests : IntegrationTestBase` em `GymErp.IntegrationTests`. *Motivo:* garante containers provisionados antes dos testes.
2. **SEMPRE** implemente `IAsyncLifetime` na base de testes para gerenciar containers. *Padrão detectável:* `InitializeAsync` e `DisposeAsync` em `IntegrationTestBase`. *Motivo:* xUnit chama esses métodos antes/depois de cada test class.
3. **NUNCA** mock `EnrollmentRepository`, `IUnitOfWork`, `SubscriptionsDbContext` em testes de integração. *Padrão detectável:* `new Mock<EnrollmentRepository>` em `GymErp.IntegrationTests`. *Motivo:* testes de integração devem exercitar a stack real.
4. **SEMPRE** use `IIntegrationSpy` (Silverback.Testing) para verificar publicação de mensagens em vez de consumir do Kafka. *Padrão detectável:* `_spy.OutboundEnvelopes` em testes de integração. *Motivo:* evita timeout aguardando consumo; o spy intercepta no produtor.
5. **PREFIRA** `protected virtual Task SetupDatabase()` para inserir dados de setup específicos de cada suite. *Padrão detectável:* `override SetupDatabase()` em classes de teste. *Motivo:* evita duplicação de setup e mantém a base genérica.

## Exemplos

### ✅ DO

```csharp
// modernized/GymErp.IntegrationTests/Subscriptions/AddNewEnrollment/HandlerTests.cs
public class HandlerTests : IntegrationTestBase
{
    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldCreateEnrollment()
    {
        var handler = new Handler(
            new EnrollmentRepository(_dbContext),
            new UnitOfWork(_dbContext, _serviceBus),
            CancellationToken.None);

        var result = await handler.HandleAsync(new Request(
            "João Silva Ferreira", "joao@test.com", "11999999999",
            "529.982.247-25", DateTime.Now.AddYears(-25), "M", "Rua A, 123"));

        result.IsSuccess.Should().BeTrue();
        VerifyMessagePublished<EnrollmentCreatedEvent>();

        var enrollment = await _dbContext.Enrollments.FindAsync(result.Value);
        enrollment.Should().NotBeNull();
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: mock de repositório em teste de integração, não testa a persistência real
public class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldSucceed()
    {
        var mockRepo = new Mock<EnrollmentRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var handler = new Handler(mockRepo.Object, mockUow.Object, CancellationToken.None);
        // não valida nada no banco real
    }
}
```

## Benefícios

- **Testes confiáveis** — exercitam o mesmo stack (EF Core + Npgsql + Kafka) que produção usa.
- **Isolamento total** — cada suite tem seu próprio container, sem interferência.
- **Zero configuração externa** — não precisa de PostgreSQL ou Kafka instalados na máquina.

## Relacionado

- [ADR-014](./adr-014-xunit-fluentassertions-moq.md): stack de testes base.
- [ADR-009](./adr-009-silverback-kafka.md): KafkaContainer testa a integração com Silverback.
- [ADR-011](./adr-011-efcore-dapper.md): PostgreSqlContainer testa EF Core e Dapper.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
