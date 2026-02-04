using FluentAssertions;
using GymErp.Domain.Subscriptions;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using Xunit;

namespace GymErp.UnitTests.Subscriptions;

public class EnrollmentTests
{
    [Fact]
    public void Create_WithValidClient_ShouldCreateEnrollment()
    {
        // Arrange
        var client = new Client(
            "52998224725", // CPF válido
            "João da Silva Santos",
            "joao.silva@email.com",
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
        var name = "João da Silva Santos";
        var email = "joao.silva@email.com";
        var phone = "11999999999";
        var document = "52998224725"; // CPF válido
        var birthDate = new DateTime(1990, 1, 1);
        var gender = "M";
        var address = "Rua Exemplo, 123";

        // Act
        var result = Enrollment.Create(name, email, phone, document, birthDate, gender, address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var enrollment = result.Value;
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
    [InlineData("", "email@test.com", "11999999999", "52998224725", "1990-01-01", "M", "Rua Teste", "Nome não pode ser vazio")]
    [InlineData("João", "email@test.com", "11999999999", "52998224725", "1990-01-01", "M", "Rua Teste", "Nome deve ter pelo menos 10 caracteres")]
    [InlineData("João da Silva Santos", "", "11999999999", "52998224725", "1990-01-01", "M", "Rua Teste", "Email não pode ser vazio")]
    [InlineData("João da Silva Santos", "invalid-email", "11999999999", "52998224725", "1990-01-01", "M", "Rua Teste", "Email inválido")]
    [InlineData("João da Silva Santos", "email@test.com", "", "52998224725", "1990-01-01", "M", "Rua Teste", "Telefone não pode ser vazio")]
    [InlineData("João da Silva Santos", "email@test.com", "123", "52998224725", "1990-01-01", "M", "Rua Teste", "Telefone inválido")]
    [InlineData("João da Silva Santos", "email@test.com", "11999999999", "", "1990-01-01", "M", "Rua Teste", "CPF não pode ser vazio")]
    [InlineData("João da Silva Santos", "email@test.com", "11999999999", "12345678901", "1990-01-01", "M", "Rua Teste", "CPF inválido")]
    public void Create_WithInvalidParameters_ShouldReturnFailure(
        string name,
        string email,
        string phone,
        string document,
        string birthDate,
        string gender,
        string address,
        string expectedError)
    {
        // Act
        var result = Enrollment.Create(
            name,
            email,
            phone,
            document,
            DateTime.Parse(birthDate),
            gender,
            address
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(expectedError);
    }

    [Theory]
    [InlineData("52998224725")] // CPF válido
    [InlineData("529.982.247-25")] // CPF válido com formatação
    public void IsValidCpf_WithValidCpf_ShouldReturnTrue(string cpf)
    {
        // Arrange
        var client = new Client(
            cpf,
            "João da Silva Santos",
            "joao.silva@email.com",
            "11999999999",
            "Rua Exemplo, 123"
        );

        // Act
        var result = Enrollment.Create(client);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345678901")] // CPF inválido
    [InlineData("11111111111")] // CPF com dígitos iguais
    [InlineData("123")] // CPF muito curto
    public void IsValidCpf_WithInvalidCpf_ShouldReturnFalse(string cpf)
    {
        // Arrange
        var client = new Client(
            cpf,
            "João da Silva Santos",
            "joao.silva@email.com",
            "11999999999",
            "Rua Exemplo, 123"
        );

        // Act
        var result = Enrollment.Create(client);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("CPF inválido");
    }

    [Theory]
    [InlineData("test@email.com")]
    [InlineData("test.name@email.com")]
    [InlineData("test+name@email.com")]
    public void IsValidEmail_WithValidEmail_ShouldReturnTrue(string email)
    {
        // Arrange
        var client = new Client(
            "52998224725",
            "João da Silva Santos",
            email,
            "11999999999",
            "Rua Exemplo, 123"
        );

        // Act
        var result = Enrollment.Create(client);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@email.com")]
    public void IsValidEmail_WithInvalidEmail_ShouldReturnFalse(string email)
    {
        // Arrange
        var client = new Client(
            "52998224725",
            "João da Silva Santos",
            email,
            "11999999999",
            "Rua Exemplo, 123"
        );

        // Act
        var result = Enrollment.Create(client);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email inválido");
    }

    [Theory]
    [InlineData("11999999999")]
    [InlineData("(11) 99999-9999")]
    [InlineData("11 99999 9999")]
    public void IsValidPhone_WithValidPhone_ShouldReturnTrue(string phone)
    {
        // Arrange
        var client = new Client(
            "52998224725",
            "João da Silva Santos",
            "joao.silva@email.com",
            phone,
            "Rua Exemplo, 123"
        );

        // Act
        var result = Enrollment.Create(client);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("123")] // Muito curto
    [InlineData("123456789012")] // Muito longo
    public void IsValidPhone_WithInvalidPhone_ShouldReturnFalse(string phone)
    {
        // Arrange
        var client = new Client(
            "52998224725",
            "João da Silva Santos",
            "joao.silva@email.com",
            phone,
            "Rua Exemplo, 123"
        );

        // Act
        var result = Enrollment.Create(client);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Telefone inválido");
    }
} 