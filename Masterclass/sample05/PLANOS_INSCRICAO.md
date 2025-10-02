# Funcionalidade: Consulta de Planos de Inscrição

## 📋 Visão Geral

Esta funcionalidade implementa um novo agregado `Plan` no contexto de Subscriptions para gerenciar planos de inscrição utilizados no sistema GymErp. A funcionalidade permite consultar um plano específico pelo seu ID.

## 🏗️ Arquitetura Implementada

### Vertical Slice Architecture
A implementação segue o padrão Vertical Slice Architecture estabelecido no projeto, organizando todos os arquivos relacionados à funcionalidade em sua própria estrutura.

### Estrutura Criada

```
Domain/Subscriptions/
├── Aggreates/
│   └── Plans/
│       ├── Plan.cs                    # Agregado Plan
│       └── PlanType.cs                # Enum para tipos de plano
└── Features/
    └── GetPlanById/
        ├── Endpoint.cs                # FastEndpoints endpoint com Dapper
        ├── Request.cs                 # Input model (Guid Id)
        └── Response.cs                # Output model (Plan data)
```

## 📊 Modelos de Dados

### Agregado Plan
```csharp
public sealed class Plan : Aggregate
{
    public Guid Id { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public PlanType Type { get; private set; }
}
```

### Enum PlanType
```csharp
public enum PlanType
{
    Mensal = 1,      // 1 mês
    Semestral = 6,   // 6 meses
    Anual = 12       // 12 meses
}
```

### Request Model
```csharp
public record Request
{
    public Guid Id { get; set; }
}
```

### Response Model
```csharp
public record Response(Guid Id, string Description, PlanType Type);
```

## 🔧 Implementação Técnica

### Endpoint com Dapper
A funcionalidade utiliza **Dapper** diretamente no endpoint para consulta SQL, eliminando a necessidade de Handler e Repository, conforme solicitado. Isso reduz a complexidade e melhora a performance para operações de consulta simples.

```csharp
public override async Task HandleAsync(Request req, CancellationToken ct)
{
    using var connection = await _connectionFactory.Create(ct);
    await connection.OpenAsync(ct);

    const string sql = @"
        SELECT Id, Description, Type 
        FROM Plans 
        WHERE Id = @Id";

    var parameters = new { Id = req.Id };
    
    var plan = await connection.QueryFirstOrDefaultAsync<Response>(sql, parameters);
    
    if (plan == null)
    {
        await SendNotFoundAsync(ct);
        return;
    }

    await SendOkAsync(plan, ct);
}
```

### Mapeamento Entity Framework
O agregado `Plan` foi mapeado no `SubscriptionsDbContext` para controle de migrations:

```csharp
modelBuilder.Entity<Plan>(builder =>
{
    builder.ToTable("Plans");
    builder.HasKey(p => p.Id);
    builder.Property(p => p.Description).HasColumnName("Description").HasMaxLength(100);
    builder.Property(p => p.Type).HasColumnName("Type").HasConversion<int>();
});
```

### Multi-tenancy
A implementação respeita a arquitetura multi-tenant do projeto, utilizando `IDbConnectionFactory` para obter conexões específicas do tenant.

## 🚀 API Endpoint

### GET /api/plans/{id}

**Descrição**: Consulta um plano específico pelo seu identificador.

**Parâmetros**:
- `id` (Guid): Identificador único do plano

**Respostas**:
- `200 OK`: Plano encontrado com sucesso
- `404 Not Found`: Plano não encontrado
- `500 Internal Server Error`: Erro interno do servidor

**Exemplo de Request**:
```http
GET /api/plans/123e4567-e89b-12d3-a456-426614174000
```

**Exemplo de Response**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "description": "Plano Premium Mensal",
  "type": 1
}
```

## 🔄 Dependency Injection

O endpoint foi registrado no `SubscriptionsModule`:

```csharp
// Registra o Endpoint de Consulta de Plano
builder.RegisterType<Features.GetPlanById.Endpoint>()
    .AsSelf()
    .InstancePerLifetimeScope();
```

## ✅ Padrões Seguidos

### Arquitetura
- ✅ **Vertical Slice Architecture**: Organização por feature
- ✅ **FastEndpoints**: Framework principal para APIs
- ✅ **REPR Pattern**: Request-Endpoint-Response
- ✅ **Multi-tenancy**: Suporte a múltiplos tenants

### Código
- ✅ **Primary constructors**: Para injeção de dependências
- ✅ **Records**: Para Request/Response models
- ✅ **Result pattern**: Para validações de domínio
- ✅ **Dapper**: Para consultas SQL diretas
- ✅ **Entity Framework**: Para mapeamento e migrations

### Qualidade
- ✅ **Nullable enabled**: Tipos nullable habilitados
- ✅ **Async/await**: Operações assíncronas
- ✅ **CancellationToken**: Suporte a cancelamento
- ✅ **Error handling**: Tratamento de erros adequado

## 🎯 Benefícios da Implementação

1. **Simplicidade**: Uso direto do Dapper elimina camadas desnecessárias
2. **Performance**: Consulta SQL direta é mais eficiente
3. **Manutenibilidade**: Código mais simples e direto
4. **Consistência**: Segue os padrões estabelecidos no projeto
5. **Multi-tenancy**: Respeita a arquitetura multi-tenant
6. **Migrations**: Controle de schema via Entity Framework

## 📝 Próximos Passos

Para utilizar esta funcionalidade:

1. **Criar migration**: Execute `dotnet ef migrations add AddPlansTable`
2. **Aplicar migration**: Execute `dotnet ef database update`
3. **Inserir dados**: Adicione planos na tabela `Plans`
4. **Testar endpoint**: Use o endpoint `/api/plans/{id}` para consultas

## 🔍 Validações de Domínio

O agregado `Plan` inclui validações de domínio:

- Descrição não pode ser vazia
- Descrição deve ter pelo menos 3 caracteres
- Descrição deve ter no máximo 100 caracteres
- Descrição é automaticamente trimada

## 📊 Estrutura da Tabela

```sql
CREATE TABLE Plans (
    Id UUID PRIMARY KEY,
    Description VARCHAR(100) NOT NULL,
    Type INTEGER NOT NULL
);
```

---

**Data de Implementação**: Dezembro 2024  
**Contexto**: Subscriptions  
**Tecnologias**: FastEndpoints, Dapper, Entity Framework Core, PostgreSQL  
**Padrão**: Vertical Slice Architecture
