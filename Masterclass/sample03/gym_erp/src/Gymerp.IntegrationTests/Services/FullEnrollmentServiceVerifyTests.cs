using Gymerp.Application.DTOs;
using Gymerp.Application.Interfaces;
using Gymerp.Application.Services;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Gymerp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;
using Gymerp.Application.Models;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace Gymerp.IntegrationTests.Services;

public class FullEnrollmentServiceVerifyTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly ApplicationDbContext _context;
    private readonly IStudentRepository _studentRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IPhysicalAssessmentRepository _assessmentRepository;
    private readonly IPersonalRepository _personalRepository;
    private readonly IPaymentService _paymentService;
    private readonly FullEnrollmentService _service;

    public FullEnrollmentServiceVerifyTests(ITestOutputHelper output)
    {
        _output = output;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _studentRepository = new StudentRepository(_context);
        _planRepository = new PlanRepository(_context);
        _enrollmentRepository = new EnrollmentRepository(_context);
        _assessmentRepository = new PhysicalAssessmentRepository(_context);
        _personalRepository = new PersonalRepository(_context);

        var paymentServiceMock = new Mock<IPaymentService>();
        paymentServiceMock
            .Setup(x => x.ProcessAsync(It.IsAny<Enrollment>()))
            .ReturnsAsync(new PaymentResult { Success = true, Message = "Pagamento processado com sucesso" });

        _paymentService = paymentServiceMock.Object;
        _service = new FullEnrollmentService(
            _studentRepository,
            _planRepository,
            _enrollmentRepository,
            _assessmentRepository,
            _personalRepository,
            _paymentService
        );
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }
    
    public async Task EnrollAsync_WithValidData_ShouldCreateEnrollmentAndAssessment()
    {
        // Arrange
        var student = new Student(
            "João Silva",
            "joao@email.com",
            "11999999999",
            "12345678900",
            DateTime.Now.AddYears(-20),
            Gender.Male,
            "Rua Teste, 123"
        );
        await _studentRepository.AddAsync(student);

        var plan = new Plan(
            "Plano Mensal",
            "Acesso à academia por 30 dias",
            100.00m,
            30
        );
        await _planRepository.AddAsync(plan);

        var personal = new Personal(
            "Maria Personal",
            "maria@email.com",
            "11988888888",
            "98765432100",
            "Musculação"
        );
        await _personalRepository.AddAsync(personal);

        var enrollmentDto = new FullEnrollmentDto
        {
            Student = new StudentDto
            {
                Name = student.Name,
                Email = student.Email,
                Phone = student.Phone,
                Document = student.Document,
                Address = student.Address
            },
            PlanId = plan.Id,
            PhysicalAssessment = new PhysicalAssessmentDto
            {
                Weight = 80.5m,
                Height = 1.75m,
                BodyFatPercentage = 20.0m,
                PersonalId = personal.Id
            }
        };

        // Act
        var result = await _service.EnrollAsync(enrollmentDto);

        // Assert
        await Verifier.Verify(result)
            .UseDirectory("Snapshots")
            .UseFileName("EnrollAsync_WithValidData_ShouldCreateEnrollmentAndAssessment")
            .DontScrubDateTimes()
            .DontScrubGuids();
    }
} 