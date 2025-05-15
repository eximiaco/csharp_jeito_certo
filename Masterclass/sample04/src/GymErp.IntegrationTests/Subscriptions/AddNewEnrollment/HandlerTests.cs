using FluentAssertions;
using GymErp.Common;
using GymErp.Domain.Subscriptions;
using Gymerp.Domain.Subscriptions.AddNewEnrollment;
using Gymerp.Domain.Subscriptions.Infrastructure;
using GymErp.Domain.Subscriptions.Infrastructure;
using GymErp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymErp.IntegrationTests.Subscriptions.AddNewEnrollment;

public class HandlerTests : IntegrationTestBase
{
    private Handler? _handler;
    private EnrollmentRepository? _enrollmentRepository;
    private IUnitOfWork? _unitOfWork;
    private SubscriptionsDbContext? _context;

    protected override async Task SetupDatabase()
    {
        var options = new DbContextOptionsBuilder<SubscriptionsDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _context = new SubscriptionsDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _enrollmentRepository = new EnrollmentRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
        _handler = new Handler(_enrollmentRepository, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateEnrollment_WhenValidRequest()
    {
        // Arrange
        var request = new Request
        {
            Name = "João Silva",
            Email = "joao@email.com",
            Phone = "11999999999",
            Document = "12345678900",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = "M",
            Address = "Rua Exemplo, 123"
        };

        // Act
        var enrollmentId = await _handler!.HandleAsync(request, CancellationToken.None);

        // Assert
        enrollmentId.Should().NotBeEmpty();

        var options = new DbContextOptionsBuilder<SubscriptionsDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        using var context = new SubscriptionsDbContext(options);
        var enrollment = await context.Enrollments.FindAsync(enrollmentId);
        
        enrollment.Should().NotBeNull();
        enrollment!.Client.Name.Should().Be(request.Name);
        enrollment.Client.Email.Should().Be(request.Email);
        enrollment.Client.Phone.Should().Be(request.Phone);
        enrollment.Client.Cpf.Should().Be(request.Document);
        enrollment.Client.Address.Should().Be(request.Address);
    }
} 