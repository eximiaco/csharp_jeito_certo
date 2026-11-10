using GymErp.Application.UseCases;
using GymErp.Application.UseCases.AddNewEnrollment;
using GymErp.Application.UseCases.CancelEnrollment;
using GymErp.Application.UseCases.SuspendEnrollment;
using GymErp.Domain.Enrollments;

namespace GymErp.Tests.UseCases;

// Os casos de uso são exercitados sem HTTP e sem banco: só o círculo de dentro e dublês das abstrações.
public class EnrollmentUseCasesTests
{
    private readonly InMemoryEnrollmentRepository _repository = new();
    private readonly CountingUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task AddNewEnrollment_with_valid_data_persists_and_commits()
    {
        var useCase = new AddNewEnrollmentHandler(_repository, _unitOfWork);

        var result = await useCase.HandleAsync(new AddNewEnrollmentRequest
        {
            Name = "João da Silva Santos",
            Email = "joao.silva@email.com",
            Phone = "11999999999",
            Document = "52998224725",
            Address = "Rua Exemplo, 123"
        });

        Assert.True(result.IsSuccess);
        Assert.True(_repository.Items.ContainsKey(result.Value));
        Assert.Equal(1, _unitOfWork.Commits);
    }

    [Fact]
    public async Task AddNewEnrollment_with_invalid_document_is_a_rule_violation_and_does_not_commit()
    {
        var useCase = new AddNewEnrollmentHandler(_repository, _unitOfWork);

        var result = await useCase.HandleAsync(new AddNewEnrollmentRequest
        {
            Name = "João da Silva Santos",
            Email = "joao.silva@email.com",
            Phone = "11999999999",
            Document = "12345678900",
            Address = "Rua Exemplo, 123"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrorKind.RuleViolated, result.Error.Kind);
        Assert.Equal(0, _unitOfWork.Commits);
    }

    [Fact]
    public async Task CancelEnrollment_of_unknown_id_is_not_found()
    {
        var useCase = new CancelEnrollmentHandler(_repository, _unitOfWork);

        var result = await useCase.HandleAsync(new CancelEnrollmentRequest { EnrollmentId = Guid.NewGuid(), Reason = "x" });

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrorKind.NotFound, result.Error.Kind);
    }

    [Fact]
    public async Task CancelEnrollment_records_the_reason_in_the_domain_event()
    {
        var enrollment = SampleData.NewEnrollment();
        await _repository.AddAsync(enrollment);
        var useCase = new CancelEnrollmentHandler(_repository, _unitOfWork);

        var result = await useCase.HandleAsync(new CancelEnrollmentRequest { EnrollmentId = enrollment.Id, Reason = "Mudou de cidade" });

        Assert.True(result.IsSuccess);
        Assert.Equal(EState.Canceled, enrollment.State);
        var canceled = Assert.Single(enrollment.DomainEvents.OfType<EnrollmentCanceledEvent>());
        Assert.Equal("Mudou de cidade", canceled.Reason);
    }

    [Fact]
    public async Task CancelEnrollment_twice_is_a_rule_violation()
    {
        var enrollment = SampleData.NewEnrollment();
        await _repository.AddAsync(enrollment);
        var useCase = new CancelEnrollmentHandler(_repository, _unitOfWork);
        await useCase.HandleAsync(new CancelEnrollmentRequest { EnrollmentId = enrollment.Id, Reason = "primeira" });

        var second = await useCase.HandleAsync(new CancelEnrollmentRequest { EnrollmentId = enrollment.Id, Reason = "segunda" });

        Assert.True(second.IsFailure);
        Assert.Equal(EnrollmentErrorKind.RuleViolated, second.Error.Kind);
    }

    [Fact]
    public async Task SuspendEnrollment_shorter_than_30_days_is_a_rule_violation_from_the_entity()
    {
        var enrollment = SampleData.NewEnrollment();
        enrollment.Activate();
        await _repository.AddAsync(enrollment);
        var useCase = new SuspendEnrollmentHandler(_repository, _unitOfWork);

        var result = await useCase.HandleAsync(new SuspendEnrollmentCommand(enrollment.Id, DateTime.UtcNow, DateTime.UtcNow.AddDays(10)));

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrorKind.RuleViolated, result.Error.Kind);
        Assert.Equal(EState.Active, enrollment.State);
    }
}
