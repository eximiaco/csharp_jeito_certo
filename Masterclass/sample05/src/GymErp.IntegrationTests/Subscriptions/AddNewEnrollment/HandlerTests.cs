using FluentAssertions;
using GymErp.Common;
using GymErp.Domain.Subscriptions;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Features.AddNewEnrollment;
using GymErp.Domain.Subscriptions.Infrastructure;
using GymErp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymErp.IntegrationTests.Subscriptions.AddNewEnrollment;

public class HandlerTests : IntegrationTestBase, IAsyncLifetime
{
    private Handler _handler = null!;
    private EnrollmentRepository _enrollmentRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private EfDbContextAccessor<SubscriptionsDbContext> _dbContextAccessor = null!;

    public HandlerTests() : base() { }

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _dbContextAccessor = new EfDbContextAccessor<SubscriptionsDbContext>(_dbContext);
        _enrollmentRepository = new EnrollmentRepository(_dbContextAccessor);
        _unitOfWork = new UnitOfWork(_dbContext);
        _handler = new Handler(_enrollmentRepository, _unitOfWork, CancellationToken.None);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        _dbContextAccessor?.Dispose();
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateEnrollment_WhenValidRequest()
    {
        // Arrange
        var request = new Request
        {
            Name = "João da Silva Santos",
            Email = "joao.silva@email.com",
            Phone = "11999999999",
            Document = "52998224725", // CPF válido
            BirthDate = new DateTime(1990, 1, 1),
            Gender = "M",
            Address = "Rua Exemplo, 123"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var enrollment = await _dbContext.Enrollments.FindAsync(result.Value);
        enrollment.Should().NotBeNull();
        enrollment!.Client.Name.Should().Be(request.Name);
        enrollment.Client.Email.Should().Be(request.Email);
        enrollment.Client.Phone.Should().Be(request.Phone);
        enrollment.Client.Cpf.Should().Be(request.Document);
        enrollment.Client.Address.Should().Be(request.Address);
        enrollment.RequestDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        enrollment.State.Should().Be(EState.Suspended);
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishEnrollmentCreatedEvent_WhenValidRequest()
    {
        // Arrange
        var request = new Request
        {
            Name = "Maria da Silva Santos",
            Email = "maria.silva@email.com",
            Phone = "11888888888",
            Document = "52998224725", // CPF válido
            BirthDate = new DateTime(1985, 5, 15),
            Gender = "F",
            Address = "Rua das Flores, 456"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verificar se o evento de domínio foi publicado no tópico Kafka real
        await VerifyMessagePublishedInKafkaTopic<EnrollmentCreatedEvent>(
            "enrollment-events", 
            1);
    }

    [Theory]
    [InlineData("", "email@test.com", "11999999999", "52998224725", "1990-01-01", "M", "Rua Teste", "Nome não pode ser vazio")]
    [InlineData("João S.", "email@test.com", "11999999999", "52998224725", "1990-01-01", "M", "Rua Teste", "Nome deve ter pelo menos 10 caracteres")]
    [InlineData("João da Silva Santos", "", "11999999999", "52998224725", "1990-01-01", "M", "Rua Teste", "Email não pode ser vazio")]
    [InlineData("João da Silva Santos", "invalid-email", "11999999999", "52998224725", "1990-01-01", "M", "Rua Teste", "Email inválido")]
    [InlineData("João da Silva Santos", "email@test.com", "", "52998224725", "1990-01-01", "M", "Rua Teste", "Telefone não pode ser vazio")]
    [InlineData("João da Silva Santos", "email@test.com", "123", "52998224725", "1990-01-01", "M", "Rua Teste", "Telefone inválido")]
    [InlineData("João da Silva Santos", "email@test.com", "11999999999", "", "1990-01-01", "M", "Rua Teste", "CPF não pode ser vazio")]
    [InlineData("João da Silva Santos", "email@test.com", "11999999999", "12345678901", "1990-01-01", "M", "Rua Teste", "CPF inválido")]
    public async Task HandleAsync_ShouldReturnFailure_WhenInvalidRequest(
        string name,
        string email,
        string phone,
        string document,
        string birthDate,
        string gender,
        string address,
        string expectedError)
    {
        // Arrange
        var request = new Request
        {
            Name = name,
            Email = email,
            Phone = phone,
            Document = document,
            BirthDate = DateTime.Parse(birthDate),
            Gender = gender,
            Address = address
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(expectedError);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotPublishEvent_WhenValidationFails()
    {
        // Arrange
        var request = new Request
        {
            Name = "", // Nome inválido - vazio
            Email = "email@test.com",
            Phone = "11999999999",
            Document = "52998224725",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = "M",
            Address = "Rua Teste"
        };

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Nome não pode ser vazio");

        // Verificar que nenhum evento foi publicado
        VerifyNoMessagesPublished();
    }
} 