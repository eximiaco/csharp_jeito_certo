using CSharpFunctionalExtensions;
using GymErp.Domain.Subscriptions.Aggreates.Plans;

namespace GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;

public class Handler(LegacyAdapter legacyAdapter, ModernizedAdapter modernizedAdapter, PlanService planService)
{
    public async Task<Result<Response>> HandleAsync(Request request)
    {
        // Buscar informações do plano para decidir qual sistema usar
        var planResult = await planService.GetPlanByIdAsync(request.PlanId);
        if (planResult.IsFailure)
        {
            return Result.Failure<Response>(planResult.Error);
        }

        var plan = planResult.Value;
        
        // Usar sistema legado apenas para planos mensais
        var useLegacySystem = plan.Type == PlanType.Mensal;

        if (useLegacySystem)
        {
            var result = await legacyAdapter.ProcessEnrollmentAsync(request);
            if (result.IsFailure)
            {
                return Result.Failure<Response>(result.Error);
            }

            return Result.Success(new Response(result.Value, "Legacy"));
        }
        else
        {
            var result = await modernizedAdapter.ProcessEnrollmentAsync(request);
            if (result.IsFailure)
            {
                return Result.Failure<Response>(result.Error);
            }

            return Result.Success(new Response(result.Value, "Modernized"));
        }
    }
}
