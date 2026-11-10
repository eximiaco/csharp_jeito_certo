# 01 · Sync-over-async e o cache de token

**Branch:** `masterclass/dotnet-producao/01-sync-over-async` · **Parte de:** `00-baseline`
**Para ver o que mudou:** `git diff masterclass/dotnet-producao/00-baseline..masterclass/dotnet-producao/01-sync-over-async`

## O problema

O dispatcher autentica no destino com um token JWT (client credentials). Dois pontos desse caminho tinham problemas.

### 1. A renovação do token no 401 bloqueava a thread

Quando o destino responde 401, a policy do Polly renova o token e tenta de novo. O callback de retry era **síncrono** e esperava operações assíncronas com `.Result` e `.GetAwaiter().GetResult()`:

```csharp
.RetryAsync(
    onRetry: (result, retry, context) =>
    {
        // ...
        if (result.Exception is FlurlHttpException flurlException)
            error = flurlException.GetResponseStringAsync().Result;

        telemetry!.UnAuthorizeCall();
        provider.GetTokenAsync(route, correlationId).GetAwaiter().GetResult();
    });
```

Cada `.Result` segura uma thread do `ThreadPool` até a rede responder. O dispatcher processa várias mensagens em paralelo; se o provedor de identidade ficar lento, todas elas caem nesse caminho ao mesmo tempo e **bloqueiam threads em vez de liberá-las**. O pool começa com poucas threads (o mínimo é o número de núcleos) e só cria novas a cada cerca de 500 ms. O resultado é *task starvation*: a latência explode, a CPU fica baixa e o processo parece "travado" sem estar fazendo nada.

`.Wait()` tem exatamente o mesmo efeito: as três formas (`.Result`, `.Wait()` e `.GetAwaiter().GetResult()`) bloqueiam a thread. A única diferença é como a exceção chega (`AggregateException` nas duas primeiras).

O gatilho em produção é concreto: o token de um destino expira, **todas** as entregas em andamento para aquele destino tomam 401 ao mesmo tempo, e todas entram nesse callback bloqueante juntas.

Em ASP.NET Core não há `SynchronizationContext`, então o deadlock clássico não aparece. O que aparece é esse esgotamento do pool, que é mais difícil de diagnosticar.

### 2. O cache de token não funcionava

[TokenServiceProvider.cs](../src/Eximia.Integrations/Routing/Authentication/TokenServiceProvider.cs) tinha três defeitos encadeados:

```csharp
JwtBearerToken? token = null;                          // só é preenchido DENTRO do factory do cache
var cachedValue = await _memoryCache.GetOrCreateAsync(key, async cacheEntry =>
{
    token = await RequestNewTokenAsync(route);
    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(token!.expires_in);
    return token;
});

if (!cachedValue!.Expired(TimeSpan.FromSeconds(5)))
    return token;                                       // null quando o valor veio do cache
```

1. **Retorno nulo.** Quando o token vem do cache, o método devolve a variável local `token`, que está `null`.
2. **Unidade de `expires_in` misturada.** O cache usa `FromMilliseconds(expires_in)`, e `Expired` trata o mesmo valor como um *epoch* em segundos. Pelo padrão OAuth2, `expires_in` é a **duração em segundos** (por exemplo, 3600). Nesse caso, o cache dura cerca de 3,6 segundos e `Expired` considera o token sempre vencido (ano 1970).
3. **Um bug esconde o outro.** Como `Expired` é sempre verdadeiro, o `return token` (nulo) nunca é alcançado: o método **pede um token novo ao provedor a cada chamada**. Corrigir só a unidade faria o `null` aparecer. É um bom exemplo de por que corrigir um bug isolado, sem teste, pode piorar o sistema.

Além disso não havia proteção de concorrência: sob carga, N mensagens simultâneas disparam N pedidos de token ao mesmo tempo (*stampede*). E nada recebia `CancellationToken`.

> A confirmação de que `expires_in` vem em segundos depende do provedor de identidade usado. Vale conferir na documentação dele.

## A solução

### Async de ponta a ponta

```csharp
onRetryAsync: async (result, retry, context) =>
{
    // ...
    if (result.Exception is FlurlHttpException flurlException)
        error = await flurlException.GetResponseStringAsync();

    telemetry!.UnAuthorizeCall();
    provider.Invalidate(route);                       // descarta o token recusado
    await provider.GetTokenAsync(route, correlationId);
}
```

Além de aguardar de verdade, a policy agora **invalida** o token em cache antes de pedir outro. Antes, ela pedia um token e descartava o resultado: o 401 se repetia, porque o token recusado continuava sendo o usado.

### Validade absoluta, calculada na aquisição

[JwtBearerToken.cs](../src/Eximia.Integrations/Routing/Authentication/JwtBearerToken.cs) guarda o instante de expiração no momento em que o token é recebido, e a comparação deixa de misturar unidades:

```csharp
public DateTimeOffset ExpiresAt { get; } = DateTimeOffset.UtcNow.AddSeconds(expires_in);
public bool Expired(TimeSpan margin) => DateTimeOffset.UtcNow.Add(margin) >= ExpiresAt;
```

### Renovação single-flight

[TokenCache.cs](../src/Eximia.Integrations/Routing/Authentication/TokenCache.cs) faz com que **uma única chamada** ao provedor atenda todas as mensagens simultâneas de um destino:

```csharp
if (_tokens.TryGetValue(key, out var token) && !token.Expired(Margin))
    return token;                                       // caminho rápido: sem lock

var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
await gate.WaitAsync(cancellationToken);
try
{
    if (_tokens.TryGetValue(key, out token) && !token.Expired(Margin))
        return token;                                   // outra mensagem já renovou enquanto esperávamos

    return _tokens[key] = await requestNewToken(cancellationToken);
}
finally { gate.Release(); }
```

- É **singleton**: os semáforos precisam sobreviver entre as mensagens. Por isso foi registrado com `SingleInstance()` nos módulos do Autofac.
- O `TokenCache` e o provider já aceitam `CancellationToken`. Quem ainda não o repassa é o dispatcher: isso é assunto da próxima branch.

## Regras para levar

- **Nunca bloqueie em código assíncrono** (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) em serviços que processam mensagens em paralelo. Se um callback é síncrono, procure a variante assíncrona (`onRetryAsync`, não `onRetry`).
- **`Task.Delay`, não `Thread.Sleep`**, em esperas e backoffs.
- **`Task.Run` em volta de código síncrono não é async**: só empurra o bloqueio para outra thread do mesmo pool.
- Cache compartilhado e caro de renovar precisa de **single-flight**, e de validade guardada em um instante absoluto.

## Novidades de plataforma

- `System.Threading.Lock` (.NET 9) é ótimo para seções críticas **síncronas**, mas não pode conter `await`. Por isso o single-flight aqui usa `SemaphoreSlim`.

## Próxima branch

`02-cancellation-token`: o token que não chega às queries, ao HTTP e às operações filhas.
