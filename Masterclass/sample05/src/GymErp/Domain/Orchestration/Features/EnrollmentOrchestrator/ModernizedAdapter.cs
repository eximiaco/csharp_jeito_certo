using CSharpFunctionalExtensions;
using WorkflowCore.Interface;

namespace GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;

public class ModernizedAdapter
{
    private readonly IWorkflowHost _workflowHost;

    public ModernizedAdapter(IWorkflowHost workflowHost)
    {
        _workflowHost = workflowHost;
    }

    public async Task<Result<Guid>> ProcessEnrollmentAsync(Request request)
    {
        try
        {
            // TODO: Implementar chamada para o workflow modernizado quando aprovado
            // Por enquanto, retorna erro indicando que não está disponível
            return Result.Failure<Guid>("Sistema modernizado ainda não está disponível para processamento de inscrições");
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>($"Erro ao processar inscrição no sistema modernizado: {ex.Message}");
        }
    }
}
