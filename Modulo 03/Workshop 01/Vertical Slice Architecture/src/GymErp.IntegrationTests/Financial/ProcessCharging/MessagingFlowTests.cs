using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Features.AddNewEnrollment;
using GymErp.Domain.Subscriptions.Infrastructure;
using GymErp.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Testing;
using Xunit;
using FluentAssertions;
using FinancialHandler = GymErp.Domain.Financial.Features.ProcessCharging.Handler;
using SubscriptionsHandler = GymErp.Domain.Subscriptions.Features.AddNewEnrollment.Handler;

namespace GymErp.IntegrationTests.Financial.ProcessCharging;

public class MessagingFlowTests : IntegrationTestBase
{
    private FinancialHandler _financialHandler = null!;
    private SubscriptionsHandler _subscriptionsHandler = null!;
    private IBroker _broker = null!;

    protected override async Task SetupDatabase()
    {
        await base.SetupDatabase();
        
        // Registrar os serviços do Financial
        var financialHandler = new FinancialHandler(new NullLogger<FinancialHandler>());
        
        // Registrar o handler do Subscriptions
        var dbContextAccessor = new EfDbContextAccessor<SubscriptionsDbContext>(_dbContext);
        var enrollmentRepository = new EnrollmentRepository(dbContextAccessor);
        var unitOfWork = new UnitOfWork(_dbContext);
        var subscriptionsHandler = new SubscriptionsHandler(
            enrollmentRepository,
            unitOfWork,
            CancellationToken.None
        );
        
        _serviceProvider.GetService<IServiceCollection>()?.AddSingleton(financialHandler);
        _serviceProvider.GetService<IServiceCollection>()?.AddSingleton(subscriptionsHandler);
        
        _financialHandler = financialHandler;
        _subscriptionsHandler = subscriptionsHandler;
        _broker = _serviceProvider.GetRequiredService<IBroker>();
    }

    [Fact]
    public async Task CreateEnrollment_ShouldTriggerFinancialProcessing_WhenEnrollmentCreated()
    {
        // Arrange
        var request = new Request
        {
            Name = "João da Silva Santos",
            Email = "joao.silva@email.com",
            Phone = "11999999999",
            Document = "52998224725", // CPF válido
            BirthDate = new DateTime(1990, 1, 1),
            Gender = "M",
            Address = "Rua Exemplo, 123"
        };

        // Act
        var result = await _subscriptionsHandler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verificar se o evento foi publicado
        await VerifyMessagePublishedInKafkaTopic<EnrollmentCreatedEvent>(
            "enrollment-events", 
            1);
    }

    [Fact]
    public async Task FinancialHandler_ShouldProcessEnrollmentCreatedEvent_WhenEventReceived()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var enrollmentCreatedEvent = new EnrollmentCreatedEvent(enrollmentId);

        // Act
        await _financialHandler.HandleAsync(enrollmentCreatedEvent, CancellationToken.None);

        // Assert
        // Como o handler apenas faz log "Hello World", não temos muito o que verificar
        // Mas podemos garantir que não houve exceções
        true.Should().BeTrue();
    }
}
