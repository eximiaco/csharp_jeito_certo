# ADR-009: Silverback + Kafka para Mensageria Assíncrona

> **Escopo:** exclusivamente `modernized/GymErp/` — **não se aplica** ao código em `legacy/`.

## Contexto

Domain events (como `EnrollmentCreatedEvent`, `EnrollmentCanceledEvent`) precisam ser publicados para outros sistemas de forma assíncrona e desacoplada. Aplica-se a todo código de publicação/consumo de mensagens em `modernized/GymErp/`.

Evidência: `Common/Infrastructure/SilverbackServiceBus.cs` implementa `IServiceBus` usando Silverback. `Common/Kafka/KafkaConfig.cs` e `KafkaConnectionConfig.cs` configuram o broker. `SubscriptionsDbContext` recebe `IServiceBus` e publica eventos. PackageReference `Silverback.Integration 4.5.0` e `Silverback.Integration.Kafka 4.5.0`.

**Alternativas consideradas:**
1. MassTransit — rejeitado por maior complexidade de configuração para o escopo atual.
2. Publicação direta via `Confluent.Kafka` — rejeitada por ausência de abstração, dificuldade de testar e falta de outbox pattern nativo.
3. Silverback — **escolhido** pela abstração `IServiceBus`, suporte a outbox/transactional messaging e `IIntegrationSpy` para testes.

## Decisão

Adotamos Silverback como biblioteca de mensageria sobre Kafka. A interface `IServiceBus` (em `Common/`) abstrai o produtor para que os aggregates e handlers não dependam diretamente do Silverback. Tópicos são configurados na inicialização via `AddKafkaEndpoints`. Consumidores de eventos ficam em `Features/` do contexto que reage ao evento.

## Regras Operacionais

1. **SEMPRE** injete `IServiceBus` (não `IPublisher` do Silverback) nos componentes de domínio. *Padrão detectável:* `IServiceBus` em construtores de Handler e DbContext. *Motivo:* abstração isola o domínio da biblioteca de mensageria.
2. **SEMPRE** configure produtores e consumidores em `Program.cs` via `AddSilverbackKafka()`. *Padrão detectável:* `AddSilverbackKafka` em `ServiceExtensions.cs` ou `Program.cs`. *Motivo:* configuração centralizada de endpoints Kafka.
3. **NUNCA** referencie `Silverback.*` diretamente fora de `Common/Infrastructure/`. *Padrão detectável:* `using Silverback.` em arquivos fora de `Common/Infrastructure/`. *Motivo:* mantém o domínio independente da implementação de mensageria.
4. **SEMPRE** use `IIntegrationSpy` da biblioteca `Silverback.Integration.Testing` em testes de integração para verificar publicação. *Padrão detectável:* `_spy.OutboundEnvelopes` em testes. *Motivo:* evita consumir do Kafka real apenas para verificar produção.
5. **SEMPRE** nomeie eventos de domínio com sufixo `Event` e faça-os herdar `IDomainEvent` (em `GymErp.Common`). *Padrão detectável:* `public record {Nome}Event(...) : IDomainEvent;` em `Domain/**/Aggreates/**/*Event.cs`. *Motivo:* convenção uniforme permite descoberta por grep e tipagem comum no `IServiceBus.PublishAsync<T>`. Exemplo real: `Domain/Subscriptions/Aggreates/Enrollments/EnrollmentCreatedEvent.cs`.
6. **SEMPRE** handler de evento Kafka recebe o evento **diretamente** como parâmetro de `HandleAsync` — sem wrapper `Request`. *Padrão detectável:* `public async Task HandleAsync({Evento} message, CancellationToken ct)` em `Domain/**/Features/**/Handler.cs` que reage a evento. *Motivo:* evento já é o payload tipado; wrapper Request é só para endpoints HTTP. Exemplo real: `Domain/Financial/Features/ProcessCharging/Handler.cs:9`.
7. **SEMPRE** propague `CancellationToken` em `serviceBus.PublishAsync(evento, ct)` (conforme ADR-024). *Padrão detectável:* `serviceBus.PublishAsync(evento)` (sem `ct`) em código de produção. *Motivo:* publicação Kafka deve respeitar cancelamento do request HTTP que originou o evento.

## Envelope e Retry

Silverback usa envelope próprio (`IOutboundEnvelope<T>` / `IInboundEnvelope<T>`) para metadados de mensagem. Domínio publica o evento puro via `IServiceBus.PublishAsync(evento, ct)`; o envelope é montado transparentemente pelo Silverback baseado na configuração de endpoint em `Program.cs`.

Política de retry em consumer: configurada por endpoint via Silverback (`.OnError(policy => policy.Retry(...).ThenSkip())`). Para retries de publish, use `Polly` no nível do `IServiceBus` se necessário — não duplique retry no Handler.

## Exemplos

### ✅ DO

```csharp
// modernized/GymErp/Common/IServiceBus.cs
public interface IServiceBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}

// modernized/GymErp/Common/Infrastructure/SilverbackServiceBus.cs
public class SilverbackServiceBus(IPublisher publisher) : IServiceBus
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        => publisher.PublishAsync(message, cancellationToken);
}
```

### ❌ DON'T

```csharp
// Por que está errado: DbContext/Handler depende diretamente da biblioteca Silverback
public class SubscriptionsDbContext(DbContextOptions options, IPublisher silverbackPublisher)
    : DbContext(options)
{
    // usa IPublisher (Silverback) diretamente no domínio
}
```

## Benefícios

- **Domínio isolado** — `IServiceBus` pode ser substituído (ex.: RabbitMQ) sem tocar o domínio.
- **Testes fáceis** — `IIntegrationSpy` intercepta mensagens sem Kafka real.
- **Transactional messaging** — Silverback suporta outbox pattern nativo se necessário.

## Relacionado

- [ADR-006](./adr-006-aggregates-domain-events.md): domain events disparados nos aggregates.
- [ADR-015](./adr-015-testcontainers.md): testes de integração usam KafkaContainer.
- [ADR-024](./adr-024-cancellation-token-flow.md): CT propagado em `PublishAsync`.
- [ADR-027](./adr-027-tenant-access-handler.md): consumer Kafka precisa registrar tenant antes do Handler rodar.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
**Atualizada em:** 25/05/2026 — adicionadas regras 5, 6, 7 (naming de eventos, shape de handler de evento, propagação de CT) e seção "Envelope e Retry".
