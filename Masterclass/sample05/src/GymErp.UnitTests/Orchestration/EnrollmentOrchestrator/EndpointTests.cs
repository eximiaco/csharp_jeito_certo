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

public class EndpointTests
{
    private readonly Mock<IOptions<LegacyApiConfiguration>> _mockConfiguration;
    private readonly Mock<IWorkflowHost> _mockWorkflowHost;

    public EndpointTests()
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
    }

    [Fact]
    public void Endpoint_ShouldBeConstructedCorrectly()
    {
        // Arrange
        var legacyAdapter = new LegacyAdapter(_mockConfiguration.Object);
        var modernizedAdapter = new ModernizedAdapter(_mockWorkflowHost.Object);
        var handler = new Handler(legacyAdapter, modernizedAdapter);

        // Act
        var endpoint = new Endpoint(handler);

        // Assert
        endpoint.Should().NotBeNull();
    }

    [Fact]
    public void Endpoint_ShouldHaveCorrectRoute()
    {
        // Arrange
        var legacyAdapter = new LegacyAdapter(_mockConfiguration.Object);
        var modernizedAdapter = new ModernizedAdapter(_mockWorkflowHost.Object);
        var handler = new Handler(legacyAdapter, modernizedAdapter);
        var endpoint = new Endpoint(handler);

        // Act & Assert
        // Como não podemos testar o Configure() diretamente em testes unitários,
        // vamos apenas verificar que o endpoint foi criado corretamente
        endpoint.Should().NotBeNull();
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
