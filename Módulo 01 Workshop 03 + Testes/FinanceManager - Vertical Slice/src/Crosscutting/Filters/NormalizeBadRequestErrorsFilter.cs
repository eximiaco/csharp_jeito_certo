using Microsoft.AspNetCore.Http.HttpResults;

namespace FinanceManager.Crosscutting.Filters;

public class NormalizeBadRequestErrorsFilter: IEndpointFilter  
{  
    public async ValueTask<object?> InvokeAsync(  
        EndpointFilterInvocationContext context,  
        EndpointFilterDelegate next)  
    {
        var result = await next(context);

        return result is BadRequest<ErrorMessage> badRequest
            ? BadRequest(new [] { badRequest.Value })
            : result;
    }
}
