using FluentAssertions;
using GymErp.Domain.Subscriptions;
using Xunit;

namespace GymErp.UnitTests.Subscriptions;

public class EnrollmentTests
{
    [Fact]
    public void Create_WithValidClient_ShouldCreateEnrollment()
    {
        // Arrange
        var client = new Client(
            "12345678900",
            "João Silva",
            "joao@email.com",
            "11999999999",
            "Rua Exemplo, 123"
        );

        // Act
        var result = Enrollment.Create(client);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var enrollment = result.Value;
        enrollment.Should().NotBeNull();
        enrollment.Id.Should().NotBe(Guid.Empty);
        enrollment.Client.Should().Be(client);
        enrollment.RequestDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        enrollment.State.Should().Be(EState.Suspended);
    }

    [Fact]
    public void Create_WithValidParameters_ShouldCreateEnrollment()
    {
        // Arrange
        var name = "João Silva";
        var email = "joao@email.com";
        var phone = "11999999999";
        var document = "12345678900";
        var birthDate = new DateTime(1990, 1, 1);
        var gender = "M";
        var address = "Rua Exemplo, 123";

        // Act
        var enrollment = Enrollment.Create(name, email, phone, document, birthDate, gender, address);

        // Assert
        enrollment.Should().NotBeNull();
        enrollment.Id.Should().NotBe(Guid.Empty);
        enrollment.Client.Name.Should().Be(name);
        enrollment.Client.Email.Should().Be(email);
        enrollment.Client.Phone.Should().Be(phone);
        enrollment.Client.Cpf.Should().Be(document);
        enrollment.Client.Address.Should().Be(address);
        enrollment.RequestDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        enrollment.State.Should().Be(EState.Suspended);
    }

    [Theory]
    [InlineData("", "email@test.com", "11999999999", "12345678900", "1990-01-01", "M", "Rua Teste")]
    [InlineData("João Silva", "", "11999999999", "12345678900", "1990-01-01", "M", "Rua Teste")]
    [InlineData("João Silva", "email@test.com", "", "12345678900", "1990-01-01", "M", "Rua Teste")]
    [InlineData("João Silva", "email@test.com", "11999999999", "", "1990-01-01", "M", "Rua Teste")]
    [InlineData("João Silva", "email@test.com", "11999999999", "12345678900", "1990-01-01", "M", "")]
    public void Create_WithInvalidParameters_ShouldCreateEnrollmentWithEmptyFields(
        string name,
        string email,
        string phone,
        string document,
        string birthDate,
        string gender,
        string address)
    {
        // Act
        var enrollment = Enrollment.Create(
            name,
            email,
            phone,
            document,
            DateTime.Parse(birthDate),
            gender,
            address
        );

        // Assert
        enrollment.Should().NotBeNull();
        enrollment.Client.Name.Should().Be(name);
        enrollment.Client.Email.Should().Be(email);
        enrollment.Client.Phone.Should().Be(phone);
        enrollment.Client.Cpf.Should().Be(document);
        enrollment.Client.Address.Should().Be(address);
    }
} 