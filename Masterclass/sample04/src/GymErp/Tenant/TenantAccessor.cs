namespace GymErp.Tenant;

public class TenantAccessor : IDisposable
{
    public Common.GymErpTenant GymErpTenant { get; private set; }

    public void Register(Common.GymErpTenant gymErpTenant)
    {
        GymErpTenant = gymErpTenant;
    }

    public void Dispose()
    {
        GymErpTenant = null!;
    }
}