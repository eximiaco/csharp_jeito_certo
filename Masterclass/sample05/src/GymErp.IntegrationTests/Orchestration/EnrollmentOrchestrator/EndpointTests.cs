using FluentAssertions;
using GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;
using GymErp.Domain.Subscriptions.Aggreates.Plans;
using GymErp.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WorkflowCore.Interface;
using Xunit;

namespace GymErp.IntegrationTests.Orchestration.EnrollmentOrchestrator;

public class EndpointTests : IntegrationTestBase, IAsyncLifetime
{
    private HttpClient _httpClient = null!;
    private PlanService _planService = null!;
    private LegacyAdapter _legacyAdapter = null!;
    private ModernizedAdapter _modernizedAdapter = null!;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        // Setup mocks
        // Create real PlanService with mocked configuration
        var configuration = new SubscriptionsApiConfiguration
        {
            BaseUrl = "http://localhost:5000",
            TimeoutSeconds = 30
        };
        var options = Options.Create(configuration);
        _planService = new PlanService(options);
        
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
        
        // Create HTTP client for testing
        _httpClient = new HttpClient();
    }

    public new async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        await base.DisposeAsync();
    }

    // TODO: Implementar teste de endpoint quando a aplicação estiver configurada corretamente
    // [Fact]
    // public async Task Post_ShouldReturnBadRequest_WhenPlanServiceFails()
    // {
    //     // Arrange
    //     var request = CreateValidRequest();
    //     var planId = Guid.NewGuid();
    //     request.PlanId = planId;

    //     // Act
    //     var response = await _httpClient.PostAsJsonAsync("http://localhost:5000/api/orchestrator/enroll", request);

    //     // Assert
    //     response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
    //     var content = await response.Content.ReadAsStringAsync();
    //     content.Should().Contain("Erro ao buscar informações do plano");
    // }

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
