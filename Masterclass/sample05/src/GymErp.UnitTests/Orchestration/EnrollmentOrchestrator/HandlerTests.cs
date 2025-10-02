using CSharpFunctionalExtensions;
using FluentAssertions;
using Flurl.Http;
using Flurl.Http.Testing;
using GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;
using Microsoft.Extensions.Options;
using Moq;
using WorkflowCore.Interface;
using Xunit;

namespace GymErp.UnitTests.Orchestration.EnrollmentOrchestrator;

public class HandlerTests
{
    private readonly Mock<IOptions<LegacyApiConfiguration>> _mockConfiguration;
    private readonly Mock<IWorkflowHost> _mockWorkflowHost;
    private readonly LegacyAdapter _legacyAdapter;
    private readonly ModernizedAdapter _modernizedAdapter;
    private readonly Handler _handler;

    public HandlerTests()
    {
        _mockConfiguration = new Mock<IOptions<LegacyApiConfiguration>>();
        _mockWorkflowHost = new Mock<IWorkflowHost>();

        var configuration = new LegacyApiConfiguration
        {
            BaseUrl = "http://localhost:5000",
            TimeoutSeconds = 30,
            RetryAttempts = 3
        };

        _mockConfiguration.Setup(x => x.Value).Returns(configuration);

        _legacyAdapter = new LegacyAdapter(_mockConfiguration.Object);
        _modernizedAdapter = new ModernizedAdapter(_mockWorkflowHost.Object);
        _handler = new Handler(_legacyAdapter, _modernizedAdapter);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccessWithLegacySystem_WhenValidRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        var expectedEnrollmentId = Guid.NewGuid();

        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new LegacyEnrollmentResponse { EnrollmentId = expectedEnrollmentId });

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EnrollmentId.Should().Be(expectedEnrollmentId);
        result.Value.ProcessingSystem.Should().Be("Legacy");

        // Verificar que a chamada HTTP foi feita corretamente
        httpTest.ShouldHaveCalled("http://localhost:5000/api/enrollment/enroll")
            .WithVerb(HttpMethod.Post);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenLegacySystemReturnsError()
    {
        // Arrange
        var request = CreateValidRequest();

        using var httpTest = new HttpTest();
        httpTest.RespondWith("Internal Server Error", 500);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Erro ao processar inscrição no sistema legado");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenLegacySystemTimesOut()
    {
        // Arrange
        var request = CreateValidRequest();

        using var httpTest = new HttpTest();
        httpTest.SimulateTimeout();

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Erro ao processar inscrição no sistema legado");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenLegacySystemIsUnavailable()
    {
        // Arrange
        var request = CreateValidRequest();

        using var httpTest = new HttpTest();
        httpTest.RespondWith("Service Unavailable", 503);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Erro ao processar inscrição no sistema legado");
    }

    [Fact]
    public async Task HandleAsync_ShouldCallLegacyAdapter_WhenUseLegacySystemIsTrue()
    {
        // Arrange
        var request = CreateValidRequest();
        var expectedEnrollmentId = Guid.NewGuid();

        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new LegacyEnrollmentResponse { EnrollmentId = expectedEnrollmentId });

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProcessingSystem.Should().Be("Legacy");

        // Verificar que não houve interação com o workflow host (sistema modernizado)
        _mockWorkflowHost.Verify(x => x.StartWorkflow(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.Never);
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
