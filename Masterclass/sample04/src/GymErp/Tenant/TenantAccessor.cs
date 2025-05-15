namespace GymErp.Tenant;

public class TenantAccessor : IDisposable
{
    public Common.GymErpTenant Tenant { get; private set; }

    public void Register(Common.GymErpTenant gymErpTenant)
    {
        Tenant = gymErpTenant;
    }

    public void Dispose()
    {
        Tenant = null!;
    }
}