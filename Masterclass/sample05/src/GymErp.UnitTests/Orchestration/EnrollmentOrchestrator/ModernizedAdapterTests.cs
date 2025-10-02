using CSharpFunctionalExtensions;
using FluentAssertions;
using GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;
using Moq;
using WorkflowCore.Interface;
using Xunit;

namespace GymErp.UnitTests.Orchestration.EnrollmentOrchestrator;

public class ModernizedAdapterTests
{
    private readonly Mock<IWorkflowHost> _mockWorkflowHost;
    private readonly ModernizedAdapter _adapter;

    public ModernizedAdapterTests()
    {
        _mockWorkflowHost = new Mock<IWorkflowHost>();
        _adapter = new ModernizedAdapter(_mockWorkflowHost.Object);
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldReturnFailure_WhenCalled()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = await _adapter.ProcessEnrollmentAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Sistema modernizado ainda não está disponível para processamento de inscrições");
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldNotCallWorkflowHost_WhenCalled()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        await _adapter.ProcessEnrollmentAsync(request);

        // Assert
        // Verificar que o workflow host não foi chamado, pois a implementação ainda não está disponível
        _mockWorkflowHost.Verify(x => x.StartWorkflow(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldReturnConsistentErrorMessage_WhenCalledMultipleTimes()
    {
        // Arrange
        var request1 = CreateValidRequest();
        var request2 = CreateValidRequest();

        // Act
        var result1 = await _adapter.ProcessEnrollmentAsync(request1);
        var result2 = await _adapter.ProcessEnrollmentAsync(request2);

        // Assert
        result1.IsFailure.Should().BeTrue();
        result2.IsFailure.Should().BeTrue();
        result1.Error.Should().Be(result2.Error);
        result1.Error.Should().Be("Sistema modernizado ainda não está disponível para processamento de inscrições");
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldHandleDifferentRequests_Consistently()
    {
        // Arrange
        var request1 = CreateValidRequest();
        var request2 = CreateValidRequest();
        request2.ClientId = Guid.NewGuid();
        request2.Student.Name = "Maria Silva";

        // Act
        var result1 = await _adapter.ProcessEnrollmentAsync(request1);
        var result2 = await _adapter.ProcessEnrollmentAsync(request2);

        // Assert
        result1.IsFailure.Should().BeTrue();
        result2.IsFailure.Should().BeTrue();
        result1.Error.Should().Be(result2.Error);
    }

    private static Request CreateValidRequest()
    {
        return new Request
        {
            ClientId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(12),
            Student = new StudentDto
            {
                Name = "João da Silva Santos",
                Email = "joao.silva@email.com",
                Phone = "11999999999",
                Document = "52998224725",
                BirthDate = new DateTime(1990, 1, 1),
                Gender = "M",
                Address = "Rua das Flores, 123"
            },
            PhysicalAssessment = new PhysicalAssessmentDto
            {
                PersonalId = Guid.NewGuid(),
                AssessmentDate = DateTime.UtcNow,
                Weight = 75.5m,
                Height = 1.75m,
                BodyFatPercentage = 15.0m,
                Notes = "Avaliação física inicial"
            }
        };
    }
}
