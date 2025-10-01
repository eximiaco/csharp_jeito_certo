using System;
using System.Linq;
using System.Threading.Tasks;
using Gymerp.Application.DTOs;
using Gymerp.Application.Interfaces;
using Gymerp.Application.Services;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Gymerp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;
using Moq;

namespace Gymerp.IntegrationTests.Services
{
    public class FullEnrollmentServiceTests : IAsyncLifetime
    {
        private readonly MsSqlContainer _container;
        private readonly ApplicationDbContext _context;
        private readonly IStudentRepository _studentRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IPhysicalAssessmentRepository _assessmentRepository;
        private readonly IPersonalRepository _personalRepository;
        private readonly IPaymentService _paymentService;
        private readonly FullEnrollmentService _service;

        public FullEnrollmentServiceTests()
        {
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("YourStrong!Passw0rd")
                .Build();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(_container.GetConnectionString())
                .Options;

            _context = new ApplicationDbContext(options);
            _studentRepository = new StudentRepository(_context);
            _planRepository = new PlanRepository(_context);
            _enrollmentRepository = new EnrollmentRepository(_context);
            _assessmentRepository = new PhysicalAssessmentRepository(_context);
            _personalRepository = new PersonalRepository(_context);

            // Mocks para dependências do PaymentService
            var paymentRepoMock = new Mock<IPaymentRepository>();
            var notificationServiceMock = new Mock<INotificationService>();
            _paymentService = new PaymentService(paymentRepoMock.Object, _enrollmentRepository, notificationServiceMock.Object);

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
            await _container.StartAsync();
            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            await _container.DisposeAsync();
        }

        [Fact]
        public async Task EnrollAsync_WithValidData_ShouldCreateEnrollmentAndAssessment()
        {
            // Arrange
            var plan = new Plan("Plano Teste", "Descrição Teste", 100m, 1);
            await _planRepository.AddAsync(plan);

            var personal = new Personal(
                "Personal Teste",
                "personal@teste.com",
                "11999999999",
                "12345678900",
                "Musculação"
            );
            await _personalRepository.AddAsync(personal);

            var dto = new FullEnrollmentDto
            {
                Student = new StudentDto
                {
                    Name = "Aluno Teste",
                    Email = "aluno@teste.com",
                    Phone = "11999999999",
                    Document = "12345678900",
                    BirthDate = DateTime.Now.AddYears(-20),
                    Gender = Gender.Male,
                    Address = "Endereço Teste"
                },
                PlanId = plan.Id,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(1),
                PhysicalAssessment = new PhysicalAssessmentDto
                {
                    PersonalId = personal.Id,
                    AssessmentDate = DateTime.Now.AddDays(1),
                    Weight = 70,
                    Height = 1.75m,
                    BodyFatPercentage = 15,
                    Notes = "Notas teste"
                }
            };

            // Act
            // O PaymentService está mockado, então pode ser necessário ajustar o mock para simular sucesso
            // Se necessário, configure o mock para retornar sucesso
            // (paymentRepoMock.Setup...)
            var enrollmentId = await _service.EnrollAsync(dto);

            // Assert
            var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId);
            Assert.NotNull(enrollment);
            Assert.Equal(plan.Id, enrollment.PlanId);
            Assert.Equal(dto.StartDate.Date, enrollment.StartDate.Date);
            Assert.Equal(dto.EndDate.Date, enrollment.EndDate.Date);

            var student = await _studentRepository.GetByIdAsync(enrollment.StudentId);
            Assert.NotNull(student);
            Assert.Equal(dto.Student.Name, student.Name);
            Assert.Equal(dto.Student.Email, student.Email);
            Assert.Equal(dto.Student.Document, student.Document);

            var assessment = (await _assessmentRepository.GetByDateAsync(dto.PhysicalAssessment.AssessmentDate))
                .FirstOrDefault(a => a.StudentId == student.Id);
            Assert.NotNull(assessment);
            Assert.Equal(personal.Id, assessment.PersonalId);
            Assert.Equal(dto.PhysicalAssessment.AssessmentDate.Date, assessment.AssessmentDate.Date);
            Assert.Equal(dto.PhysicalAssessment.Weight, assessment.Weight);
            Assert.Equal(dto.PhysicalAssessment.Height, assessment.Height);
            Assert.Equal(dto.PhysicalAssessment.BodyFatPercentage, assessment.BodyFatPercentage);
            Assert.Equal(dto.PhysicalAssessment.Notes, assessment.Notes);
        }
    }
} 