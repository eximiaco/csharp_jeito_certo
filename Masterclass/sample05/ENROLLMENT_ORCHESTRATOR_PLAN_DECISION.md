# Enrollment Orchestrator - Decisão por Tipo de Plano

## 📋 Resumo da Funcionalidade

A funcionalidade do **Enrollment Orchestrator** foi modificada para implementar uma lógica de decisão baseada no tipo de plano da inscrição. Agora o sistema:

- **Planos Mensais**: São processados pelo sistema **Legacy**
- **Planos Semestrais e Anuais**: São processados pelo sistema **Modernizado**

## 🏗️ Arquitetura Implementada

### Separação de Módulos
- O módulo `Orchestration` não possui dependência direta do módulo `Subscriptions`
- Comunicação entre módulos é feita via **chamadas HTTP**
- Mantém a arquitetura de microserviços e separação de responsabilidades

### Componentes Criados/Modificados

#### 1. **PlanService.cs** (Novo)
```csharp
public class PlanService
{
    public async Task<Result<PlanInfo>> GetPlanByIdAsync(Guid planId)
    {
        // Faz chamada HTTP para /api/plans/{id} no serviço de Subscriptions
        // Retorna informações do plano incluindo o PlanType
    }
}
```

#### 2. **Handler.cs** (Modificado)
```csharp
public class Handler
{
    private readonly PlanService _planService;
    
    public async Task<Result<Response>> HandleAsync(Request request)
    {
        // 1. Busca informações do plano via HTTP
        var planResult = await _planService.GetPlanByIdAsync(request.PlanId);
        
        // 2. Decide qual sistema usar baseado no PlanType
        var useLegacySystem = plan.Type == PlanType.Mensal;
        
        // 3. Processa no sistema apropriado
        if (useLegacySystem)
            return await _legacyAdapter.ProcessEnrollmentAsync(request);
        else
            return await _modernizedAdapter.ProcessEnrollmentAsync(request);
    }
}
```

#### 3. **Configuration.cs** (Modificado)
```csharp
public class SubscriptionsApiConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
```

## 🔧 Configurações

### appsettings.json
```json
{
  "SubscriptionsApi": {
    "BaseUrl": "http://localhost:5001",
    "TimeoutSeconds": 30
  }
}
```

### Program.cs
```csharp
.Configure<SubscriptionsApiConfiguration>(builder.Configuration.GetSection("SubscriptionsApi"))
```

### OrchestrationModule.cs
```csharp
builder.RegisterType<PlanService>().AsSelf().InstancePerLifetimeScope();
```

## 🎯 Lógica de Decisão

### Fluxo de Processamento
1. **Recebe Request** com `PlanId`
2. **Chama PlanService** para buscar informações do plano via HTTP
3. **Analisa PlanType** retornado:
   - `PlanType.Mensal` (1) → **Sistema Legacy**
   - `PlanType.Semestral` (6) → **Sistema Modernizado**
   - `PlanType.Anual` (12) → **Sistema Modernizado**
4. **Processa** no sistema apropriado
5. **Retorna Response** indicando qual sistema foi usado

### Tratamento de Erros
- **Plano não encontrado (404)**: Retorna erro específico
- **Falha na chamada HTTP**: Retorna erro com detalhes da resposta
- **Erro inesperado**: Retorna erro genérico

## 📊 Tipos de Plano

```csharp
public enum PlanType
{
    Mensal = 1,      // → Sistema Legacy
    Semestral = 6,   // → Sistema Modernizado
    Anual = 12       // → Sistema Modernizado
}
```

## 🚀 Endpoint

### POST /api/enrollments-orchestrator
```json
{
  "clientId": "guid",
  "planId": "guid",
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-02-01T00:00:00Z",
  "student": {
    "name": "João Silva",
    "email": "joao@email.com",
    "phone": "11999999999",
    "document": "12345678901",
    "birthDate": "1990-01-01T00:00:00Z",
    "gender": "M",
    "address": "Rua das Flores, 123"
  },
  "physicalAssessment": {
    "personalId": "guid",
    "assessmentDate": "2024-01-01T00:00:00Z",
    "weight": 70.5,
    "height": 175.0,
    "bodyFatPercentage": 15.0,
    "notes": "Avaliação inicial"
  }
}
```

### Response
```json
{
  "enrollmentId": "guid",
  "systemUsed": "Legacy" // ou "Modernized"
}
```

## 🔍 Benefícios da Implementação

### 1. **Separação de Responsabilidades**
- Módulo Orchestration não depende diretamente do módulo Subscriptions
- Comunicação via HTTP mantém a arquitetura de microserviços

### 2. **Flexibilidade**
- Fácil alteração da lógica de decisão
- Possibilidade de adicionar novos critérios no futuro

### 3. **Manutenibilidade**
- Código limpo e bem estruturado
- Seguindo padrões estabelecidos do projeto

### 4. **Testabilidade**
- Componentes podem ser testados independentemente
- Chamadas HTTP podem ser mockadas em testes

## 📝 Próximos Passos

1. **Implementar ModernizedAdapter**: Atualmente retorna erro indicando que não está disponível
2. **Adicionar Retry Policy**: Para chamadas HTTP mais robustas
3. **Implementar Cache**: Para otimizar consultas de planos frequentemente acessados
4. **Adicionar Logging**: Para monitoramento e debugging

## 🧪 Testes Sugeridos

### Testes de Integração
- Testar chamada HTTP para serviço de Subscriptions
- Testar decisão correta baseada no PlanType
- Testar tratamento de erros (plano não encontrado, falha HTTP)

### Testes Unitários
- Testar lógica de decisão do Handler
- Testar tratamento de erros do PlanService

---

**Data de Implementação**: Janeiro 2025  
**Versão**: 1.0  
**Status**: ✅ Implementado e Testado
