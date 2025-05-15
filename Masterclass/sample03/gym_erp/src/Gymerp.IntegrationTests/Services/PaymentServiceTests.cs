using System;
using System.Threading.Tasks;
using Gymerp.Application.Interfaces;
using Gymerp.Application.Models;
using Gymerp.Application.Services;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Gymerp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.MsSql;
using Xunit;

namespace Gymerp.IntegrationTests.Services
{
    public class PaymentServiceTests : IAsyncLifetime
    {
        private readonly MsSqlContainer _container;
        private ApplicationDbContext _context;
        private IPaymentRepository _paymentRepository;
        private IEnrollmentRepository _enrollmentRepository;
        private IPaymentGatewayService _paymentGatewayService;
        private INotificationService _notificationService;
        private PaymentService _service;

        public PaymentServiceTests()
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
            _paymentRepository = new PaymentRepository(_context);
            _enrollmentRepository = new EnrollmentRepository(_context);

            // Mock do PaymentGatewayService
            var paymentGatewayServiceMock = new Mock<IPaymentGatewayService>();
            paymentGatewayServiceMock
                .Setup(x => x.ProcessCreditCardPaymentAsync(
                    It.IsAny<Payment>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(true);
            _paymentGatewayService = paymentGatewayServiceMock.Object;

            // Mock do NotificationService
            var notificationServiceMock = new Mock<INotificationService>();
            _notificationService = notificationServiceMock.Object;

            _service = new PaymentService(
                _paymentRepository,
                _enrollmentRepository,
                _paymentGatewayService,
                _notificationService
            );

            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
                await _context.DisposeAsync();
            await _container.DisposeAsync();
        }

        [Fact]
        public async Task ProcessAsync_WithValidEnrollment_ShouldCreatePaymentAndConfirmEnrollment()
        {
            // Arrange
            var plan = new Plan("Plano Teste", "Descrição Teste", 100m, 1);
            var student = new Student(
                "Aluno Teste",
                "aluno@teste.com",
                "11999999999",
                "12345678900",
                DateTime.Now.AddYears(-20),
                Gender.Male,
                "Endereço Teste"
            );
            await _context.Plans.AddAsync(plan);
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();
            var enrollment = new Enrollment(
                student.Id,
                plan.Id,
                DateTime.Now,
                DateTime.Now.AddMonths(1)
            );

            await _enrollmentRepository.AddAsync(enrollment);

            // Act
            var result = await _service.ProcessAsync(enrollment);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Pagamento aprovado", result.Message);

            var updatedEnrollment = await _enrollmentRepository.GetByIdAsync(enrollment.Id);
            Assert.Equal(EnrollmentStatus.Active, updatedEnrollment.Status);

            var payments = await _paymentRepository.GetByEnrollmentIdAsync(enrollment.Id);
            Assert.Single(payments);
            var payment = payments.First();
            Assert.Equal(PaymentStatus.Paid, payment.Status);
        }

        [Fact]
        public async Task ProcessAsync_WithMaxPaymentAttempts_ShouldCancelEnrollment()
        {
            // Arrange
            var plan = new Plan("Plano Teste", "Descrição Teste", 100m, 1);
            var student = new Student(
                "Aluno Teste",
                "aluno@teste.com",
                "11999999999",
                "12345678900",
                DateTime.Now.AddYears(-20),
                Gender.Male,
                "Endereço Teste"
            );
            await _context.Plans.AddAsync(plan);
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();
            var enrollment = new Enrollment(
                student.Id,
                plan.Id,
                DateTime.Now,
                DateTime.Now.AddMonths(1)
            );

            await _enrollmentRepository.AddAsync(enrollment);

            // Criar 3 pagamentos rejeitados
            for (int i = 0; i < 3; i++)
            {
                var payment = new Payment(
                    enrollment.Id,
                    plan.Price,
                    DateTime.Now.AddDays(7)
                );
                payment.MarkAsRejected();
                await _paymentRepository.AddAsync(payment);
            }

            // Act
            var result = await _service.ProcessAsync(enrollment);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Número máximo de tentativas excedido", result.Message);

            var updatedEnrollment = await _enrollmentRepository.GetByIdAsync(enrollment.Id);
            Assert.Equal(EnrollmentStatus.Cancelled, updatedEnrollment.Status);
        }

        [Fact]
        public async Task ProcessPaymentAsync_WithValidCard_ShouldProcessPayment()
        {
            // Arrange
            var plan = new Plan("Plano Teste", "Descrição Teste", 100m, 1);
            var student = new Student(
                "Aluno Teste",
                "aluno@teste.com",
                "11999999999",
                "12345678900",
                DateTime.Now.AddYears(-20),
                Gender.Male,
                "Endereço Teste"
            );
            await _context.Plans.AddAsync(plan);
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();
            var enrollment = new Enrollment(
                student.Id,
                plan.Id,
                DateTime.Now,
                DateTime.Now.AddMonths(1)
            );

            await _enrollmentRepository.AddAsync(enrollment);

            var payment = new Payment(
                enrollment.Id,
                plan.Price,
                DateTime.Now.AddDays(7)
            );
            await _paymentRepository.AddAsync(payment);

            // Act
            var result = await _service.ProcessPaymentAsync(
                payment,
                "4111111111111111",
                "12/25",
                "123"
            );

            // Assert
            Assert.True(result);

            var updatedPayment = await _paymentRepository.GetByIdAsync(payment.Id);
            Assert.Equal(PaymentStatus.Paid, updatedPayment.Status);
        }

        [Fact]
        public async Task ProcessPaymentAsync_WithRejectedPayment_ShouldMarkPaymentAsRejected()
        {
            // Arrange
            var plan = new Plan("Plano Teste", "Descrição Teste", 100m, 1);
            var student = new Student(
                "Aluno Teste",
                "aluno@teste.com",
                "11999999999",
                "12345678900",
                DateTime.Now.AddYears(-20),
                Gender.Male,
                "Endereço Teste"
            );
            var enrollment = new Enrollment(
                student.Id,
                plan.Id,
                DateTime.Now,
                DateTime.Now.AddMonths(1)
            );

            await _enrollmentRepository.AddAsync(enrollment);

            var payment = new Payment(
                enrollment.Id,
                plan.Price,
                DateTime.Now.AddDays(7)
            );
            await _paymentRepository.AddAsync(payment);

            // Configurar o mock para rejeitar o pagamento
            var paymentGatewayServiceMock = new Mock<IPaymentGatewayService>();
            paymentGatewayServiceMock
                .Setup(x => x.ProcessCreditCardPaymentAsync(
                    It.IsAny<Payment>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(false);

            var service = new PaymentService(
                _paymentRepository,
                _enrollmentRepository,
                paymentGatewayServiceMock.Object,
                _notificationService
            );

            // Act
            var result = await service.ProcessPaymentAsync(
                payment,
                "4111111111111111",
                "12/25",
                "123"
            );

            // Assert
            Assert.False(result);

            var updatedPayment = await _paymentRepository.GetByIdAsync(payment.Id);
            Assert.Equal(PaymentStatus.Rejected, updatedPayment.Status);
        }

        [Fact]
        public async Task ProcessPaymentAsync_WithGatewayError_ShouldMarkPaymentAsRejected()
        {
            // Arrange
            var plan = new Plan("Plano Teste", "Descrição Teste", 100m, 1);
            var student = new Student(
                "Aluno Teste",
                "aluno@teste.com",
                "11999999999",
                "12345678900",
                DateTime.Now.AddYears(-20),
                Gender.Male,
                "Endereço Teste"
            );
            var enrollment = new Enrollment(
                student.Id,
                plan.Id,
                DateTime.Now,
                DateTime.Now.AddMonths(1)
            );

            await _enrollmentRepository.AddAsync(enrollment);

            var payment = new Payment(
                enrollment.Id,
                plan.Price,
                DateTime.Now.AddDays(7)
            );
            await _paymentRepository.AddAsync(payment);

            // Configurar o mock para lançar uma exceção
            var paymentGatewayServiceMock = new Mock<IPaymentGatewayService>();
            paymentGatewayServiceMock
                .Setup(x => x.ProcessCreditCardPaymentAsync(
                    It.IsAny<Payment>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Erro no gateway de pagamento"));

            var service = new PaymentService(
                _paymentRepository,
                _enrollmentRepository,
                paymentGatewayServiceMock.Object,
                _notificationService
            );

            // Act
            var result = await service.ProcessPaymentAsync(
                payment,
                "4111111111111111",
                "12/25",
                "123"
            );

            // Assert
            Assert.False(result);

            var updatedPayment = await _paymentRepository.GetByIdAsync(payment.Id);
            Assert.Equal(PaymentStatus.Rejected, updatedPayment.Status);
        }
    }
} 