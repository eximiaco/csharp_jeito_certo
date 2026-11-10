# ADR-010: Flurl + Polly para Chamadas HTTP Externas com Retry

> **Escopo:** exclusivamente `modernized/GymErp/` — **não se aplica** ao código em `legacy/`.

## Contexto

O contexto Orchestration faz chamadas HTTP para o sistema legado (`LegacyAdapter`) e para a API de Subscriptions (`ModernizedAdapter`). Essas chamadas precisam de retry automático em falhas transitórias. Aplica-se a todo código que faz HTTP outbound em `modernized/GymErp/`.

Evidência: `Common/HttpRetryPolicy.cs` define política Polly. `GymErp.csproj` referencia `Flurl 4.0.0` e `Flurl.Http 4.0.2` e `Polly 8.5.2`. `Domain/Orchestration/Features/EnrollmentOrchestrator/LegacyAdapter.cs` e `ModernizedAdapter.cs` usam Flurl para chamadas HTTP.

**Alternativas consideradas:**
1. `HttpClient` nativo com `IHttpClientFactory` — rejeitado pela verbosidade de construção de URLs e ausência de fluent API.
2. RestSharp — rejeitado pela API mais verbosa comparada ao Flurl.
3. Flurl + Polly — **escolhido** pela API fluente de Flurl e robustez do Polly para resiliência.

## Decisão

Adotamos Flurl como cliente HTTP fluente e Polly para políticas de resiliência (retry com backoff exponencial). Adaptadores HTTP ficam em `Features/{Feature}/` do contexto que os usa. A política de retry é centralizada em `Common/HttpRetryPolicy.cs` e reutilizada pelos adaptadores.

## Regras Operacionais

1. **SEMPRE** use Flurl para chamadas HTTP outbound no projeto modernizado. *Padrão detectável:* `using Flurl.Http` em adaptadores HTTP. *Motivo:* API fluente reduz boilerplate e facilita construção de URLs com parâmetros.
2. **SEMPRE** aplique política Polly de retry em chamadas a sistemas externos. *Padrão detectável:* `HttpRetryPolicy` ou `Policy.HandleResult<>` nos adaptadores. *Motivo:* sistemas externos têm falhas transitórias; retry automático aumenta resiliência.
3. **NUNCA** use `HttpClient` diretamente fora de `Common/`. *Padrão detectável:* `new HttpClient()` ou `IHttpClientFactory` injetado em `Features/`. *Motivo:* Flurl gerencia o ciclo de vida dos clientes internamente.
4. **SEMPRE** encapsule chamadas HTTP externas em classes Adapter separadas. *Padrão detectável:* `*Adapter.cs` em `Features/{Feature}/`. *Motivo:* isola a feature da mecânica HTTP e facilita mocking em testes.

## Exemplos

### ✅ DO

```csharp
// modernized/GymErp/Common/HttpRetryPolicy.cs
public static class HttpRetryPolicy
{
    public static AsyncRetryPolicy<IFlurlResponse> Create() =>
        Policy
            .HandleResult<IFlurlResponse>(r => !r.ResponseMessage.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
}

// modernized/GymErp/Domain/Orchestration/Features/EnrollmentOrchestrator/LegacyAdapter.cs
public class LegacyAdapter(IOptions<LegacyApiConfiguration> config)
{
    public async Task<Result> CreateEnrollmentAsync(CreateEnrollmentRequest request)
    {
        var policy = HttpRetryPolicy.Create();
        var response = await policy.ExecuteAsync(() =>
            config.Value.BaseUrl
                .AppendPathSegment("/api/enrollments")
                .PostJsonAsync(request));

        return response.StatusCode == 200
            ? Result.Success()
            : Result.Failure("Falha ao criar matrícula no sistema legado");
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: HttpClient direto sem retry, URL concatenada manualmente
public class LegacyAdapter(HttpClient httpClient)
{
    public async Task CreateEnrollmentAsync(CreateEnrollmentRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await httpClient.PostAsync("http://legacy-api/api/enrollments", content);
        // sem retry, sem tratamento de erro
    }
}
```

## Benefícios

- **API fluente** — `baseUrl.AppendPathSegment(...).PostJsonAsync(...)` legível.
- **Resiliência** — retry com backoff exponencial para falhas transitórias.
- **Encapsulamento** — adaptadores isolam a feature da mecânica HTTP.

## Relacionado

- [ADR-008](./adr-008-workflowcore-sagas.md): adaptadores são chamados nos passos do workflow.
- [ADR-004](./adr-004-vertical-slice.md): adaptadores ficam dentro da feature que os usa.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
