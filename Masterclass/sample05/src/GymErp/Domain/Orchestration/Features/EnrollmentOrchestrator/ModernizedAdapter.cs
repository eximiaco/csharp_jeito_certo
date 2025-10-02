using CSharpFunctionalExtensions;
using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;
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
            // Criar dados para o workflow NewEnrollmentFlow
            var workflowData = new NewEnrollmentFlowData
            {
                ClientId = request.ClientId,
                PlanId = request.PlanId,
                EnrollmentId = Guid.NewGuid(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                
                // Dados do estudante
                Name = request.Student.Name,
                Email = request.Student.Email,
                Phone = request.Student.Phone,
                Document = request.Student.Document,
                BirthDate = request.Student.BirthDate,
                Gender = request.Student.Gender,
                Address = request.Student.Address,
                
                // Dados da avaliação física
                PersonalId = request.PhysicalAssessment.PersonalId,
                AssessmentDate = request.PhysicalAssessment.AssessmentDate,
                Weight = request.PhysicalAssessment.Weight,
                Height = request.PhysicalAssessment.Height,
                BodyFatPercentage = request.PhysicalAssessment.BodyFatPercentage,
                Notes = request.PhysicalAssessment.Notes
            };

            // Iniciar o workflow NewEnrollmentFlow
            var workflowId = await _workflowHost.StartWorkflow("new-enrollment-workflow", workflowData);
            
            if (string.IsNullOrEmpty(workflowId))
            {
                return Result.Failure<Guid>("Falha ao iniciar workflow de inscrição no sistema modernizado");
            }

            // Retornar o ID da inscrição (que será usado para acompanhar o progresso)
            return Result.Success(workflowData.EnrollmentId);
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>($"Erro ao processar inscrição no sistema modernizado: {ex.Message}");
        }
    }
}
