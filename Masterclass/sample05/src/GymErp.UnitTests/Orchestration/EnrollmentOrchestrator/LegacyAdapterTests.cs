using CSharpFunctionalExtensions;
using FluentAssertions;
using Flurl.Http;
using Flurl.Http.Testing;
using GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GymErp.UnitTests.Orchestration.EnrollmentOrchestrator;

public class LegacyAdapterTests
{
    private readonly Mock<IOptions<LegacyApiConfiguration>> _mockConfiguration;
    private readonly LegacyAdapter _adapter;

    public LegacyAdapterTests()
    {
        _mockConfiguration = new Mock<IOptions<LegacyApiConfiguration>>();
        
        var configuration = new LegacyApiConfiguration
        {
            BaseUrl = "http://localhost:5000",
            TimeoutSeconds = 30,
            RetryAttempts = 3
        };

        _mockConfiguration.Setup(x => x.Value).Returns(configuration);
        _adapter = new LegacyAdapter(_mockConfiguration.Object);
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldReturnSuccess_WhenLegacyApiReturnsSuccess()
    {
        // Arrange
        var request = CreateValidRequest();
        var expectedEnrollmentId = Guid.NewGuid();

        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new LegacyEnrollmentResponse { EnrollmentId = expectedEnrollmentId });

        // Act
        var result = await _adapter.ProcessEnrollmentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedEnrollmentId);

        // Verificar que a chamada foi feita com os dados corretos
        httpTest.ShouldHaveCalled("http://localhost:5000/api/enrollment/enroll")
            .WithVerb(HttpMethod.Post)
            .WithContentType("application/json");
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldMapRequestCorrectly_WhenCalled()
    {
        // Arrange
        var request = CreateValidRequest();
        var expectedEnrollmentId = Guid.NewGuid();

        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new LegacyEnrollmentResponse { EnrollmentId = expectedEnrollmentId });

        // Act
        await _adapter.ProcessEnrollmentAsync(request);

        // Assert
        var capturedRequest = httpTest.CallLog.First().RequestBody;
        capturedRequest.Should().NotBeNull();
        
        // Verificar se os dados foram mapeados corretamente
        var capturedJson = capturedRequest.ToString();
        capturedJson.Should().Contain("Jo\\u00E3o da Silva Santos"); // Nome esperado (unicode escapado)
        capturedJson.Should().Contain(request.Student.Email);
        capturedJson.Should().Contain(request.PlanId.ToString());
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldReturnFailure_WhenLegacyApiReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();

        using var httpTest = new HttpTest();
        httpTest.RespondWith("Bad Request", 400);

        // Act
        var result = await _adapter.ProcessEnrollmentAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Erro ao processar inscrição no sistema legado");
        result.Error.Should().Contain("Bad Request");
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldReturnFailure_WhenLegacyApiReturnsInternalServerError()
    {
        // Arrange
        var request = CreateValidRequest();

        using var httpTest = new HttpTest();
        httpTest.RespondWith("Internal Server Error", 500);

        // Act
        var result = await _adapter.ProcessEnrollmentAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Erro ao processar inscrição no sistema legado");
        result.Error.Should().Contain("Internal Server Error");
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldReturnFailure_WhenLegacyApiTimesOut()
    {
        // Arrange
        var request = CreateValidRequest();

        using var httpTest = new HttpTest();
        httpTest.SimulateTimeout();

        // Act
        var result = await _adapter.ProcessEnrollmentAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Erro ao processar inscrição no sistema legado");
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldReturnFailure_WhenLegacyApiIsUnavailable()
    {
        // Arrange
        var request = CreateValidRequest();

        using var httpTest = new HttpTest();
        httpTest.RespondWith("Service Unavailable", 503);

        // Act
        var result = await _adapter.ProcessEnrollmentAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Erro ao processar inscrição no sistema legado");
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldReturnFailure_WhenNetworkExceptionOccurs()
    {
        // Arrange
        var request = CreateValidRequest();

        using var httpTest = new HttpTest();
        httpTest.SimulateException(new System.Net.Http.HttpRequestException("Network error"));

        // Act
        var result = await _adapter.ProcessEnrollmentAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        // O Flurl captura HttpRequestException como FlurlHttpException, então verificamos a mensagem correta
        result.Error.Should().Contain("Erro ao processar inscrição no sistema legado");
    }

    [Fact]
    public async Task ProcessEnrollmentAsync_ShouldUseCorrectTimeout_WhenConfigured()
    {
        // Arrange
        var request = CreateValidRequest();
        var configuration = new LegacyApiConfiguration
        {
            BaseUrl = "http://localhost:5000",
            TimeoutSeconds = 60, // Timeout diferente
            RetryAttempts = 3
        };

        _mockConfiguration.Setup(x => x.Value).Returns(configuration);
        var adapter = new LegacyAdapter(_mockConfiguration.Object);

        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new LegacyEnrollmentResponse { EnrollmentId = Guid.NewGuid() });

        // Act
        await adapter.ProcessEnrollmentAsync(request);

        // Assert
        // Verificar se a chamada foi feita (o timeout é aplicado internamente pelo Flurl)
        httpTest.ShouldHaveCalled("http://localhost:5000/api/enrollment/enroll");
    }

    private static Request CreateValidRequest()
    {
        return new Request
        {
            ClientId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(12),
            Student = new StudentDto
            {
                Name = "João da Silva Santos",
                Email = "joao.silva@email.com",
                Phone = "11999999999",
                Document = "52998224725",
                BirthDate = new DateTime(1990, 1, 1),
                Gender = "M",
                Address = "Rua das Flores, 123"
            },
            PhysicalAssessment = new PhysicalAssessmentDto
            {
                PersonalId = Guid.NewGuid(),
                AssessmentDate = DateTime.UtcNow,
                Weight = 75.5m,
                Height = 1.75m,
                BodyFatPercentage = 15.0m,
                Notes = "Avaliação física inicial"
            }
        };
    }
}
