using GymErp.Common;
using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Financial.Features.ProcessCharging.Consumers;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using FluentAssertions;

namespace GymErp.IntegrationTests.Financial.ProcessCharging.Consumers;

public class EnrollmentCreatedEventConsumerTests
{
    [Fact]
    public async Task HandleAsync_ShouldDelegateToProcessChargingHandler_WhenEventReceived()
    {
        // Arrange: Consumer recebe o evento e chama o Handler; o Handler publica ChargingProcessedEvent via IServiceBus.
        // Verificamos que o Consumer não lança e que o Handler foi acionado (via publicação).
        var enrollmentId = Guid.NewGuid();
        var enrollmentCreatedEvent = new EnrollmentCreatedEvent(enrollmentId);
        var serviceBusMock = new Mock<IServiceBus>();
        serviceBusMock
            .Setup(s => s.PublishAsync(It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        var handler = new Handler(NullLogger<Handler>.Instance, serviceBusMock.Object);
        var consumer = new EnrollmentCreatedEventConsumer(handler);

        // Act
        await consumer.HandleAsync(enrollmentCreatedEvent, CancellationToken.None);

        // Assert: Handler foi chamado (Consumer delega) e publicou ChargingProcessedEvent
        serviceBusMock.Verify(
            s => s.PublishAsync(It.Is<ChargingProcessedEvent>(e => e.EnrollmentId == enrollmentId)),
            Times.Once);
    }
}
