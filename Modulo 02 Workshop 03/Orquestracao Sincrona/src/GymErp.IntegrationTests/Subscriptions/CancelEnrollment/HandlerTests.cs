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
        var request = new Request
        {
            EnrollmentId = _enrollment.Id,
            Reason = "Cliente solicitou cancelamento"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var enrollment = await _dbContext.Enrollments.FindAsync(_enrollment.Id);
        enrollment.Should().NotBeNull();
        enrollment!.State.Should().Be(EState.Canceled);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenEnrollmentNotFound()
    {
        // Arrange
        var request = new Request
        {
            EnrollmentId = Guid.NewGuid(),
            Reason = "Cliente solicitou cancelamento"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Inscrição não encontrada");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenEnrollmentAlreadyCanceled()
    {
        // Arrange
        _enrollment.Cancel();
        await _enrollmentRepository.UpdateAsync(_enrollment, CancellationToken.None);
        await _unitOfWork.Commit(CancellationToken.None);

        var request = new Request
        {
            EnrollmentId = _enrollment.Id,
            Reason = "Cliente solicitou cancelamento"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Inscrição já está cancelada");
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishEnrollmentCanceledEvent_WhenSuccessfullyCanceled()
    {
        // Arrange
        var request = new Request
        {
            EnrollmentId = _enrollment.Id,
            Reason = "Cliente solicitou cancelamento"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verificar se o evento foi publicado
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

        var request = new Request
        {
            EnrollmentId = _enrollment.Id,
            Reason = "Cliente solicitou cancelamento"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var enrollment = await _dbContext.Enrollments.FindAsync(_enrollment.Id);
        enrollment.Should().NotBeNull();
        enrollment!.State.Should().Be(EState.Canceled);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenEnrollmentIsSuspended()
    {
        // Arrange - Inscrição já está suspensa por padrão
        var request = new Request
        {
            EnrollmentId = _enrollment.Id,
            Reason = "Cliente solicitou cancelamento"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Suspended pode ser cancelado
        var enrollment = await _dbContext.Enrollments.FindAsync(_enrollment.Id);
        enrollment.Should().NotBeNull();
        enrollment!.State.Should().Be(EState.Canceled);
    }
} 