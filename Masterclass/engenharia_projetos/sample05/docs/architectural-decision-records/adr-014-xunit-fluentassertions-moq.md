# ADR-014: xUnit + FluentAssertions + Moq como Stack de Testes

> **Escopo:** exclusivamente `modernized/` (GymErp.UnitTests e GymErp.IntegrationTests) — **não se aplica** a `legacy/Gymerp.IntegrationTests/`.

## Contexto

O projeto modernizado tem dois projetos de teste: `GymErp.UnitTests` e `GymErp.IntegrationTests`. A stack de testes precisa ser uniforme entre os dois. Aplica-se a todos os testes em `modernized/`.

Evidência: Ambos os `.csproj` referenciam `xunit 2.9.2`, `FluentAssertions 6.12.2`, `Moq 4.20.72`. `GymErp.IntegrationTests.csproj` inclui `<Using Include="Xunit" />` para global using.

**Alternativas consideradas:**
1. NUnit — rejeitado; xUnit é mais alinhado com .NET moderno e comunidade ASP.NET Core.
2. Shouldly em vez de FluentAssertions — rejeitado; FluentAssertions tem API mais rica.
3. NSubstitute em vez de Moq — rejeitado; Moq mais amplamente conhecido no time.

## Decisão

Adotamos xUnit como test runner, FluentAssertions para assertions expressivas e Moq para mocking de dependências em testes unitários. `coverlet.collector` é incluído para cobertura de código.

## Regras Operacionais

1. **SEMPRE** use FluentAssertions (`.Should().Be()`, `.Should().HaveCount()`) em vez de `Assert.Equal()`. *Padrão detectável:* `.Should().` em arquivos `*Tests.cs`. *Motivo:* mensagens de falha são mais descritivas e a API é mais fluente.
2. **SEMPRE** use Moq para mocking em testes unitários via `new Mock<T>()`. *Padrão detectável:* `new Mock<` em `GymErp.UnitTests`. *Motivo:* testes unitários não devem subir infraestrutura real.
3. **NUNCA** use Moq em testes de integração para dependências que têm container real disponível. *Padrão detectável:* `new Mock<IUnitOfWork>` em `GymErp.IntegrationTests`. *Motivo:* testes de integração devem testar a cadeia real (ver ADR-015).
4. **SEMPRE** nomeie classes de teste como `{Feature}Tests` ou `{Feature}HandlerTests`. *Padrão detectável:* arquivos `*Tests.cs` em pastas `{Feature}/`. *Motivo:* nome explicita o que está sendo testado.

## Exemplos

### ✅ DO

```csharp
// modernized/GymErp.UnitTests/Subscriptions/EnrollmentTests.cs
public class EnrollmentTests
{
    [Fact]
    public void Create_WithValidClient_ShouldSucceed()
    {
        var result = Enrollment.Create("João Silva Ferreira", "joao@email.com",
            "11999999999", "123.456.789-09", DateTime.Now.AddYears(-25), "M", "Rua A, 123");

        result.IsSuccess.Should().BeTrue();
        result.Value.State.Should().Be(EState.Suspended);
    }

    [Fact]
    public void Create_WithInvalidCpf_ShouldFail()
    {
        var result = Enrollment.Create("João Silva Ferreira", "joao@email.com",
            "11999999999", "000.000.000-00", DateTime.Now.AddYears(-25), "M", "Rua A, 123");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("CPF inválido");
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: Assert.Equal sem mensagem, sem FluentAssertions
[TestMethod]
public void TestCreate()
{
    var result = Enrollment.Create(...);
    Assert.IsTrue(result.IsSuccess);
    Assert.AreEqual(EState.Suspended, result.Value.State);
}
```

## Benefícios

- **Mensagens claras** — FluentAssertions descreve o que era esperado vs. o que foi recebido.
- **Mocking preciso** — Moq verifica chamadas, argumentos e sequências.
- **Cobertura integrada** — coverlet gera relatório sem configuração extra.

## Relacionado

- [ADR-015](./adr-015-testcontainers.md): testes de integração estendem a stack com TestContainers.
- [ADR-016](./adr-016-estrutura-testes.md): organização dos arquivos de teste.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
