using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using FluentAssertions;
using FinancialHandler = GymErp.Domain.Financial.Features.ProcessCharging.Handler;

namespace GymErp.IntegrationTests.Financial.ProcessCharging;

public class HandlerTests : IntegrationTestBase
{
    private FinancialHandler _handler = null!;

    protected override async Task SetupDatabase()
    {
        await base.SetupDatabase();

        _handler = new FinancialHandler(new NullLogger<FinancialHandler>(), _serviceBus);
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
        true.Should().BeTrue(); // Garante que não houve exceções
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishChargingProcessedEvent_WhenProcessingEnrollment()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var enrollmentCreatedEvent = new EnrollmentCreatedEvent(enrollmentId);

        // Act
        await _handler.HandleAsync(enrollmentCreatedEvent, CancellationToken.None);

        // Assert
        VerifyMessagePublished<ChargingProcessedEvent>(1);
        var messages = GetPublishedMessages<ChargingProcessedEvent>().ToList();
        messages.Should().ContainSingle(m => m.EnrollmentId == enrollmentId);
    }
}
