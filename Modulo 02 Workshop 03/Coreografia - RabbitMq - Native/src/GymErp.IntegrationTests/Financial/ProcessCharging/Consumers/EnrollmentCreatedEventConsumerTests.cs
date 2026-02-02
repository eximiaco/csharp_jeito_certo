using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using FluentAssertions;
using GymErp.Common;

namespace GymErp.IntegrationTests.Financial.ProcessCharging.Consumers;

public class EnrollmentCreatedEventConsumerTests
{
    [Fact]
    public async Task Handler_ShouldPublishChargingProcessedEvent_WhenHandleAsyncCalled()
    {
        // Arrange: Handler recebe EnrollmentCreatedEvent e publica ChargingProcessedEvent via IServiceBus.
        var enrollmentId = Guid.NewGuid();
        var enrollmentCreatedEvent = new EnrollmentCreatedEvent(enrollmentId);
        var serviceBusMock = new Mock<IServiceBus>();
        serviceBusMock
            .Setup(s => s.PublishAsync(It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        var handler = new Handler(NullLogger<Handler>.Instance, serviceBusMock.Object);

        // Act
        await handler.HandleAsync(enrollmentCreatedEvent, CancellationToken.None);

        // Assert: Handler publicou ChargingProcessedEvent
        serviceBusMock.Verify(
            s => s.PublishAsync(It.Is<ChargingProcessedEvent>(e => e.EnrollmentId == enrollmentId)),
            Times.Once);
    }
}
