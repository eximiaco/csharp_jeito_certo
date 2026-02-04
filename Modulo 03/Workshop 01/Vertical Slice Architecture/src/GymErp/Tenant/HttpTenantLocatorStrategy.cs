using System.Security.Authentication;
using Flurl;
using Flurl.Http;
using GymErp.Common;

namespace GymErp.Tenant;

public record GetTenantResponse(Guid Id, string Name, string ConnectionString);

public class HttpTenantLocatorStrategy() : ITenantLocator
{
    // TODO: Adicionar a URL do serviço de tenants e autenticação
    private const string TENANT_SERVICE_URL = "https://localhost:7207";
    private const string JWT_BEARER = "<TOKEN>";

    public async Task<Common.GymErpTenant> Get(string name, CancellationToken cancellationToken)
    {
        var response = await $"{TENANT_SERVICE_URL}"
           .AppendPathSegment("tenants")
           .AppendPathSegment(name)
           .WithOAuthBearerToken(JWT_BEARER)
           .AllowHttpStatus("404")
           .GetJsonAsync<GetTenantResponse>(cancellationToken: cancellationToken);

        if (response == null)
            throw new AuthenticationException($"Tenant not found [{response}]");

        return new Common.GymErpTenant(response.Id, response.Name, response.ConnectionString);
    }

    public async Task<IEnumerable<Common.GymErpTenant>> GetAll(CancellationToken cancellationToken)
    {
        var response = await $"{TENANT_SERVICE_URL}"
            .AppendPathSegment("tenants")
            .WithOAuthBearerToken(JWT_BEARER)
            .GetJsonAsync<IEnumerable<GetTenantResponse>>(cancellationToken: cancellationToken);

        return response.Select(t => new Common.GymErpTenant(t.Id, t.Name, t.ConnectionString)); ;
    }
}