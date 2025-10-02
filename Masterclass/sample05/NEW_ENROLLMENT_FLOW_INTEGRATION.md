# NewEnrollmentFlow - Integração Sistema Modernizado + Legado

## 📋 Visão Geral

A funcionalidade `NewEnrollmentFlow` foi ajustada para integrar o sistema modernizado com o sistema legado, seguindo a arquitetura Vertical Slice e os padrões estabelecidos do projeto GymErp.

## 🎯 Objetivo

Implementar um fluxo de matrícula que:
1. **Cria inscrição no sistema modernizado** (Subscriptions)
2. **Cria inscrição no sistema legado** (sem pagamento)
3. **Processa pagamento via sistema legado**
4. **Agenda avaliação via sistema legado**

## 🏗️ Arquitetura Implementada

### Fluxo de Execução
```
1. AddEnrollmentStep (Sistema Modernizado)
   ↓
2. AddLegacyEnrollmentStep (Sistema Legado - sem pagamento)
   ↓
3. ProcessPaymentStep (Sistema Legado)
   ↓
4. ScheduleEvaluationStep (Sistema Legado)
```

### Compensação (Saga Pattern)
Cada step possui seu step de compensação correspondente:
- `AddEnrollmentCompensationStep`
- `AddLegacyEnrollmentCompensationStep` (com log + TODO)
- `ProcessPaymentCompensationStep`
- `ScheduleEvaluationCompensationStep`

## 📁 Estrutura de Arquivos

```
Domain/Orchestration/Features/NewEnrollmentFlow/
├── Endpoint.cs                                    # Endpoint FastEndpoints
├── Request.cs                                     # Modelo de entrada
├── NewEnrollmentFlowData.cs                      # Dados do workflow
├── MainWorkflow.cs                               # Definição do workflow
└── Steps/
    ├── AddEnrollmentStep.cs                      # Step 1: Sistema modernizado
    ├── AddEnrollmentCompensationStep.cs          # Compensação step 1
    ├── AddLegacyEnrollmentStep.cs                # Step 2: Sistema legado
    ├── AddLegacyEnrollmentCompensationStep.cs    # Compensação step 2
    ├── ProcessPaymentStep.cs                     # Step 3: Pagamento legado
    ├── ProcessPaymentCompensationStep.cs         # Compensação step 3
    ├── ScheduleEvaluationStep.cs                 # Step 4: Agendamento legado
    └── ScheduleEvaluationCompensationStep.cs     # Compensação step 4
```

## 🔧 Configurações

### ServicesSettings
```csharp
public record ServicesSettings
{
    public string SubscriptionsUri { get; init; } = string.Empty;  // Sistema modernizado
    public string LegacyApiUri { get; init; } = string.Empty;      // Sistema legado
    public string ProcessPaymentUri { get; init; } = string.Empty; // Mantido para compatibilidade
    public string ScheduleEvaluationUri { get; init; } = string.Empty; // Mantido para compatibilidade
}
```

### appsettings.json
```json
{
  "ServicesSettings": {
    "SubscriptionsUri": "http://localhost:5001",
    "LegacyApiUri": "http://localhost:5000",
    "ProcessPaymentUri": "http://localhost:5002",
    "ScheduleEvaluationUri": "http://localhost:5003"
  }
}
```

## 📊 Modelos de Dados

### Request (Entrada)
```csharp
public readonly record struct Request(
    Guid ClientId,
    Guid PlanId,
    string Name,
    string Email,
    string Phone,
    string Document,
    DateTime BirthDate,
    string Gender,
    string Address,
    DateTime StartDate,           // Novo: Data início plano
    DateTime EndDate,             // Novo: Data fim plano
    Guid PersonalId,              // Novo: ID do personal trainer
    DateTime AssessmentDate,      // Novo: Data do agendamento
    decimal Weight,               // Novo: Peso inicial
    decimal Height,               // Novo: Altura
    decimal BodyFatPercentage,   // Novo: Percentual de gordura
    string Notes = ""             // Novo: Observações
);
```

### NewEnrollmentFlowData (Dados do Workflow)
```csharp
public class NewEnrollmentFlowData
{
    // Campos existentes...
    public Guid ClientId { get; set; }
    public Guid PlanId { get; set; }
    public Guid EnrollmentId { get; set; }
    public bool EnrollmentCreated { get; set; }
    public bool PaymentProcessed { get; set; }
    public bool EvaluationScheduled { get; set; }
    
    // Dados do cliente...
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    // Novos campos para integração com sistema legado
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid PersonalId { get; set; }
    public DateTime AssessmentDate { get; set; }
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public decimal BodyFatPercentage { get; set; }
    public string Notes { get; set; } = string.Empty;
    
    // Campos para controle do fluxo com sistema legado
    public Guid LegacyEnrollmentId { get; set; }
    public bool LegacyEnrollmentCreated { get; set; }
}
```

## 🔄 Steps Implementados

### 1. AddEnrollmentStep
- **Sistema**: Modernizado (Subscriptions)
- **Endpoint**: `POST {SubscriptionsUri}/enrollments`
- **Função**: Cria matrícula no sistema modernizado
- **Retorna**: `EnrollmentId` do sistema modernizado

### 2. AddLegacyEnrollmentStep
- **Sistema**: Legado
- **Endpoint**: `POST {LegacyApiUri}/api/enrollment/create`
- **Função**: Cria matrícula no sistema legado (sem pagamento)
- **Mapeamento**: Dados do modernizado → `EnrollmentDto` do legado
- **Retorna**: `EnrollmentId` do sistema legado

### 3. ProcessPaymentStep
- **Sistema**: Legado
- **Endpoint**: `POST {LegacyApiUri}/api/enrollment/process-payment`
- **Função**: Processa pagamento usando `LegacyEnrollmentId`
- **Dados**: `ProcessPaymentDto { EnrollmentId }`

### 4. ScheduleEvaluationStep
- **Sistema**: Legado
- **Endpoint**: `POST {LegacyApiUri}/api/enrollment/schedule-assessment`
- **Função**: Agenda avaliação física
- **Dados**: `ScheduleAssessmentDto` com todos os campos necessários

## 🛡️ Tratamento de Erros

### Retry Policy
- **Configuração**: Polly com retry automático
- **Timeout**: 30 segundos entre tentativas
- **Tratamento**: Exceções são propagadas para o WorkflowCore

### Compensação
- **AddLegacyEnrollmentCompensationStep**: Log + TODO para implementação futura
- **Outros steps**: Mantêm compensações existentes

## 🚀 Como Usar

### Endpoint
```http
POST /api/enrollments
Content-Type: application/json

{
  "clientId": "guid",
  "planId": "guid",
  "name": "João da Silva Santos",
  "email": "joao.silva@email.com",
  "phone": "11999999999",
  "document": "12345678901",
  "birthDate": "1990-01-01T00:00:00Z",
  "gender": "M",
  "address": "Rua das Flores, 123",
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-12-31T23:59:59Z",
  "personalId": "guid",
  "assessmentDate": "2024-01-08T10:00:00Z",
  "weight": 75.5,
  "height": 1.75,
  "bodyFatPercentage": 15.0,
  "notes": "Cliente iniciante"
}
```

### Resposta
```json
{
  "workflowId": "workflow-instance-id"
}
```

## 🔍 Monitoramento

### Logs
- **AddLegacyEnrollmentCompensationStep**: Logs de compensação com `LegacyEnrollmentId`
- **WorkflowCore**: Logs automáticos de execução dos steps
- **Polly**: Logs de retry e falhas de conectividade

### Health Checks
- Sistema modernizado: `/healthz`
- Sistema legado: Depende da configuração do legado

## 📝 TODOs e Melhorias Futuras

### Compensação AddLegacyEnrollmentStep
```csharp
// TODO: Implementar lógica de compensação para remover matrícula do sistema legado
// Por enquanto apenas logamos a necessidade de compensação
```

### Possíveis Melhorias
1. **Implementar compensação completa** para `AddLegacyEnrollmentStep`
2. **Adicionar validações** de dados antes de chamar APIs externas
3. **Implementar circuit breaker** para APIs do sistema legado
4. **Adicionar métricas** de performance e sucesso/falha
5. **Implementar notificações** de status do workflow

## 🧪 Testes

### Testes de Integração
- **Localização**: `src/GymErp.IntegrationTests/Orchestration/NewEnrollmentFlow/`
- **Estratégia**: Usar `IntegrationTestBase` com TestContainers
- **Cenários**: Sucesso completo, falhas em cada step, compensações

### Testes Unitários
- **Localização**: `src/GymErp.UnitTests/`
- **Foco**: Lógica de mapeamento de dados, validações

## 🔗 Dependências

### Bibliotecas Utilizadas
- **WorkflowCore**: Orquestração de workflows
- **Flurl**: Cliente HTTP para chamadas às APIs
- **Polly**: Retry policies e resilience
- **FastEndpoints**: Framework de endpoints
- **CSharpFunctionalExtensions**: Result pattern

### Sistemas Externos
- **Sistema Modernizado**: `http://localhost:5001`
- **Sistema Legado**: `http://localhost:5000`

---

## 📋 Resumo da Implementação

✅ **Concluído**:
- Ajuste do `NewEnrollmentFlow` para integrar sistemas modernizado e legado
- Criação do `AddLegacyEnrollmentStep` com mapeamento de dados
- Modificação dos steps de pagamento e agendamento para usar sistema legado
- Implementação de compensação com log + TODO
- Atualização de modelos de dados para suportar novos campos
- Configuração de URLs do sistema legado
- Build bem-sucedido com apenas warnings menores

🔄 **Fluxo Implementado**:
1. Sistema Modernizado → Sistema Legado → Pagamento Legado → Agendamento Legado
2. Compensação automática em caso de falha
3. Retry automático com Polly
4. Logs estruturados para monitoramento

A funcionalidade está pronta para uso e pode ser testada através do endpoint `/api/enrollments`.
