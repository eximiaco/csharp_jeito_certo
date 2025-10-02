using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps;

public class AddLegacyEnrollmentCompensationStep(ILogger<AddLegacyEnrollmentCompensationStep> logger) : StepBodyAsync
{
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.Workflow.Data as NewEnrollmentFlowData;
        
        logger.LogWarning("Executando compensação para AddLegacyEnrollmentStep. LegacyEnrollmentId: {LegacyEnrollmentId}", 
            data!.LegacyEnrollmentId);
        
        // TODO: Implementar lógica de compensação para remover matrícula do sistema legado
        // Por enquanto apenas logamos a necessidade de compensação
        
        logger.LogInformation("Compensação AddLegacyEnrollmentStep executada com sucesso");
        
        return ExecutionResult.Next();
    }
}
