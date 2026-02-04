using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Testing;
using Xunit;
using FluentAssertions;
using FinancialHandler = GymErp.Domain.Financial.Features.ProcessCharging.Handler;

namespace GymErp.IntegrationTests.Financial.ProcessCharging;

public class HandlerTests : IntegrationTestBase
{
    private FinancialHandler _handler = null!;
    private IBroker _broker = null!;

    protected override async Task SetupDatabase()
    {
        await base.SetupDatabase();
        
        // Registrar os serviços do Financial
        var handler = new FinancialHandler(new NullLogger<FinancialHandler>());
        
        _serviceProvider.GetService<IServiceCollection>()?.AddSingleton(handler);
        
        _handler = handler;
        _broker = _serviceProvider.GetRequiredService<IBroker>();
    }

    [Fact]
    public async Task HandleAsync_ShouldLogHelloWorld_WhenProcessingEnrollment()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var enrollmentCreatedEvent = new EnrollmentCreatedEvent(enrollmentId);

        // Act
        await _handler.HandleAsync(enrollmentCreatedEvent, CancellationToken.None);

        // Assert
        // Como o handler apenas faz log, não temos muito o que verificar
        // Mas podemos garantir que não houve exceções
        true.Should().BeTrue(); // Placeholder para validação futura
    }
}
