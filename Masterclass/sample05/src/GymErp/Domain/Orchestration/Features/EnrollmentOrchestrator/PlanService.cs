using CSharpFunctionalExtensions;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using GymErp.Domain.Subscriptions.Aggreates.Plans;

namespace GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;

public class PlanService
{
    private readonly SubscriptionsApiConfiguration _configuration;

    public PlanService(IOptions<SubscriptionsApiConfiguration> configuration)
    {
        _configuration = configuration.Value;
    }

    public async Task<Result<PlanInfo>> GetPlanByIdAsync(Guid planId)
    {
        try
        {
            var response = await _configuration.BaseUrl
                .AppendPathSegment($"api/plans/{planId}")
                .WithTimeout(_configuration.TimeoutSeconds)
                .GetJsonAsync<PlanInfo>();

            return Result.Success(response);
        }
        catch (FlurlHttpException ex)
        {
            if (ex.StatusCode == 404)
            {
                return Result.Failure<PlanInfo>($"Plano com ID {planId} não encontrado");
            }

            var errorMessage = await ex.GetResponseStringAsync();
            return Result.Failure<PlanInfo>($"Erro ao buscar informações do plano: {errorMessage}");
        }
        catch (Exception ex)
        {
            return Result.Failure<PlanInfo>($"Erro inesperado ao buscar informações do plano: {ex.Message}");
        }
    }
}

public record PlanInfo(Guid Id, string Description, PlanType Type);
