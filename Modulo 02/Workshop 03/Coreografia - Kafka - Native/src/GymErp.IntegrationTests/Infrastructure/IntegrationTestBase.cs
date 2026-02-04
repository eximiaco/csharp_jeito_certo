using GymErp.Common;
using GymErp.Common.Kafka;
using GymErp.Domain.Subscriptions.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
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
    protected readonly KafkaContainer _kafkaContainer;
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

        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:latest")
            .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
            .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
            .WithEnvironment("KAFKA_CONFLUENT_SUPPORT_METRICS_ENABLE", "false")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _kafkaContainer.StartAsync();

        var kafkaConfig = new KafkaConfig
        {
            BootstrapServers = _kafkaContainer.GetBootstrapAddress()
        };

        _serviceBus = new FakeServiceBus();

        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IHostApplicationLifetime, MockHostApplicationLifetime>();

        services.AddSingleton(kafkaConfig);
        services.AddSingleton<IOptions<KafkaConfig>>(new OptionsWrapper<KafkaConfig>(kafkaConfig));
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
        await _kafkaContainer.DisposeAsync();
        _serviceProvider?.Dispose();
    }

    protected virtual Task SetupDatabase() => Task.CompletedTask;

    protected void VerifyMessagePublished<T>(int expectedCount = 1) where T : class
    {
        var messages = _serviceBus.GetPublished<T>();
        messages.Should().HaveCount(expectedCount);
    }

    protected async Task VerifyMessagePublishedInBroker<T>(string topicName, int expectedCount = 1, TimeSpan? timeout = null) where T : class
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
