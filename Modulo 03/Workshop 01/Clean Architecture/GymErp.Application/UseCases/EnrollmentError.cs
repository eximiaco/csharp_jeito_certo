namespace GymErp.Application.UseCases;

// Erro de caso de uso tipado. A Application diz o que aconteceu;
// quem traduz para HTTP é o círculo de adaptadores (GymErp.Api/Presentation/ResultHttp.cs).
public enum EnrollmentErrorKind
{
    NotFound,
    RuleViolated
}

public sealed record EnrollmentError(EnrollmentErrorKind Kind, string Message)
{
    public static EnrollmentError NotFound(Guid enrollmentId) =>
        new(EnrollmentErrorKind.NotFound, $"Inscrição {enrollmentId} não encontrada");

    public static EnrollmentError RuleViolated(string message) =>
        new(EnrollmentErrorKind.RuleViolated, message);
}
