using FluentAssertions;
using GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowCore.Interface;
using Xunit;

namespace GymErp.IntegrationTests.Orchestration.CancelEnrollmentFlow;

public class WorkflowTests : IntegrationTestBase, IAsyncLifetime
{
    private IWorkflowHost _workflowHost = null!;
    private Enrollment _enrollment = null!;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Configurar WorkflowCore para testes
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorkflow();
        
        // Registrar o workflow de cancelamento
        services.AddTransient<GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow.MainWorkflow>();
        
        // Mock dos settings
        var settings = new GymErp.Common.Settings.ServicesSettings
        {
            SubscriptionsUri = "http://localhost:5001",
            ProcessPaymentUri = "http://localhost:5002"
        };
        services.AddSingleton(Options.Create(settings));

        var serviceProvider = services.BuildServiceProvider();
        _workflowHost = serviceProvider.GetRequiredService<IWorkflowHost>();

        // Criar uma inscrição para os testes
        var client = new Client(
            "52998224725",
            "João da Silva Santos",
            "joao.silva@email.com",
            "11999999999",
            "Rua Exemplo, 123"
        );

        var result = Enrollment.Create(client);
        result.IsSuccess.Should().BeTrue();
        _enrollment = result.Value;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact]
    public void Workflow_ShouldBeDefinedCorrectly()
    {
        // Arrange & Act
        var workflow = new GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow.MainWorkflow();

        // Assert
        workflow.Id.Should().Be("cancel-enrollment-workflow");
        workflow.Version.Should().Be(1);
    }


    [Fact]
    public void WorkflowData_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var data = new CancelEnrollmentFlowData
        {
            EnrollmentId = Guid.NewGuid(),
            Reason = "Test reason",
            EnrollmentCanceled = false,
            RefundProcessed = false
        };

        // Assert
        data.EnrollmentId.Should().NotBeEmpty();
        data.Reason.Should().Be("Test reason");
        data.EnrollmentCanceled.Should().BeFalse();
        data.RefundProcessed.Should().BeFalse();
        data.RefundId.Should().BeNull();
    }
}
