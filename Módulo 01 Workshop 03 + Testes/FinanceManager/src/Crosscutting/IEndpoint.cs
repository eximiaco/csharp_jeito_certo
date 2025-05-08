using System;

namespace FinanceManager.Crosscutting;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
