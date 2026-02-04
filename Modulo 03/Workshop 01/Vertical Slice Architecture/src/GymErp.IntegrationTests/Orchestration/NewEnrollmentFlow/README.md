# Testes do NewEnrollmentFlow

Esta pasta contém uma suíte completa de testes para o workflow `NewEnrollmentFlow` do módulo de orquestração.

## 📁 Estrutura

```
NewEnrollmentFlow/
├── WorkflowTestBase.cs              # Classe base para testes de workflow
├── WorkflowTests.cs                 # Testes do workflow principal
├── IntegrationTests.cs              # Testes de integração completos
├── StepTests/                       # Testes dos steps individuais
│   ├── AddEnrollmentStepTests.cs
│   ├── ProcessPaymentStepTests.cs
│   ├── ScheduleEvaluationStepTests.cs
│   ├── AddEnrollmentCompensationStepTests.cs
│   ├── ProcessPaymentCompensationStepTests.cs
│   └── ScheduleEvaluationCompensationStepTests.cs
└── TestHelpers/                     # Helpers e builders
    ├── TestDataBuilder.cs
    ├── MockServicesHelper.cs
    └── WorkflowTestExtensions.cs
```

## 🧪 Tipos de Testes

### 1. Testes Unitários dos Steps
- **AddEnrollmentStep**: Testa criação de matrícula via API
- **ProcessPaymentStep**: Testa processamento de pagamento
- **ScheduleEvaluationStep**: Testa agendamento de avaliação
- **Compensation Steps**: Testa compensações em caso de falha

### 2. Testes do Workflow Principal
- Configuração e definição do workflow
- Execução completa com sucesso
- Tratamento de falhas e compensações
- Retry policies e timeouts

### 3. Testes de Integração
- Cenários end-to-end completos
- Falhas em diferentes etapas
- Compensações parciais e completas
- Concorrência e consistência de dados

## 🛠️ Como Usar

### Exemplo Básico - Teste de Sucesso

```csharp
[Fact]
public async Task Should_CompleteFullFlow_WhenAllServicesAvailable()
{
    // Arrange
    SetupAllServicesToReturnSuccess();
    var data = CreateValidWorkflowData();

    // Act
    var workflowId = await StartNewEnrollmentWorkflow(data);
    await WaitForWorkflowCompletion(workflowId);

    // Assert
    VerifyWorkflowCompletedSuccessfully(workflowId);
    VerifyAllStepsExecutedSuccessfully(data);
}
```

### Exemplo - Teste de Falha e Compensação

```csharp
[Fact]
public async Task Should_CompensateAllSteps_WhenPaymentFails()
{
    // Arrange
    SetupPaymentServiceToFail();
    SetupSubscriptionsServiceToReturnSuccess();
    
    var data = CreateValidWorkflowData();

    // Act
    var workflowId = await StartNewEnrollmentWorkflow(data);
    await WaitForWorkflowCompletion(workflowId);

    // Assert
    VerifyWorkflowFailed(workflowId);
    VerifyEnrollmentWasCompensated(data);
}
```

### Exemplo - Usando TestDataBuilder

```csharp
[Fact]
public async Task Should_HandleInvalidClient()
{
    // Arrange
    var data = TestDataBuilder.CreateWithInvalidClient().Build();
    SetupAllServicesToReturnSuccess();

    // Act & Assert
    var workflowId = await StartNewEnrollmentWorkflow(data);
    await WaitForWorkflowCompletion(workflowId);
    
    VerifyWorkflowFailed(workflowId);
}
```

### Exemplo - Usando MockServicesHelper

```csharp
[Fact]
public async Task Should_RetryOnServiceFailure()
{
    // Arrange
    var mockHelper = new MockServicesHelper(HttpTest, ServicesSettings);
    mockHelper.SetupServiceToFailThenSucceed(
        $"{ServicesSettings.SubscriptionsUri}/enrollments", 
        failureCount: 2);
    
    var data = CreateValidWorkflowData();

    // Act
    var workflowId = await StartNewEnrollmentWorkflow(data);
    await WaitForWorkflowCompletion(workflowId);

    // Assert
    VerifyWorkflowCompletedSuccessfully(workflowId);
    mockHelper.VerifyServiceWasCalled(
        $"{ServicesSettings.SubscriptionsUri}/enrollments", 
        expectedTimes: 3);
}
```

## 🔧 Configuração

### Dependências Necessárias
- `Flurl.Http.Testing` - Para mock de chamadas HTTP
- `WorkflowCore` - Para execução de workflows
- `FluentAssertions` - Para assertivas legíveis
- `xUnit` - Framework de testes

### Serviços Mockados
- **Subscriptions Service**: `http://localhost:5001`
- **Payment Service**: `http://localhost:5002`
- **Scheduling Service**: `http://localhost:5003`

## 📊 Cobertura de Testes

### Cenários Cobertos

#### ✅ Cenários de Sucesso
- Fluxo completo bem-sucedido
- Execução de todos os steps
- Publicação de eventos (quando implementado)
- Concorrência de workflows

#### ❌ Cenários de Falha
- Falha no serviço de Subscriptions
- Falha no serviço de Payment
- Falha no serviço de Scheduling
- Timeouts e indisponibilidade
- Falhas de rede e retry

#### 🔄 Cenários de Compensação
- Compensação completa
- Compensação parcial
- Falhas durante compensação
- Múltiplas compensações

#### 🚀 Cenários de Resiliência
- Retry policies
- Timeouts configuráveis
- Degradação graciosa
- Consistência de dados

## 🎯 Executando os Testes

### Comando Básico
```bash
dotnet test src/GymErp.IntegrationTests
```

### Testes Específicos
```bash
# Todos os testes do NewEnrollmentFlow
dotnet test --filter "NewEnrollmentFlow"

# Testes de steps específicos
dotnet test --filter "AddEnrollmentStepTests"

# Testes de integração
dotnet test --filter "IntegrationTests"
```

### Com Logs Detalhados
```bash
dotnet test --logger "console;verbosity=detailed"
```

## 🔍 Debugging

### Logs de Workflow
Os testes configuram logging básico para o WorkflowCore. Para mais detalhes:

```csharp
// No WorkflowTestBase.cs
services.AddLogging(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
```

### Verificação de Chamadas HTTP
```csharp
// Verificar todas as chamadas feitas
var calls = HttpTest.CallLog;
foreach (var call in calls)
{
    Console.WriteLine($"{call.Request.Method} {call.Request.RequestUri}");
}
```

## 📝 Adicionando Novos Testes

### 1. Criar Teste de Step
```csharp
public class NewStepTests : WorkflowTestBase
{
    [Fact]
    public async Task Should_ExecuteNewStep_Successfully()
    {
        // Arrange
        SetupServiceToReturnSuccess();
        var data = CreateValidWorkflowData();
        var context = CreateMockExecutionContext(data);
        var step = new NewStep(Options.Create(ServicesSettings));

        // Act
        var result = await step.RunAsync(context);

        // Assert
        result.Proceed.Should().BeTrue();
        VerifyServiceWasCalled(serviceUri);
    }
}
```

### 2. Criar Teste de Integração
```csharp
[Fact]
public async Task Should_HandleNewScenario()
{
    // Arrange
    var data = TestDataBuilder.CreateForFailureScenario(FailingStep.NewStep).Build();
    SetupNewScenario();

    // Act
    var workflowId = await StartNewEnrollmentWorkflow(data);
    await WaitForWorkflowCompletion(workflowId);

    // Assert
    VerifyWorkflowCompletedSuccessfully(workflowId);
    VerifyNewScenarioBehavior();
}
```

## 🚨 Troubleshooting

### Problemas Comuns

1. **Timeout nos Testes**
   - Aumente o timeout no `WaitForWorkflowCompletion`
   - Verifique se os mocks estão configurados corretamente

2. **Falhas de Compensação**
   - Verifique se os endpoints de compensação estão mockados
   - Confirme se os dados do workflow estão corretos

3. **Chamadas HTTP Inesperadas**
   - Use `HttpTest.CallLog` para debugar
   - Verifique se todos os serviços necessários estão mockados

4. **Workflow Não Completa**
   - Verifique o status do workflow com `GetWorkflowStatus`
   - Confirme se todas as dependências estão resolvidas

---

## 📚 Referências

- [Flurl.Http.Testing Documentation](https://flurl.dev/docs/testable-http/)
- [WorkflowCore Testing](https://github.com/danielgerlag/workflow-core)
- [FluentAssertions Documentation](https://fluentassertions.com/)
