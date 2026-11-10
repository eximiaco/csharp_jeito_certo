namespace GymErp.Application.UseCases.AddNewEnrollment;

public record AddNewEnrollmentRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
}
