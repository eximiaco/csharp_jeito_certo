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
        private ApplicationDbContext _context;
        private IStudentRepository _studentRepository;
        private IPlanRepository _planRepository;
        private IEnrollmentRepository _enrollmentRepository;
        private IPhysicalAssessmentRepository _assessmentRepository;
        private IPersonalRepository _personalRepository;
        private IPaymentService _paymentService;
        private FullEnrollmentService _service;

        public FullEnrollmentServiceTests()
        {
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("YourStrong!Passw0rd")
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(_container.GetConnectionString())
                .Options;
            _context = new ApplicationDbContext(options);
            _studentRepository = new StudentRepository(_context);
            _planRepository = new PlanRepository(_context);
            _enrollmentRepository = new EnrollmentRepository(_context);
            _assessmentRepository = new PhysicalAssessmentRepository(_context);
            _personalRepository = new PersonalRepository(_context);

            // Mock do PaymentService
            var paymentServiceMock = new Mock<IPaymentService>();
            paymentServiceMock.Setup(x => x.ProcessAsync(It.IsAny<Enrollment>()))
                .ReturnsAsync(new Gymerp.Application.Models.PaymentResult { Success = true, Message = "Pagamento aprovado" });
            _paymentService = paymentServiceMock.Object;

            _service = new FullEnrollmentService(
                _studentRepository,
                _planRepository,
                _enrollmentRepository,
                _assessmentRepository,
                _personalRepository,
                _paymentService
            );
            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
                await _context.DisposeAsync();
            await _container.DisposeAsync();
            _studentRepository = null;
            _planRepository = null;
            _enrollmentRepository = null;
            _assessmentRepository = null;
            _personalRepository = null;
            _paymentService = null;
            _service = null;
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
            var enrollmentId = await _service.EnrollAsync(dto);

            // Assert
            var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId.Id);
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

        [Fact]
        public async Task EnrollAsync_WithExistingStudent_ShouldReuseStudent()
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

            var existingStudent = new Student(
                "Aluno Existente",
                "aluno.existente@teste.com",
                "11999999999",
                "12345678900",
                DateTime.Now.AddYears(-20),
                Gender.Male,
                "Endereço Teste"
            );
            await _studentRepository.AddAsync(existingStudent);

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
            var enrollmentId = await _service.EnrollAsync(dto);

            // Assert
            var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId.Id);
            Assert.NotNull(enrollment);
            Assert.Equal(existingStudent.Id, enrollment.StudentId);
        }

        [Fact]
        public async Task EnrollAsync_WithInvalidPlan_ShouldThrowException()
        {
            // Arrange
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
                PlanId = Guid.NewGuid(), // Plano inexistente
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

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollAsync(dto));
        }

        [Fact]
        public async Task EnrollAsync_WithUnavailablePersonal_ShouldThrowException()
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

            var student = new Student(
                "Aluno Teste",
                "aluno@teste.com",
                "11999999999",
                "12345678900",
                DateTime.Now.AddYears(-20),
                Gender.Male,
                "Endereço Teste"
            );
            await _studentRepository.AddAsync(student);

            var assessmentDate = DateTime.Now.AddDays(1);
            var existingAssessment = new PhysicalAssessment(
                student.Id,
                personal.Id,
                assessmentDate,
                70,
                1.75m,
                15,
                "Notas teste"
            );
            await _assessmentRepository.AddAsync(existingAssessment);

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
                    AssessmentDate = assessmentDate,
                    Weight = 70,
                    Height = 1.75m,
                    BodyFatPercentage = 15,
                    Notes = "Notas teste"
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnrollAsync(dto));
        }
    }
} 