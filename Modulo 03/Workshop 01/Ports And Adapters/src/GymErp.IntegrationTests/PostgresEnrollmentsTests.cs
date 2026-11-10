using GymErp.Adapters.NoOpNotifications;
using GymErp.Adapters.Postgres;
using GymErp.Core;
using GymErp.Core.Ports.Driving;
using GymErp.Domain.Enrollments;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GymErp.IntegrationTests;

// Mesmo fluxo do teste em memória, agora com o adaptador Postgres.
// Cada etapa usa um DbContext novo, como em requisições diferentes: é isso que pega defeitos de materialização.
public class PostgresEnrollmentsTests : IAsyncLifetime
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
            new NoOpEnrollmentEventNotifier());

    private static IForManagingEnrollments PortOver(SubscriptionsDbContext context) =>
        new EnrollmentManagement(new PostgresEnrollments(context));

    [Fact]
    public async Task Enrollment_saved_in_one_request_can_be_canceled_in_another()
    {
        Guid id;
        await using (var context = NewContext())
            id = (await PortOver(context).EnrollAsync(
                new EnrollRequest("João da Silva Santos", "joao.silva@email.com", "11999999999", "52998224725", "Rua Exemplo, 123"))).Value;

        await using (var context = NewContext())
            Assert.True((await PortOver(context).CancelAsync(new CancelRequest(id, "Mudou de cidade"))).IsSuccess);

        await using (var context = NewContext())
            Assert.Equal(EState.Canceled, (await context.Enrollments.SingleAsync(e => e.Id == id)).State);
    }
}
