using GymErp.Common;
using GymErp.Common.Infrastructure;
using GymErp.Common.RabbitMq;
using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using GymErp.Bootstrap;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;
using Silverback.Testing;
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
        // Mock implementation
    }
}

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainer _postgresContainer;
    protected readonly RabbitMqContainer _rabbitMqContainer;
    protected SubscriptionsDbContext _dbContext = null!;
    protected IServiceBus _serviceBus = null!;
    protected IIntegrationSpy _spy = null!;
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

        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IHostApplicationLifetime, MockHostApplicationLifetime>();

        services.AddSingleton(rabbitMqConfig);
        services.AddSingleton<IOptions<RabbitMqConfig>>(new OptionsWrapper<RabbitMqConfig>(rabbitMqConfig));

        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddRabbit())
            .AddEndpointsConfigurator<RabbitMqEndpointsConfigurator>()
            .AddIntegrationSpy();

        services.AddTransient<IServiceBus, SilverbackServiceBus>();

        _serviceProvider = services.BuildServiceProvider();
        _serviceBus = _serviceProvider.GetRequiredService<IServiceBus>();
        _spy = _serviceProvider.GetRequiredService<IIntegrationSpy>();

        var broker = _serviceProvider.GetRequiredService<IBroker>();
        await broker.ConnectAsync();

        var options = new DbContextOptionsBuilder<SubscriptionsDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
        _dbContext = new SubscriptionsDbContext(options, _serviceBus);
        await _dbContext.Database.EnsureCreatedAsync();

        await SetupDatabase();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
        {
            var broker = _serviceProvider.GetRequiredService<IBroker>();
            await broker.DisconnectAsync();
        }

        await _dbContext.Database.EnsureDeletedAsync();
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        _serviceProvider?.Dispose();
    }

    protected virtual Task SetupDatabase() => Task.CompletedTask;

    protected void VerifyMessagePublished<T>(int expectedCount = 1) where T : class
    {
        var messages = _spy.OutboundEnvelopes.Where(e => e.Message is T).Select(e => (T)e.Message!);
        messages.Should().HaveCount(expectedCount);
    }

    /// <summary>
    /// Verifica se uma mensagem foi publicada no broker (exchange RabbitMQ).
    /// </summary>
    protected async Task VerifyMessagePublishedInBroker<T>(
        string exchangeName,
        int expectedCount = 1,
        TimeSpan? timeout = null) where T : class
    {
        timeout ??= TimeSpan.FromSeconds(10);

        await Task.Delay(1000);

        var spyMessages = _spy.OutboundEnvelopes
            .Where(e => e.Message is T)
            .Select(e => (T)e.Message!)
            .ToList();

        spyMessages.Should().HaveCount(expectedCount);

        var topicMessages = _spy.OutboundEnvelopes
            .Where(e => e.Message is T &&
                        e.Endpoint?.Name == exchangeName)
            .Select(e => (T)e.Message!)
            .ToList();

        topicMessages.Should().HaveCount(expectedCount);

        Console.WriteLine($"Mensagens encontradas no spy: {spyMessages.Count}");
        Console.WriteLine($"Mensagens encontradas na exchange {exchangeName}: {topicMessages.Count}");
    }

    protected void VerifyMessagePublished<T>(Func<T, bool> predicate, int expectedCount = 1) where T : class
    {
        var messages = _spy.OutboundEnvelopes
            .Where(e => e.Message is T)
            .Select(e => (T?)e.Message)
            .Where(m => m != null && predicate(m))
            .Cast<T>();
        messages.Should().HaveCount(expectedCount);
    }

    protected void VerifyNoMessagesPublished()
    {
        _spy.OutboundEnvelopes.Should().BeEmpty();
    }

    protected IEnumerable<T> GetPublishedMessages<T>() where T : class
    {
        return _spy.OutboundEnvelopes
            .Where(e => e.Message is T)
            .Select(e => (T?)e.Message)
            .Where(m => m != null)
            .Cast<T>();
    }
}
