using GymErp.Adapters.InMemory;
using GymErp.Adapters.NoOpNotifications;
using GymErp.Core;
using GymErp.Core.Ports.Driven;
using GymErp.Core.Ports.Driving;
using GymErp.Domain.Enrollments;

namespace GymErp.Tests;

// O teste conduz a aplicação pela mesma porta que o controller HTTP usa.
// É a intenção original de Cockburn: a aplicação "equally driven by users, programs, automated test or batch scripts".
public class ManagingEnrollmentsTests
{
    private readonly RecordingNotifier _notifier = new();
    private readonly IForManagingEnrollments _port;

    public ManagingEnrollmentsTests() =>
        _port = new EnrollmentManagement(new InMemoryEnrollments(_notifier));

    private static readonly EnrollRequest ValidEnrollment =
        new("João da Silva Santos", "joao.silva@email.com", "11999999999", "52998224725", "Rua Exemplo, 123");

    [Fact]
    public async Task Enroll_returns_the_new_id_and_notifies_creation()
    {
        var result = await _port.EnrollAsync(ValidEnrollment);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(_notifier.Events.OfType<EnrollmentCreatedEvent>());
        Assert.Equal(result.Value, created.EnrollmentId);
    }

    [Fact]
    public async Task Enroll_with_invalid_document_is_a_rule_violation()
    {
        var result = await _port.EnrollAsync(ValidEnrollment with { Document = "12345678900" });

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrorKind.RuleViolated, result.Error.Kind);
    }

    [Fact]
    public async Task Cancel_unknown_enrollment_is_not_found()
    {
        var result = await _port.CancelAsync(new CancelRequest(Guid.NewGuid(), "x"));

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrorKind.NotFound, result.Error.Kind);
    }

    [Fact]
    public async Task Cancel_notifies_with_the_reason_and_a_second_cancel_is_a_rule_violation()
    {
        var id = (await _port.EnrollAsync(ValidEnrollment)).Value;

        var first = await _port.CancelAsync(new CancelRequest(id, "Mudou de cidade"));
        var second = await _port.CancelAsync(new CancelRequest(id, "De novo"));

        Assert.True(first.IsSuccess);
        Assert.Equal("Mudou de cidade", Assert.Single(_notifier.Events.OfType<EnrollmentCanceledEvent>()).Reason);
        Assert.Equal(EnrollmentErrorKind.RuleViolated, second.Error.Kind);
    }

    [Fact]
    public async Task Suspend_shorter_than_30_days_is_a_rule_violation()
    {
        var id = (await _port.EnrollAsync(ValidEnrollment)).Value;

        var result = await _port.SuspendAsync(new SuspendRequest(id, DateTime.UtcNow, DateTime.UtcNow.AddDays(10)));

        Assert.True(result.IsFailure);
        Assert.Equal(EnrollmentErrorKind.RuleViolated, result.Error.Kind);
    }

    private sealed class RecordingNotifier : IForNotifyingEnrollmentEvents
    {
        public List<object> Events { get; } = [];

        public Task NotifyAsync(object domainEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
