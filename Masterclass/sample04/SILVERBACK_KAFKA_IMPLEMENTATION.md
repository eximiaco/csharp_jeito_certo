# Implementação Silverback + Kafka no GymErp

## 📋 Resumo da Implementação

Esta implementação integra o **Silverback** com **Kafka** como meio de transporte para o sistema de mensageria do GymErp, substituindo a interface `IServiceBus` por uma implementação concreta que utiliza Kafka para publicação de eventos de domínio.

## 🏗️ Arquitetura Implementada

### 1. Configuração do Kafka
- **Arquivo**: `appsettings.json`
- **Classes de Configuração**:
  - `KafkaConfig.cs`
  - `KafkaConnectionConfig.cs`

### 2. ServiceBus Concreto
- **Arquivo**: `Common/Infrastructure/SilverbackServiceBus.cs`
- **Implementação**: Utiliza `IEventPublisher` do Silverback para publicar mensagens

### 3. Configuração do Silverback
- **Arquivo**: `Bootstrap/ServiceExtensions.cs`
- **Método**: `AddSilverbackKafka()`
- **Funcionalidades**:
  - Configuração do broker Kafka
  - Mapeamento de eventos para tópicos
  - Serialização JSON
  - Configuração de chaves Kafka

### 4. Testes de Integração
- **Arquivo**: `IntegrationTests/Infrastructure/IntegrationTestBase.cs`
- **Funcionalidades**:
  - Uso do `IInMemoryOutbox` para testes
  - Métodos helper para verificação de mensagens
  - Testcontainers para Kafka e PostgreSQL

## 🚀 Como Usar

### 1. Configuração do Kafka

No `appsettings.json`, configure a conexão com o Kafka:

```json
{
  "Kafka": {
    "Connection": {
      "BootstrapServers": "localhost:9092",
      "SecurityProtocol": "Plaintext",
      "SaslMechanism": "Plain",
      "SaslUsername": "",
      "SaslPassword": "",
      "EnableSslCertificateVerification": false,
      "ClientId": "GymErp"
    }
  }
}
```

### 2. Registro de Novos Eventos

Para adicionar um novo evento ao Kafka, edite o método `AddSilverbackKafka` em `ServiceExtensions.cs`:

```csharp
.AddOutbound<SeuNovoEvento>(endpoint => endpoint
    .ProduceTo("nome-do-topico")
    .WithKafkaKey<SeuNovoEvento>(envelope => envelope.Message!.Id)
    .SerializeAsJson(serializer => serializer.UseFixedType<SeuNovoEvento>())
    .DisableMessageValidation())
```

### 3. Publicação de Eventos

No seu código de domínio, injete `IServiceBus` e publique eventos:

```csharp
public class MeuHandler(IServiceBus serviceBus)
{
    public async Task HandleAsync(Request request)
    {
        // Lógica de negócio...

        var evento = new MeuEvento(id);
        await serviceBus.PublishAsync(evento);
    }
}
```

### 4. Testes de Integração

Nos testes, use os métodos helper da base class:

```csharp
[Fact]
public async Task DevePublicarEvento()
{
    // Arrange & Act
    var result = await _handler.HandleAsync(request);

    // Assert
    VerifyMessagePublished<MeuEvento>(evt => evt.Id == expectedId);
}
```

## 📦 Pacotes NuGet Adicionados

### Projeto Principal (GymErp)
- `Silverback.Integration` (4.5.0)
- `Silverback.Integration.Kafka` (4.5.0)

### Projeto de Testes (GymErp.IntegrationTests)
- `Testcontainers.Kafka` (4.0.0)
- `Silverback.Integration.Testing` (4.5.0)

## 🔧 Configuração do Ambiente

### 1. Kafka Local (Docker)

```bash
# docker-compose.yml
version: '3.8'
services:
  zookeeper:
    image: confluentinc/cp-zookeeper:latest
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000

  kafka:
    image: confluentinc/cp-kafka:latest
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
```

### 2. Executar o Ambiente

```bash
# Subir Kafka
docker-compose up -d

# Executar aplicação
dotnet run --project src/GymErp

# Executar testes
dotnet test
```

## 📊 Monitoramento

### Logs do Silverback

O Silverback está configurado para log level `Warning` no `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "Silverback": "Warning"
      }
    }
  }
}
```

### Tópicos Kafka Criados

- `enrollment-events`: Eventos de matrícula (`EnrollmentCreatedEvent`)

## 🧪 Testes

### Executar Testes de Integração

```bash
# Todos os testes
dotnet test

# Testes específicos de integração
dotnet test src/GymErp.IntegrationTests

# Teste específico
dotnet test --filter "HandlerTests"
```

### Verificação de Mensagens nos Testes

Os testes usam o `IInMemoryOutbox` do Silverback para verificar se as mensagens foram publicadas corretamente, sem necessidade de um Kafka real durante os testes.

## 🔄 Próximos Passos

1. **Configurar Consumers**: Implementar consumers para processar os eventos publicados
2. **Dead Letter Queue**: Configurar tratamento de falhas
3. **Retry Policies**: Implementar políticas de retry
4. **Monitoring**: Adicionar métricas e health checks específicos do Kafka
5. **Schema Registry**: Considerar uso de schema registry para versionamento de eventos

## 📝 Notas Importantes

- ✅ A implementação segue os padrões do projeto (Vertical Slice Architecture)
- ✅ Usa injeção de dependência com Autofac
- ✅ Mantém compatibilidade com testes existentes
- ✅ Configuração flexível via appsettings
- ✅ Logs estruturados com Serilog
- ✅ Testcontainers para testes de integração