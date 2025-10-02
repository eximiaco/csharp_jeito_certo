using CSharpFunctionalExtensions;
using FluentAssertions;
using GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;
using GymErp.Domain.Subscriptions.Aggreates.Plans;
using GymErp.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WorkflowCore.Interface;
using Xunit;

namespace GymErp.IntegrationTests.Orchestration.EnrollmentOrchestrator;

public class HandlerTests : IntegrationTestBase, IAsyncLifetime
{
    private Handler _handler = null!;
    private LegacyAdapter _legacyAdapter = null!;
    private ModernizedAdapter _modernizedAdapter = null!;
    private PlanService _planService = null!;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        // Create real adapters with mocked configurations
        var legacyConfig = new LegacyApiConfiguration
        {
            BaseUrl = "http://localhost:5001",
            TimeoutSeconds = 30
        };
        var legacyOptions = Options.Create(legacyConfig);
        _legacyAdapter = new LegacyAdapter(legacyOptions);
        
        var workflowHost = Mock.Of<IWorkflowHost>();
        _modernizedAdapter = new ModernizedAdapter(workflowHost);
        
        // Create real PlanService with mocked configuration
        var configuration = new SubscriptionsApiConfiguration
        {
            BaseUrl = "http://localhost:5000",
            TimeoutSeconds = 30
        };
        var options = Options.Create(configuration);
        _planService = new PlanService(options);
        
        // Create handler with real dependencies
        _handler = new Handler(_legacyAdapter, _modernizedAdapter, _planService);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenPlanServiceFails()
    {
        // Arrange
        var request = CreateValidRequest();
        var planId = Guid.NewGuid();
        request.PlanId = planId;

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Erro ao buscar informações do plano");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenLegacyAdapterFails()
    {
        // Arrange
        var request = CreateValidRequest();
        var planId = Guid.NewGuid();
        request.PlanId = planId;

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Como o PlanService vai falhar (não há servidor real), o erro será sobre buscar informações do plano
        result.Error.Should().Contain("Erro ao buscar informações do plano");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenModernizedAdapterFails()
    {
        // Arrange
        var request = CreateValidRequest();
        var planId = Guid.NewGuid();
        request.PlanId = planId;

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Como o PlanService vai falhar (não há servidor real), o erro será sobre buscar informações do plano
        result.Error.Should().Contain("Erro ao buscar informações do plano");
    }

    private static Request CreateValidRequest()
    {
        return new Request
        {
            ClientId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(31),
            Student = new StudentDto
            {
                Name = "João da Silva Santos",
                Email = "joao.silva@email.com",
                Phone = "11999999999",
                Document = "12345678901",
                BirthDate = new DateTime(1990, 1, 1),
                Gender = "M",
                Address = "Rua das Flores, 123"
            },
            PhysicalAssessment = new PhysicalAssessmentDto
            {
                PersonalId = Guid.NewGuid(),
                AssessmentDate = DateTime.UtcNow,
                Weight = 75.5m,
                Height = 175.0m,
                BodyFatPercentage = 15.0m,
                Notes = "Avaliação física inicial"
            }
        };
    }
}
