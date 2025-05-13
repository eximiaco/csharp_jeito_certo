using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Gymerp.Application.Services;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Gymerp.Infrastructure.Repositories;

namespace Gymerp.IntegrationTests.Services
{
    public class FullEnrollmentServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly StudentRepository _studentRepository;
        private readonly PlanRepository _planRepository;
        private readonly EnrollmentRepository _enrollmentRepository;
        private readonly PhysicalAssessmentRepository _assessmentRepository;
        private readonly PersonalRepository _personalRepository;
        private readonly IPaymentService _paymentService;
        private readonly FullEnrollmentService _service;

        public FullEnrollmentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=GymerpTest;Trusted_Connection=True;MultipleActiveResultSets=true")
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
        }

        public async Task InitializeAsync()
        {
            await _context.Database.EnsureCreatedAsync();
        }

        [Fact]
        public async Task ProcessAsync_ShouldReturnSuccess()
        {
            // Arrange
            var enrollment = new Enrollment
            {
                // Popule os campos necessários para o teste
            };

            // Act
            var result = await _service.ProcessAsync(enrollment);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Pagamento aprovado", result.Message);
        }

        [Fact]
        public async Task ProcessAsync_ShouldReturnFailure()
        {
            // Arrange
            var enrollment = new Enrollment
            {
                // Popule os campos necessários para o teste
            };

            // Act
            var result = await _service.ProcessAsync(enrollment);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Pagamento rejeitado", result.Message);
        }
    }
} 