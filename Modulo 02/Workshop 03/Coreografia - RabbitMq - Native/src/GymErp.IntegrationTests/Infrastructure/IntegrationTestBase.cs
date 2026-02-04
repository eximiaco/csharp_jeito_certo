using GymErp.Common;
using GymErp.Common.RabbitMq;
using GymErp.Domain.Subscriptions.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;
using FluentAssertions;

namespace GymErp.IntegrationTests.Infrastructure;

public class MockHostApplicationLifetime : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;

    public void StopApplication()
    {
    }
}

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainer _postgresContainer;
    protected readonly RabbitMqContainer _rabbitMqContainer;
    protected SubscriptionsDbContext _dbContext = null!;
    protected FakeServiceBus _serviceBus = null!;
    protected ServiceProvider _serviceProvider = null!;

    protected IntegrationTestBase()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithDatabase("gym_erp_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        var rabbitMqConfig = new RabbitMqConfig
        {
            Connection = new RabbitMqConnectionConfig
            {
                HostName = _rabbitMqContainer.Hostname,
                Port = _rabbitMqContainer.GetMappedPublicPort(5672),
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/"
            }
        };

        _serviceBus = new FakeServiceBus();

        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IHostApplicationLifetime, MockHostApplicationLifetime>();

        services.AddSingleton(rabbitMqConfig);
        services.AddSingleton<IOptions<RabbitMqConfig>>(new OptionsWrapper<RabbitMqConfig>(rabbitMqConfig));
        services.AddSingleton<IServiceBus>(_serviceBus);

        _serviceProvider = services.BuildServiceProvider();

        var options = new DbContextOptionsBuilder<SubscriptionsDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
        _dbContext = new SubscriptionsDbContext(options, _serviceBus);
        await _dbContext.Database.EnsureCreatedAsync();

        await SetupDatabase();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        _serviceProvider?.Dispose();
    }

    protected virtual Task SetupDatabase() => Task.CompletedTask;

    protected void VerifyMessagePublished<T>(int expectedCount = 1) where T : class
    {
        var messages = _serviceBus.GetPublished<T>();
        messages.Should().HaveCount(expectedCount);
    }

    protected async Task VerifyMessagePublishedInBroker<T>(string exchangeName, int expectedCount = 1, TimeSpan? timeout = null) where T : class
    {
        timeout ??= TimeSpan.FromSeconds(10);
        await Task.Delay(500);
        var messages = _serviceBus.GetPublished<T>();
        messages.Should().HaveCount(expectedCount);
    }

    protected void VerifyMessagePublished<T>(Func<T, bool> predicate, int expectedCount = 1) where T : class
    {
        var messages = _serviceBus.GetPublished<T>().Where(predicate).ToList();
        messages.Should().HaveCount(expectedCount);
    }

    protected void VerifyNoMessagesPublished()
    {
        _serviceBus.Published.Should().BeEmpty();
    }

    protected IEnumerable<T> GetPublishedMessages<T>() where T : class
    {
        return _serviceBus.GetPublished<T>();
    }
}
