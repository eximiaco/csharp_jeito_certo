using CSharpFunctionalExtensions;

namespace GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;

public class Handler
{
    private readonly LegacyAdapter _legacyAdapter;
    private readonly ModernizedAdapter _modernizedAdapter;

    public Handler(LegacyAdapter legacyAdapter, ModernizedAdapter modernizedAdapter)
    {
        _legacyAdapter = legacyAdapter;
        _modernizedAdapter = modernizedAdapter;
    }

    public async Task<Result<Response>> HandleAsync(Request request)
    {
        // TODO: Implementar lógica de decisão baseada em critérios futuros
        // Por enquanto, sempre usa o sistema legado
        var useLegacySystem = true;

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
