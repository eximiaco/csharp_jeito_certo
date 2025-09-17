using GymErp.Common;
using GymErp.Domain.Subscriptions.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace GymErp.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainer _postgresContainer;
    protected SubscriptionsDbContext _dbContext = null!;
    protected Mock<IServiceBus> _serviceBusMock = null!;

    protected IntegrationTestBase()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithDatabase("gym_erp_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // Initialize ServiceBus mock first
        _serviceBusMock = new Mock<IServiceBus>();
        SetupServiceBusMock();

        var options = new DbContextOptionsBuilder<SubscriptionsDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
        _dbContext = new SubscriptionsDbContext(options, _serviceBusMock.Object);
        await _dbContext.Database.EnsureCreatedAsync();

        await SetupDatabase();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _postgresContainer.DisposeAsync();
    }

    protected virtual Task SetupDatabase() => Task.CompletedTask;

    /// <summary>
    /// Configure o comportamento do mock do ServiceBus.
    /// Por padrão, o PublishAsync retorna Task.CompletedTask.
    /// Override este método para configurar comportamentos específicos nos testes.
    /// </summary>
    protected virtual void SetupServiceBusMock()
    {
        _serviceBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Verifica se uma mensagem específica foi publicada no ServiceBus.
    /// </summary>
    /// <typeparam name="T">Tipo da mensagem</typeparam>
    /// <param name="times">Número de vezes que a mensagem deve ter sido publicada (padrão: uma vez)</param>
    protected void VerifyMessagePublished<T>(Times? times = null) where T : class
    {
        _serviceBusMock.Verify(
            x => x.PublishAsync(It.IsAny<T>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifica se uma mensagem específica foi publicada no ServiceBus com um predicado.
    /// </summary>
    /// <typeparam name="T">Tipo da mensagem</typeparam>
    /// <param name="predicate">Predicado para validar a mensagem</param>
    /// <param name="times">Número de vezes que a mensagem deve ter sido publicada (padrão: uma vez)</param>
    protected void VerifyMessagePublished<T>(Func<T, bool> predicate, Times? times = null) where T : class
    {
        _serviceBusMock.Verify(
            x => x.PublishAsync(It.Is<T>(msg => predicate(msg))),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifica se nenhuma mensagem foi publicada no ServiceBus.
    /// </summary>
    protected void VerifyNoMessagesPublished()
    {
        _serviceBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>()),
            Times.Never);
    }

    /// <summary>
    /// Reseta todas as verificações do mock do ServiceBus.
    /// Útil quando você quer verificar apenas as mensagens publicadas após um ponto específico.
    /// </summary>
    protected void ResetServiceBusMock()
    {
        _serviceBusMock.Reset();
        SetupServiceBusMock();
    }
} 