using GymErp.Domain.Enrollments;
using GymErp.Infrastructure.Persistence;
using GymErp.Infrastructure.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GymErp.IntegrationTests;

// Cada etapa usa um DbContext novo, como acontece em requisições diferentes.
// Reaproveitar o mesmo DbContext devolveria a instância já rastreada e esconderia
// defeitos de materialização (foi assim que o _state nulo passou despercebido).
public class EnrollmentPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = NewContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private SubscriptionsDbContext NewContext() =>
        new(new DbContextOptionsBuilder<SubscriptionsDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options,
            new NoOpEventPublisher());

    [Fact]
    public async Task Enrollment_loaded_in_a_new_context_can_be_canceled()
    {
        var enrollment = Enrollment.Create("João da Silva Santos", "joao.silva@email.com", "11999999999", "52998224725", "Rua Exemplo, 123").Value;

        await using (var context = NewContext())
        {
            await new EnrollmentRepository(context).AddAsync(enrollment);
            await new UnitOfWork(context).Commit();
        }

        await using (var context = NewContext())
        {
            var loaded = await new EnrollmentRepository(context).GetByIdAsync(enrollment.Id);
            Assert.NotNull(loaded);
            Assert.True(loaded.Cancel("Mudou de cidade").IsSuccess);
            await new UnitOfWork(context).Commit();
        }

        await using (var context = NewContext())
        {
            var reloaded = await new EnrollmentRepository(context).GetByIdAsync(enrollment.Id);
            Assert.Equal(EState.Canceled, reloaded!.State);
        }
    }
}
