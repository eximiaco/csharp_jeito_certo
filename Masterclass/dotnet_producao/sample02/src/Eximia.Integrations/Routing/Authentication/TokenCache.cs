using System.Collections.Concurrent;

namespace Eximia.Integrations.Routing.Authentication;

/// <summary>
///     Cache de tokens por destino com renovação single-flight: sob carga, N mensagens simultâneas geram UMA
///     chamada ao provedor de identidade. Deve ser SINGLETON, porque os semáforos precisam sobreviver entre
///     as mensagens (ver os samples 1 e 2).
/// </summary>
public sealed class TokenCache
{
    private static readonly TimeSpan Margin = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, JwtBearerToken> _tokens = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

    public async Task<JwtBearerToken> GetAsync(
        string key,
        Func<CancellationToken, Task<JwtBearerToken>> requestNewToken,
        CancellationToken cancellationToken)
    {
        if (_tokens.TryGetValue(key, out var token) && !token.Expired(Margin))
            return token;                                              // caminho rápido: sem lock

        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Outra mensagem pode ter renovado enquanto esperávamos o portão.
            if (_tokens.TryGetValue(key, out token) && !token.Expired(Margin))
                return token;

            return _tokens[key] = await requestNewToken(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Invalidate(string key) => _tokens.TryRemove(key, out _);
}
