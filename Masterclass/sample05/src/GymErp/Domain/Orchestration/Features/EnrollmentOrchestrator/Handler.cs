using CSharpFunctionalExtensions;
using GymErp.Domain.Subscriptions.Aggreates.Plans;

namespace GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;

public class Handler
{
    private readonly LegacyAdapter _legacyAdapter;
    private readonly ModernizedAdapter _modernizedAdapter;
    private readonly PlanService _planService;

    public Handler(LegacyAdapter legacyAdapter, ModernizedAdapter modernizedAdapter, PlanService planService)
    {
        _legacyAdapter = legacyAdapter;
        _modernizedAdapter = modernizedAdapter;
        _planService = planService;
    }

    public async Task<Result<Response>> HandleAsync(Request request)
    {
        // Buscar informações do plano para decidir qual sistema usar
        var planResult = await _planService.GetPlanByIdAsync(request.PlanId);
        if (planResult.IsFailure)
        {
            return Result.Failure<Response>(planResult.Error);
        }

        var plan = planResult.Value;
        
        // Usar sistema legado apenas para planos mensais
        var useLegacySystem = plan.Type == PlanType.Mensal;

        if (useLegacySystem)
        {
            var result = await _legacyAdapter.ProcessEnrollmentAsync(request);
            if (result.IsFailure)
            {
                return Result.Failure<Response>(result.Error);
            }

            return Result.Success(new Response(result.Value, "Legacy"));
        }
        else
        {
            var result = await _modernizedAdapter.ProcessEnrollmentAsync(request);
            if (result.IsFailure)
            {
                return Result.Failure<Response>(result.Error);
            }

            return Result.Success(new Response(result.Value, "Modernized"));
        }
    }
}
