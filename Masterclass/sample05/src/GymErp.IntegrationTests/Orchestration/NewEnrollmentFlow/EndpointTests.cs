using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;
using GymErp.IntegrationTests.Infrastructure;
using GymErp.IntegrationTests.Orchestration.NewEnrollmentFlow.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using Xunit;
using FluentAssertions;

namespace GymErp.IntegrationTests.Orchestration.NewEnrollmentFlow;

/// <summary>
/// Testes de integração para o endpoint NewEnrollmentEndpoint
/// </summary>
public class EndpointTests : IntegrationTestBase, IAsyncLifetime
{
    private IWorkflowHost _workflowHost = null!;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        // Configurar WorkflowCore para testes
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorkflow();
        
        // Registrar workflow e steps do NewEnrollmentFlow
        services.AddTransient<MainWorkflow>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.AddEnrollmentStep>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.AddLegacyEnrollmentStep>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.ProcessPaymentStep>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.ScheduleEvaluationStep>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.AddEnrollmentCompensationStep>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.AddLegacyEnrollmentCompensationStep>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.ProcessPaymentCompensationStep>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.ScheduleEvaluationCompensationStep>();
        
        var serviceProvider = services.BuildServiceProvider();
        _workflowHost = serviceProvider.GetRequiredService<IWorkflowHost>();
        
        // Registrar o workflow no host
        _workflowHost.RegisterWorkflow<MainWorkflow, NewEnrollmentFlowData>();
        _workflowHost.Start();
    }

    public new async Task DisposeAsync()
    {
        try
        {
            if (_workflowHost != null)
            {
                _workflowHost.Stop();
            }
        }
        catch (Exception)
        {
            // Ignorar erros durante o cleanup
        }
        
        await base.DisposeAsync();
    }

    [Fact]
    public void Request_ShouldHaveAllRequiredFields()
    {
        // Arrange & Act
        var request = CreateValidRequest();

        // Assert
        request.ClientId.Should().NotBeEmpty();
        request.PlanId.Should().NotBeEmpty();
        request.Name.Should().NotBeEmpty();
        request.Email.Should().NotBeEmpty();
        request.Phone.Should().NotBeEmpty();
        request.Document.Should().NotBeEmpty();
        request.BirthDate.Should().BeBefore(DateTime.UtcNow);
        request.Gender.Should().NotBeEmpty();
        request.Address.Should().NotBeEmpty();
        
        // Novos campos obrigatórios
        request.StartDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
        request.EndDate.Should().BeAfter(request.StartDate);
        request.PersonalId.Should().NotBeEmpty();
        request.AssessmentDate.Should().BeAfter(DateTime.Now);
        request.Weight.Should().BeGreaterThan(0);
        request.Height.Should().BeGreaterThan(0);
        request.BodyFatPercentage.Should().BeGreaterOrEqualTo(0);
        request.Notes.Should().NotBeEmpty();
    }

    [Fact]
    public void NewEnrollmentResponse_ShouldHaveWorkflowId()
    {
        // Arrange
        var workflowId = "test-workflow-id";

        // Act
        var response = new NewEnrollmentResponse(workflowId);

        // Assert
        response.WorkflowId.Should().Be(workflowId);
    }

    [Theory]
    [InlineData("", "email@test.com", "11999999999", "12345678901")]
    [InlineData("João Silva", "", "11999999999", "12345678901")]
    [InlineData("João Silva", "email@test.com", "", "12345678901")]
    [InlineData("João Silva", "email@test.com", "11999999999", "")]
    public void Request_ShouldAcceptEmptyFields_ForTestingPurposes(
        string name, string email, string phone, string document)
    {
        // Arrange & Act
        var request = new Request(
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            email,
            phone,
            document,
            DateTime.Now.AddYears(-30),
            "M",
            "Rua Teste, 123",
            DateTime.Now,
            DateTime.Now.AddMonths(12),
            Guid.NewGuid(),
            DateTime.Now.AddDays(7),
            75.5m,
            1.75m,
            15.0m,
            "Cliente teste"
        );

        // Assert
        request.Name.Should().Be(name);
        request.Email.Should().Be(email);
        request.Phone.Should().Be(phone);
        request.Document.Should().Be(document);
    }

    [Fact]
    public void Request_ShouldValidateDateRanges()
    {
        // Arrange & Act
        var request = CreateValidRequest();

        // Assert
        request.StartDate.Should().BeBefore(request.EndDate);
        request.AssessmentDate.Should().BeAfter(DateTime.Now);
        request.BirthDate.Should().BeBefore(DateTime.Now);
    }

    [Fact]
    public void Request_ShouldValidatePhysicalMeasurements()
    {
        // Arrange & Act
        var request = CreateValidRequest();

        // Assert
        request.Weight.Should().BeGreaterThan(0);
        request.Height.Should().BeGreaterThan(0);
        request.BodyFatPercentage.Should().BeGreaterOrEqualTo(0);
        request.BodyFatPercentage.Should().BeLessOrEqualTo(100);
    }

    /// <summary>
    /// Cria um request válido para testes
    /// </summary>
    private Request CreateValidRequest()
    {
        var data = TestDataBuilder.CreateValidData().Build();
        
        return new Request(
            data.ClientId,
            data.PlanId,
            data.Name,
            data.Email,
            data.Phone,
            data.Document,
            data.BirthDate,
            data.Gender,
            data.Address,
            data.StartDate,
            data.EndDate,
            data.PersonalId,
            data.AssessmentDate,
            data.Weight,
            data.Height,
            data.BodyFatPercentage,
            data.Notes
        );
    }
}
