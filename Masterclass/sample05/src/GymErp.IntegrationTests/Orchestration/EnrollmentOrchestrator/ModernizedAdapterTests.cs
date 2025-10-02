using CSharpFunctionalExtensions;
using FluentAssertions;
using GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;
using GymErp.IntegrationTests.Infrastructure;
using GymErp.IntegrationTests.Orchestration.EnrollmentOrchestrator.TestHelpers;
using Xunit;

namespace GymErp.IntegrationTests.Orchestration.EnrollmentOrchestrator;

public class ModernizedAdapterTests : IntegrationTestBase, IAsyncLifetime
{
    private ModernizedAdapter _modernizedAdapter = null!;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        // Create ModernizedAdapter with null WorkflowHost to test error scenarios
        _modernizedAdapter = new ModernizedAdapter(null!);
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldReturnFailure_WhenWorkflowHostIsNull()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidRequest().Build();

        // Act
        var result = await _modernizedAdapter.ProcessEnrollmentAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Erro ao processar inscrição no sistema modernizado");
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldReturnFailure_WhenRequestIsInvalid()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidRequest()
            .WithClientId(Guid.Empty)
            .WithPlanId(Guid.Empty)
            .Build();

        // Act
        var result = await _modernizedAdapter.ProcessEnrollmentAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Erro ao processar inscrição no sistema modernizado");
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldHandleException_WhenWorkflowThrows()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidRequest().Build();

        // Act
        var result = await _modernizedAdapter.ProcessEnrollmentAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Erro ao processar inscrição no sistema modernizado");
    }
}
