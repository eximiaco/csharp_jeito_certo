using CSharpFunctionalExtensions;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Npgsql;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Postgres;
using Eximia.Integrations.Routing.Rules;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing;

public class RoutingDataAccess(
    IMemoryCache memoryCache,
    NpgsqlDataSource dataSource,
    IOptions<List<RouteAuthenticationSettings>> routeAuthenticationSettings)
{
    public async Task<Maybe<Route>> GetRoute(string entity, string destiny)
    {
        var key = $"{destiny}.{entity}";
        var cachedValue = await memoryCache.GetOrCreateAsync(
            key,
            async cacheEntry =>
            {
                cacheEntry.SlidingExpiration = TimeSpan.FromSeconds(3);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
                cacheEntry.Size = 500;
                await using var connection = dataSource.CreateConnection();
                var dto = await connection
                    // PROBLEMA: consulta sem token. Não é cancelada no shutdown nem no timeout.
                    .QueryFirstOrDefaultAsync<RouteDto>(
                        @"SELECT id,
                                    origin,
                                    destination,
                                    entity,
                                    topic_name AS TopicName,
                                    callback_required AS CallbackRequired,
                                    uri,
                                    content_type AS ContentType,
                                    max_retry AS MaxRetry,
                                    time_out AS TimeOut,
                                    classname,
                                    mode AS Mode,
                                    callback_error_uri AS CallbackErrorUri,
                                    send_payload_as_link AS SendPayloadAsLink,
                                    routing_rule AS RoutingRule
                            FROM destination_routes
                            WHERE destination = @destiny
                                AND entity = @entity",
                        new { entity, destiny });

                if (dto == null)
                {
                    cacheEntry.Dispose();
                    return Maybe.None;
                }

                IEnumerable<RoutingRule> rules = [];
                if (!string.IsNullOrEmpty(dto.RoutingRule))
                    rules = dto.RoutingRule.ToNameTypeObject<IEnumerable<RoutingRule>>();

                return Maybe<Route>.From(
                    new Route(
                        dto.Id,
                        dto.Origin,
                        dto.Destination,
                        dto.Entity,
                        dto.TopicName,
                        dto.CallbackRequired,
                        dto.Uri,
                        dto.ContentType,
                        dto.MaxRetry,
                        dto.TimeOut,
                        dto.ClassName,
                        (EMode)dto.Mode,
                        ResolveAuthentication(dto.Destination),
                        dto.CallbackErrorUri,
                        dto.SendPayloadAsLink,
                        rules));
            });
        return cachedValue;
    }

    public async Task<Maybe<Route>> GetRoute(long id)
    {
        var key = $"route:{id}";
        var cachedValue = await memoryCache.GetOrCreateAsync(
            key,
            async cacheEntry =>
            {
                cacheEntry.SlidingExpiration = TimeSpan.FromSeconds(3);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
                cacheEntry.Size = 500;
                await using var connection = dataSource.CreateConnection();
                var dto = await connection
                    // PROBLEMA: consulta sem token. Não é cancelada no shutdown nem no timeout.
                    .QueryFirstOrDefaultAsync<RouteDto>(
                        @"SELECT id,
                                    origin,
                                    destination,
                                    entity,
                                    topic_name AS TopicName,
                                    callback_required AS CallbackRequired,
                                    uri,
                                    content_type AS ContentType,
                                    max_retry AS MaxRetry,
                                    time_out AS TimeOut,
                                    classname,
                                    mode AS Mode,
                                    callback_error_uri AS CallbackErrorUri,
                                    send_payload_as_link AS SendPayloadAsLink,
                                    routing_rule AS RoutingRule
                            FROM destination_routes
                            WHERE id = @id",
                        new { id });

                if (dto == null)
                {
                    cacheEntry.Dispose();
                    return Maybe.None;
                }

                IEnumerable<RoutingRule> rules = [];
                if (!string.IsNullOrEmpty(dto.RoutingRule))
                    rules = dto.RoutingRule.ToNameTypeObject<IEnumerable<RoutingRule>>();

                return Maybe<Route>.From(
                    new Route(
                        dto.Id,
                        dto.Origin,
                        dto.Destination,
                        dto.Entity,
                        dto.TopicName,
                        dto.CallbackRequired,
                        dto.Uri,
                        dto.ContentType,
                        dto.MaxRetry,
                        dto.TimeOut,
                        dto.ClassName,
                        (EMode)dto.Mode,
                        ResolveAuthentication(dto.Destination),
                        dto.CallbackErrorUri,
                        dto.SendPayloadAsLink,
                        rules));
            });
        return cachedValue;
    }

    public async Task<Maybe<Route>> GetRouteWithoutRules(long id)
    {
        var key = $"route:{id}";
        var cachedValue = await memoryCache.GetOrCreateAsync(
            key,
            async cacheEntry =>
            {
                cacheEntry.SlidingExpiration = TimeSpan.FromSeconds(3);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
                cacheEntry.Size = 500;
                await using var connection = dataSource.CreateConnection();
                var dto = await connection
                    // PROBLEMA: consulta sem token. Não é cancelada no shutdown nem no timeout.
                    .QueryFirstOrDefaultAsync<RouteWithoutRulesDto>(
                        @"SELECT id,
                                    origin,
                                    destination,
                                    entity,
                                    topic_name AS TopicName,
                                    callback_required AS CallbackRequired,
                                    uri,
                                    content_type AS ContentType,
                                    max_retry AS MaxRetry,
                                    time_out AS TimeOut,
                                    classname,
                                    mode AS Mode,
                                    callback_error_uri AS CallbackErrorUri,
                                    send_payload_as_link AS SendPayloadAsLink
                            FROM destination_routes
                            WHERE id = @id",
                        new { id });

                if (dto == null)
                {
                    cacheEntry.Dispose();
                    return Maybe.None;
                }

                return Maybe<Route>.From(
                    new Route(
                        dto.Id,
                        dto.Origin,
                        dto.Destination,
                        dto.Entity,
                        dto.TopicName,
                        dto.CallbackRequired,
                        dto.Uri,
                        dto.ContentType,
                        dto.MaxRetry,
                        dto.TimeOut,
                        dto.ClassName,
                        (EMode)dto.Mode,
                        ResolveAuthentication(dto.Destination),
                        dto.CallbackErrorUri,
                        dto.SendPayloadAsLink,
                        []));
            });
        return cachedValue;
    }

    public async Task<IEnumerable<Route>> GetRoutesByOriginAndEntityAsync(
        string origin,
        string entity,
        CancellationToken cancellationToken = default)
    {
        var key = $"routes:{origin}:{entity}";
        var cachedValue = await memoryCache.GetOrCreateAsync(
            key,
            async cacheEntry =>
            {
                cacheEntry.SlidingExpiration = TimeSpan.FromSeconds(3);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
                cacheEntry.Size = 500;

                await using var connection = dataSource.CreateConnection();

                // PROBLEMA: consulta sem token. Não é cancelada no shutdown nem no timeout.
                var results = await connection.QueryAsync<RouteDto>(
                    @"SELECT r.id, r.origin, r.destination, r.entity, r.topic_name AS TopicName,
                         r.callback_required AS CallbackRequired, r.uri, r.content_type AS ContentType,
                         r.max_retry AS MaxRetry, r.time_out AS TimeOut, r.classname, r.mode AS Mode,
                         r.callback_error_uri AS CallbackErrorUri, r.send_payload_as_link AS SendPayloadAsLink,
                         r.routing_rule AS RoutingRule
                      FROM destination_routes r
                      WHERE r.origin = @origin AND r.entity = @entity AND r.is_active = true",
                    new { origin, entity });

                return results.Select(routeDto =>
                {
                    IEnumerable<RoutingRule> routingRule = string.IsNullOrEmpty(routeDto.RoutingRule)
                        ? []
                        : routeDto.RoutingRule.ToNameTypeObject<IEnumerable<RoutingRule>>();

                    return new Route(
                        routeDto.Id,
                        routeDto.Origin,
                        routeDto.Destination,
                        routeDto.Entity,
                        routeDto.TopicName,
                        routeDto.CallbackRequired,
                        routeDto.Uri,
                        routeDto.ContentType,
                        routeDto.MaxRetry,
                        routeDto.TimeOut,
                        routeDto.ClassName,
                        (EMode)routeDto.Mode,
                        ResolveAuthentication(routeDto.Destination),
                        routeDto.CallbackErrorUri,
                        routeDto.SendPayloadAsLink,
                        routingRule);
                }).ToList();
            });
        return cachedValue ?? Enumerable.Empty<Route>();
    }

    private AuthenticationConfig ResolveAuthentication(string destination)
    {
        var entry = routeAuthenticationSettings.Value.FirstOrDefault(e => string.Equals(e.Destiny, destination, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
            return AuthenticationConfig.CreateEmpty();

        return new AuthenticationConfig
        {
            Type = entry.Type,
            Client = entry.Client,
            Secret = entry.Secret,
            Uri = entry.Uri,
            Scope = entry.Scope
        };
    }

    private sealed record RouteDto(
        long Id,
        string Origin,
        string Destination,
        string Entity,
        string TopicName,
        bool CallbackRequired,
        string Uri,
        string ContentType,
        int MaxRetry,
        int TimeOut,
        string ClassName,
        int Mode,
        string? CallbackErrorUri,
        bool SendPayloadAsLink,
        string? RoutingRule);

    private sealed record RouteWithoutRulesDto(
        long Id,
        string Origin,
        string Destination,
        string Entity,
        string TopicName,
        bool CallbackRequired,
        string Uri,
        string ContentType,
        int MaxRetry,
        int TimeOut,
        string ClassName,
        int Mode,
        string? CallbackErrorUri,
        bool SendPayloadAsLink);
}
