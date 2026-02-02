using FluentAssertions;
using GymErp.Common;
using GymErp.Domain.Subscriptions;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment;
using GymErp.Domain.Subscriptions.Infrastructure;
using GymErp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymErp.IntegrationTests.Subscriptions.CancelEnrollment;

public class HandlerTests : IntegrationTestBase, IAsyncLifetime
{
    private Handler _handler = null!;
    private EnrollmentRepository _enrollmentRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private EfDbContextAccessor<SubscriptionsDbContext> _dbContextAccessor = null!;
    private Enrollment _enrollment = null!;

    public HandlerTests() : base() { }

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _dbContextAccessor = new EfDbContextAccessor<SubscriptionsDbContext>(_dbContext);
        _enrollmentRepository = new EnrollmentRepository(_dbContextAccessor);
        _unitOfWork = new UnitOfWork(_dbContext);
        _handler = new Handler(_enrollmentRepository, _unitOfWork);

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
        await _enrollmentRepository.AddAsync(_enrollment, CancellationToken.None);
        await _unitOfWork.Commit(CancellationToken.None);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        _dbContextAccessor?.Dispose();
    }

    [Fact]
    public async Task HandleAsync_ShouldCancelEnrollment_WhenValidRequest()
    {
        // Arrange
        var command = new CancelEnrollmentCommand(_enrollment.Id, "Cliente solicitou cancelamento");

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var enrollment = await _dbContext.Enrollments.FindAsync(_enrollment.Id);
        enrollment.Should().NotBeNull();
        enrollment!.State.Should().Be(EState.Canceled);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenEnrollmentNotFound()
    {
        // Arrange
        var command = new CancelEnrollmentCommand(Guid.NewGuid(), "Cliente solicitou cancelamento");

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Inscrição não encontrada");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenEnrollmentAlreadyCanceled()
    {
        // Arrange
        _enrollment.Cancel();
        await _enrollmentRepository.UpdateAsync(_enrollment, CancellationToken.None);
        await _unitOfWork.Commit(CancellationToken.None);

        var command = new CancelEnrollmentCommand(_enrollment.Id, "Cliente solicitou cancelamento");

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Inscrição já está cancelada");
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishEnrollmentCanceledEvent_WhenSuccessfullyCanceled()
    {
        // Arrange
        var command = new CancelEnrollmentCommand(_enrollment.Id, "Cliente solicitou cancelamento");

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert - Verificar se o evento foi publicado (via SaveChangesAsync no SubscriptionsDbContext)
        await Task.Delay(1000); // Aguardar processamento assíncrono
        VerifyMessagePublished<EnrollmentCanceledEvent>();
    }

    [Fact]
    public async Task HandleAsync_ShouldChangeStateToCanceled_WhenEnrollmentIsActive()
    {
        // Arrange - Ativar a inscrição primeiro
        _enrollment.Activate();
        await _enrollmentRepository.UpdateAsync(_enrollment, CancellationToken.None);
        await _unitOfWork.Commit(CancellationToken.None);

        var command = new CancelEnrollmentCommand(_enrollment.Id, "Cliente solicitou cancelamento");

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var enrollment = await _dbContext.Enrollments.FindAsync(_enrollment.Id);
        enrollment.Should().NotBeNull();
        enrollment!.State.Should().Be(EState.Canceled);
    }

    [Fact]
    public async Task HandleAsync_ShouldCancel_WhenEnrollmentIsSuspended()
    {
        // Arrange - Inscrição já está suspensa por padrão
        var command = new CancelEnrollmentCommand(_enrollment.Id, "Cliente solicitou cancelamento");

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert - Suspended pode ser cancelado
        var enrollment = await _dbContext.Enrollments.FindAsync(_enrollment.Id);
        enrollment.Should().NotBeNull();
        enrollment!.State.Should().Be(EState.Canceled);
    }
} 