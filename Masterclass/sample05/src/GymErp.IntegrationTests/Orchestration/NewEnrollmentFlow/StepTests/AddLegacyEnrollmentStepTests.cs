using Flurl.Http;
using GymErp.Common.Settings;
using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;
using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps;
using GymErp.IntegrationTests.Infrastructure;
using GymErp.IntegrationTests.Orchestration.NewEnrollmentFlow.TestHelpers;
using Microsoft.Extensions.Options;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;

namespace GymErp.IntegrationTests.Orchestration.NewEnrollmentFlow.StepTests;

/// <summary>
/// Testes específicos para o AddLegacyEnrollmentStep
/// </summary>
public class AddLegacyEnrollmentStepTests : IntegrationTestBase, IAsyncLifetime
{
    private AddLegacyEnrollmentStep _step = null!;
    private ServicesSettings _servicesSettings = null!;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        _servicesSettings = new ServicesSettings
        {
            LegacyApiUri = "http://localhost:5000" // URL mockada para testes
        };
        
        _step = new AddLegacyEnrollmentStep(Options.Create(_servicesSettings));
    }

    [Fact]
    public void Step_ShouldBeConfiguredCorrectly()
    {
        // Arrange & Act
        var step = new AddLegacyEnrollmentStep(Options.Create(_servicesSettings));

        // Assert
        step.Should().NotBeNull();
    }

    [Fact]
    public void AddLegacyEnrollmentRequest_ShouldHaveCorrectStructure()
    {
        // Arrange
        var data = TestDataBuilder.CreateValidData().Build();

        // Act
        var request = new AddLegacyEnrollmentStep.AddLegacyEnrollmentRequest(
            new AddLegacyEnrollmentStep.StudentDto(
                data.Name,
                data.Email,
                data.Phone,
                data.Document,
                data.BirthDate,
                data.Gender,
                data.Address
            ),
            data.PlanId,
            data.StartDate,
            data.EndDate
        );

        // Assert
        request.Student.Name.Should().Be(data.Name);
        request.Student.Email.Should().Be(data.Email);
        request.Student.Phone.Should().Be(data.Phone);
        request.Student.Document.Should().Be(data.Document);
        request.Student.BirthDate.Should().Be(data.BirthDate);
        request.Student.Gender.Should().Be(data.Gender);
        request.Student.Address.Should().Be(data.Address);
        request.PlanId.Should().Be(data.PlanId);
        request.StartDate.Should().Be(data.StartDate);
        request.EndDate.Should().Be(data.EndDate);
    }

    [Fact]
    public void StudentDto_ShouldMapCorrectly()
    {
        // Arrange
        var data = TestDataBuilder.CreateValidData().Build();

        // Act
        var studentDto = new AddLegacyEnrollmentStep.StudentDto(
            data.Name,
            data.Email,
            data.Phone,
            data.Document,
            data.BirthDate,
            data.Gender,
            data.Address
        );

        // Assert
        studentDto.Name.Should().Be(data.Name);
        studentDto.Email.Should().Be(data.Email);
        studentDto.Phone.Should().Be(data.Phone);
        studentDto.Document.Should().Be(data.Document);
        studentDto.BirthDate.Should().Be(data.BirthDate);
        studentDto.Gender.Should().Be(data.Gender);
        studentDto.Address.Should().Be(data.Address);
    }

    [Theory]
    [InlineData("", "email@test.com", "11999999999", "12345678901")]
    [InlineData("João Silva", "", "11999999999", "12345678901")]
    [InlineData("João Silva", "email@test.com", "", "12345678901")]
    [InlineData("João Silva", "email@test.com", "11999999999", "")]
    public void StudentDto_ShouldAcceptEmptyFields_ForTestingPurposes(
        string name, string email, string phone, string document)
    {
        // Arrange & Act
        var studentDto = new AddLegacyEnrollmentStep.StudentDto(
            name,
            email,
            phone,
            document,
            DateTime.Now.AddYears(-30),
            "M",
            "Rua Teste, 123"
        );

        // Assert
        studentDto.Name.Should().Be(name);
        studentDto.Email.Should().Be(email);
        studentDto.Phone.Should().Be(phone);
        studentDto.Document.Should().Be(document);
    }

}
