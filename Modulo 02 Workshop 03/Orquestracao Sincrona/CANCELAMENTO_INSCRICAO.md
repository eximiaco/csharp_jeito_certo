# Cancelamento de Inscrição

## Visão Geral

O sistema de cancelamento de inscrição foi implementado seguindo a arquitetura Vertical Slice e usando orquestração com WorkflowCore. O cancelamento inclui:

1. **Cancelamento da inscrição** no domínio de Subscriptions
2. **Processamento de estorno** no domínio Financial
3. **Orquestração** para garantir consistência entre os serviços
4. **Compensação** para rollback em caso de falhas

## Arquitetura

### Domínio de Subscriptions

#### Feature: CancelEnrollment
- **Endpoint**: `POST /api/enrollments/{EnrollmentId}/cancel`
- **Handler**: Processa o cancelamento da inscrição
- **Evento**: Emite `EnrollmentCanceledEvent` quando cancelada

#### Estados do Enrollment
- **Active**: Pode ser cancelado → **Canceled**
- **Suspended**: Pode ser cancelado → **Canceled**  
- **Canceled**: Não pode ser cancelado novamente

### Domínio de Orquestração

#### Workflow: CancelEnrollmentFlow
- **ID**: `cancel-enrollment-workflow`
- **Endpoint**: `POST /api/enrollments/cancel-orchestration`

#### Steps do Workflow
1. **CancelEnrollmentStep**: Cancela a inscrição
2. **ProcessRefundStep**: Processa o estorno (simulado)

#### Steps de Compensação
1. **CancelEnrollmentCompensationStep**: Reativa a inscrição
2. **ProcessRefundCompensationStep**: Reverte o estorno

## Como Usar

### 1. Cancelamento Direto (Subscriptions)

```http
POST /api/enrollments/{enrollmentId}/cancel
Content-Type: application/json

{
    "enrollmentId": "guid-do-enrollment",
    "reason": "Solicitação do cliente"
}
```

**Resposta:**
```json
{
    "enrollmentId": "guid-do-enrollment",
    "canceledAt": "2024-01-15T10:30:00Z"
}
```

### 2. Cancelamento com Orquestração

```http
POST /api/enrollments/cancel-orchestration
Content-Type: application/json

{
    "enrollmentId": "guid-do-enrollment",
    "reason": "Solicitação do cliente"
}
```

**Resposta:**
```json
{
    "workflowId": "workflow-instance-id"
}
```

## Fluxo de Dados

### Cancelamento Simples
```
Client → CancelEnrollment Endpoint → Handler → Repository → Database
                                    ↓
                              Domain Event → ServiceBus
```

### Cancelamento com Orquestração
```
Client → Orchestration Endpoint → WorkflowCore
                                  ↓
                            CancelEnrollmentStep → Subscriptions API
                                  ↓
                            ProcessRefundStep → Financial API (simulado)
```

### Compensação em Caso de Falha
```
Falha no ProcessRefundStep → ProcessRefundCompensationStep → Reverte estorno
                           → CancelEnrollmentCompensationStep → Reativa inscrição
```

## Eventos de Domínio

### EnrollmentCanceledEvent
```csharp
public record EnrollmentCanceledEvent(Guid EnrollmentId, DateTime CanceledAt) : IDomainEvent;
```

Este evento é emitido quando uma inscrição é cancelada e pode ser consumido por outros serviços para:
- Notificações ao cliente
- Relatórios de cancelamento
- Análise de churn

## Configuração

### ServicesSettings
```json
{
    "ServicesSettings": {
        "SubscriptionsUri": "http://localhost:5001",
        "ProcessPaymentUri": "http://localhost:5002",
        "ScheduleEvaluationUri": "http://localhost:5003"
    }
}
```

### Registro de Dependências

Os componentes estão registrados automaticamente nos módulos:
- `SubscriptionsModule`: Feature de cancelamento
- `OrchestrationModule`: Workflow de cancelamento

## Tratamento de Erros

### Estados Inválidos
- **409 Conflict**: Inscrição já cancelada
- **404 Not Found**: Inscrição não encontrada

### Falhas de Rede
- **Retry Policy**: Polly configurado para tentativas automáticas
- **Compensação**: Rollback automático em caso de falha

### Workflow
- **Retry**: 30 segundos entre tentativas
- **Saga Pattern**: Compensação automática

## Próximos Passos

Para implementação completa, seria necessário:

1. **Implementar lógica real de estorno** no módulo Financial
2. **Criar endpoints de reativação** para compensação
3. **Implementar notificações** via ServiceBus
4. **Adicionar testes de integração** com TestContainers
5. **Implementar auditoria** de cancelamentos
6. **Criar relatórios** de cancelamentos

## Exemplo de Uso Completo

```csharp
// Cancelamento via orquestração
var client = new HttpClient();
var request = new
{
    EnrollmentId = Guid.Parse("enrollment-id"),
    Reason = "Mudança de endereço"
};

var response = await client.PostAsJsonAsync(
    "/api/enrollments/cancel-orchestration", 
    request);

var result = await response.Content.ReadFromJsonAsync<Response>();
// result.WorkflowId contém o ID do workflow para acompanhamento
```
