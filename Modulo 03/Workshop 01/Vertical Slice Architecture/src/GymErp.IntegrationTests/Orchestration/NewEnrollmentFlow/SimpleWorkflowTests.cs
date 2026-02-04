using GymErp.Common.Settings;
using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;
using GymErp.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;

namespace GymErp.IntegrationTests.Orchestration.NewEnrollmentFlow;

/// <summary>
/// Testes simplificados para o NewEnrollmentFlow usando mocks básicos
/// </summary>
public class SimpleWorkflowTests : IntegrationTestBase, IAsyncLifetime
{
    protected IWorkflowHost WorkflowHost { get; private set; } = null!;

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
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.ProcessPaymentStep>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.ScheduleEvaluationStep>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.AddEnrollmentCompensationStep>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.ProcessPaymentCompensationStep>();
        services.AddTransient<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps.ScheduleEvaluationCompensationStep>();
        
        // Configurar settings para os serviços
        var servicesSettings = new ServicesSettings
        {
            SubscriptionsUri = "http://localhost:5001",
            ProcessPaymentUri = "http://localhost:5002",
            ScheduleEvaluationUri = "http://localhost:5003"
        };
        services.AddSingleton(Options.Create(servicesSettings));
        
        var serviceProvider = services.BuildServiceProvider();
        WorkflowHost = serviceProvider.GetRequiredService<IWorkflowHost>();
        
        // Registrar o workflow no host (esta é a parte que estava faltando!)
        WorkflowHost.RegisterWorkflow<MainWorkflow, NewEnrollmentFlowData>();
        
        // Iniciar o workflow host
        WorkflowHost.Start();
    }

    public new async Task DisposeAsync()
    {
        try
        {
            if (WorkflowHost != null)
            {
                WorkflowHost.Stop();
            }
        }
        catch (Exception)
        {
            // Ignorar erros durante o cleanup
        }
        
        await base.DisposeAsync();
    }

    [Fact]
    public void Workflow_ShouldBeDefinedCorrectly()
    {
        // Arrange & Act
        var workflow = new MainWorkflow();

        // Assert
        workflow.Id.Should().Be("new-enrollment-workflow");
        workflow.Version.Should().Be(1);
    }

    [Fact]
    public void WorkflowData_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var data = CreateValidWorkflowData();

        // Assert
        data.ClientId.Should().NotBeEmpty();
        data.PlanId.Should().NotBeEmpty();
        data.Name.Should().NotBeEmpty();
        data.Email.Should().NotBeEmpty();
        data.Phone.Should().NotBeEmpty();
        data.Document.Should().NotBeEmpty();
        data.BirthDate.Should().BeBefore(DateTime.UtcNow);
        data.Gender.Should().NotBeEmpty();
        data.Address.Should().NotBeEmpty();
        
        // Flags de estado
        data.EnrollmentCreated.Should().BeFalse();
        data.PaymentProcessed.Should().BeFalse();
        data.EvaluationScheduled.Should().BeFalse();
    }

    [Fact]
    public async Task Workflow_ShouldStartSuccessfully()
    {
        // Arrange
        var data = CreateValidWorkflowData();

        // Act
        var workflowId = await WorkflowHost.StartWorkflow("new-enrollment-workflow", data);

        // Assert
        workflowId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Workflow_ShouldThrowException_WhenInvalidWorkflowId()
    {
        // Arrange
        var data = CreateValidWorkflowData();

        // Act & Assert
        var act = async () => await WorkflowHost.StartWorkflow("invalid-workflow-id", data);
        await act.Should().ThrowAsync<WorkflowCore.Exceptions.WorkflowNotRegisteredException>();
    }

    [Fact]
    public async Task Workflow_ShouldHandleConcurrentExecutions()
    {
        // Arrange
        var data1 = CreateValidWorkflowData();
        var data2 = CreateValidWorkflowData();

        // Act
        var workflowId1 = await WorkflowHost.StartWorkflow("new-enrollment-workflow", data1);
        var workflowId2 = await WorkflowHost.StartWorkflow("new-enrollment-workflow", data2);

        // Assert
        workflowId1.Should().NotBeEmpty();
        workflowId2.Should().NotBeEmpty();
        workflowId1.Should().NotBe(workflowId2);
    }

    /// <summary>
    /// Cria dados válidos para o workflow NewEnrollment
    /// </summary>
    private NewEnrollmentFlowData CreateValidWorkflowData()
    {
        return new NewEnrollmentFlowData
        {
            ClientId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            Name = "João da Silva Santos",
            Email = "joao.silva@email.com",
            Phone = "11999999999",
            Document = "52998224725",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = "M",
            Address = "Rua Exemplo, 123"
        };
    }
}
