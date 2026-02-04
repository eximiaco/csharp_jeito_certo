using FluentAssertions;
using GymErp.Common;
using GymErp.Domain.Subscriptions;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.IntegrationTests.Infrastructure;
using Xunit;

namespace GymErp.IntegrationTests.Subscriptions;

public class EnrollmentStateTests : IntegrationTestBase, IAsyncLifetime
{
    private Enrollment _enrollment = null!;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();

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

    public new Task DisposeAsync()
    {
        return base.DisposeAsync();
    }

    [Fact]
    public void Create_ShouldStartWithSuspendedState()
    {
        _enrollment.State.Should().Be(EState.Suspended);
    }

    [Fact]
    public void Activate_ShouldChangeStateToActive_WhenSuspended()
    {
        var result = _enrollment.Activate();
        
        result.IsSuccess.Should().BeTrue();
        _enrollment.State.Should().Be(EState.Active);
    }

    [Fact]
    public void Activate_ShouldReturnFailure_WhenAlreadyActive()
    {
        _enrollment.Activate();
        var result = _enrollment.Activate();
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Inscrição já está ativa");
        _enrollment.State.Should().Be(EState.Active);
    }

    [Fact]
    public void Suspend_ShouldChangeStateToSuspended_WhenActive()
    {
        _enrollment.Activate();
        var startDate = DateTime.Now;
        var endDate = DateTime.Now.AddDays(30);
        var result = _enrollment.Suspend(startDate, endDate);

        result.IsSuccess.Should().BeTrue();
        _enrollment.State.Should().Be(EState.Suspended);
    }

    [Fact]
    public void Suspend_ShouldReturnFailure_WhenAlreadySuspended()
    {
        var startDate = DateTime.Now;
        var endDate = DateTime.Now.AddDays(30);
        var result = _enrollment.Suspend(startDate, endDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Inscrição já está suspensa");
        _enrollment.State.Should().Be(EState.Suspended);
    }

    [Fact]
    public void Cancel_ShouldChangeStateToCanceled_WhenActive()
    {
        _enrollment.Activate();
        var result = _enrollment.Cancel();
        
        result.IsSuccess.Should().BeTrue();
        _enrollment.State.Should().Be(EState.Canceled);
    }

    [Fact]
    public void Cancel_ShouldChangeStateToCanceled_WhenSuspended()
    {
        var result = _enrollment.Cancel();
        
        result.IsSuccess.Should().BeTrue();
        _enrollment.State.Should().Be(EState.Canceled);
    }

    [Fact]
    public void Cancel_ShouldReturnFailure_WhenAlreadyCanceled()
    {
        _enrollment.Cancel();
        var result = _enrollment.Cancel();
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Inscrição já está cancelada");
        _enrollment.State.Should().Be(EState.Canceled);
    }

    [Fact]
    public void Activate_ShouldReturnFailure_WhenCanceled()
    {
        _enrollment.Cancel();
        var result = _enrollment.Activate();
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Não é possível ativar uma inscrição cancelada");
        _enrollment.State.Should().Be(EState.Canceled);
    }

    [Fact]
    public void Suspend_ShouldReturnFailure_WhenCanceled()
    {
        _enrollment.Cancel();
        var startDate = DateTime.Now;
        var endDate = DateTime.Now.AddDays(30);
        var result = _enrollment.Suspend(startDate, endDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Não é possível suspender uma inscrição cancelada");
        _enrollment.State.Should().Be(EState.Canceled);
    }
} 