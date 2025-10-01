using GymErp.Common;
using GymErp.Common.Infrastructure;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Silverback.Messaging.Publishing;
using Silverback.Testing;
using Silverback.Messaging.Broker;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Testcontainers;
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
    protected readonly KafkaContainer _kafkaContainer;
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

        // Configure services for testing
        var services = new ServiceCollection();

        // Add loggers (required by Silverback)
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        
        // Add IHostApplicationLifetime (required by Silverback)
        services.AddSingleton<IHostApplicationLifetime, MockHostApplicationLifetime>();

        // Add Silverback with real Kafka for testing
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                {
                    config.BootstrapServers = _kafkaContainer.GetBootstrapAddress(); 
                    config.SecurityProtocol = Confluent.Kafka.SecurityProtocol.Plaintext;
                })
                .AddOutbound<EnrollmentCreatedEvent>(endpoint => endpoint
                    .ProduceTo("enrollment-events"))
                .AddOutbound<EnrollmentCanceledEvent>(endpoint => endpoint
                    .ProduceTo("enrollment-events")))
            .AddIntegrationSpy();

        services.AddTransient<IServiceBus, SilverbackServiceBus>();

        _serviceProvider = services.BuildServiceProvider();
        _serviceBus = _serviceProvider.GetRequiredService<IServiceBus>();
        _spy = _serviceProvider.GetRequiredService<IIntegrationSpy>();

        // Conectar o broker Silverback
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
        // Desconectar o broker
        if (_serviceProvider != null)
        {
            var broker = _serviceProvider.GetRequiredService<IBroker>();
            await broker.DisconnectAsync();
        }

        await _dbContext.Database.EnsureDeletedAsync();
        await _postgresContainer.DisposeAsync();
        await _kafkaContainer.DisposeAsync();
        _serviceProvider?.Dispose();
    }

    protected virtual Task SetupDatabase() => Task.CompletedTask;

    /// <summary>
    /// Verifica se uma mensagem específica foi publicada no ServiceBus.
    /// </summary>
    /// <typeparam name="T">Tipo da mensagem</typeparam>
    /// <param name="expectedCount">Número de vezes que a mensagem deve ter sido publicada (padrão: 1)</param>
    protected void VerifyMessagePublished<T>(int expectedCount = 1) where T : class
    {
        var messages = _spy.OutboundEnvelopes.Where(e => e.Message is T).Select(e => (T)e.Message!);
        messages.Should().HaveCount(expectedCount);
    }

    /// <summary>
    /// Verifica se uma mensagem foi realmente publicada no tópico Kafka real.
    /// </summary>
    /// <typeparam name="T">Tipo da mensagem</typeparam>
    /// <param name="topicName">Nome do tópico</param>
    /// <param name="expectedCount">Número esperado de mensagens</param>
    /// <param name="timeout">Timeout para aguardar as mensagens</param>
    protected async Task VerifyMessagePublishedInKafkaTopic<T>(
        string topicName, 
        int expectedCount = 1, 
        TimeSpan? timeout = null) where T : class
    {
        timeout ??= TimeSpan.FromSeconds(10);
        
        // Aguardar um pouco para garantir que as mensagens foram processadas
        await Task.Delay(1000);
        
        // Verificar se as mensagens foram publicadas no spy
        var spyMessages = _spy.OutboundEnvelopes
            .Where(e => e.Message is T)
            .Select(e => (T)e.Message!)
            .ToList();
            
        spyMessages.Should().HaveCount(expectedCount);
        
        // Verificar se as mensagens foram realmente enviadas para o tópico correto
        var topicMessages = _spy.OutboundEnvelopes
            .Where(e => e.Message is T && 
                       e.Endpoint?.Name == topicName)
            .Select(e => (T)e.Message!)
            .ToList();
            
        topicMessages.Should().HaveCount(expectedCount);
        
        // Log para debug
        Console.WriteLine($"Mensagens encontradas no spy: {spyMessages.Count}");
        Console.WriteLine($"Mensagens encontradas no tópico {topicName}: {topicMessages.Count}");
    }

    /// <summary>
    /// Verifica se uma mensagem específica foi publicada no ServiceBus com um predicado.
    /// </summary>
    /// <typeparam name="T">Tipo da mensagem</typeparam>
    /// <param name="predicate">Predicado para validar a mensagem</param>
    /// <param name="expectedCount">Número de vezes que a mensagem deve ter sido publicada (padrão: 1)</param>
    protected void VerifyMessagePublished<T>(Func<T, bool> predicate, int expectedCount = 1) where T : class
    {
        var messages = _spy.OutboundEnvelopes
            .Where(e => e.Message is T)
            .Select(e => (T?)e.Message)
            .Where(m => m != null && predicate(m))
            .Cast<T>();
        messages.Should().HaveCount(expectedCount);
    }

    /// <summary>
    /// Verifica se nenhuma mensagem foi publicada no ServiceBus.
    /// </summary>
    protected void VerifyNoMessagesPublished()
    {
        _spy.OutboundEnvelopes.Should().BeEmpty();
    }


    /// <summary>
    /// Obtém todas as mensagens de um tipo específico do spy.
    /// </summary>
    /// <typeparam name="T">Tipo da mensagem</typeparam>
    /// <returns>Lista de mensagens do tipo especificado</returns>
    protected IEnumerable<T> GetPublishedMessages<T>() where T : class
    {
        return _spy.OutboundEnvelopes
            .Where(e => e.Message is T)
            .Select(e => (T?)e.Message)
            .Where(m => m != null)
            .Cast<T>();
    }
} 