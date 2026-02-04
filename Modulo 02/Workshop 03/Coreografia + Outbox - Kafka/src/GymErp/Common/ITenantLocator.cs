namespace GymErp.Common;

public interface ITenantLocator
{
    Task<GymErpTenant> Get(string identifier, CancellationToken cancellationToken);
    Task<IEnumerable<GymErpTenant>> GetAll(CancellationToken cancellationToken);
}