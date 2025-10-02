using CSharpFunctionalExtensions;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;

namespace GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;

public class LegacyAdapter
{
    private readonly LegacyApiConfiguration _configuration;

    public LegacyAdapter(IOptions<LegacyApiConfiguration> configuration)
    {
        _configuration = configuration.Value;
    }

    public async Task<Result<Guid>> ProcessEnrollmentAsync(Request request)
    {
        try
        {
            var legacyRequest = new LegacyEnrollmentRequest
            {
                Student = new LegacyStudentDto
                {
                    Name = request.Student.Name,
                    Email = request.Student.Email,
                    Phone = request.Student.Phone,
                    Document = request.Student.Document,
                    BirthDate = request.Student.BirthDate,
                    Gender = request.Student.Gender,
                    Address = request.Student.Address
                },
                PlanId = request.PlanId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                PhysicalAssessment = new LegacyPhysicalAssessmentDto
                {
                    PersonalId = request.PhysicalAssessment.PersonalId,
                    AssessmentDate = request.PhysicalAssessment.AssessmentDate,
                    Weight = request.PhysicalAssessment.Weight,
                    Height = request.PhysicalAssessment.Height,
                    BodyFatPercentage = request.PhysicalAssessment.BodyFatPercentage,
                    Notes = request.PhysicalAssessment.Notes
                }
            };

            var response = await _configuration.BaseUrl
                .AppendPathSegment("api/enrollment/enroll")
                .WithTimeout(_configuration.TimeoutSeconds)
                .PostJsonAsync(legacyRequest)
                .ReceiveJson<LegacyEnrollmentResponse>();

            return Result.Success(response.EnrollmentId);
        }
        catch (FlurlHttpException ex)
        {
            var errorMessage = await ex.GetResponseStringAsync();
            return Result.Failure<Guid>($"Erro ao processar inscrição no sistema legado: {errorMessage}");
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>($"Erro inesperado ao processar inscrição: {ex.Message}");
        }
    }

    // TODO: Implementar retry policy quando Polly.Extensions.Http estiver disponível
    // Por enquanto, usando apenas timeout configurável
}

// DTOs para comunicação com o sistema legado
public record LegacyEnrollmentRequest
{
    public LegacyStudentDto Student { get; set; } = new();
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public LegacyPhysicalAssessmentDto PhysicalAssessment { get; set; } = new();
}

public record LegacyStudentDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public record LegacyPhysicalAssessmentDto
{
    public Guid PersonalId { get; set; }
    public DateTime AssessmentDate { get; set; }
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public decimal BodyFatPercentage { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public record LegacyEnrollmentResponse
{
    public Guid EnrollmentId { get; set; }
}
