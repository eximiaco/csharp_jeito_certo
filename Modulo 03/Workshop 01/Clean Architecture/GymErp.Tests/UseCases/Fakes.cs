using GymErp.Application.Abstractions;
using GymErp.Domain.Enrollments;

namespace GymErp.Tests.UseCases;

internal sealed class InMemoryEnrollmentRepository : IEnrollmentRepository
{
    public Dictionary<Guid, Enrollment> Items { get; } = new();

    public Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        Items[enrollment.Id] = enrollment;
        return Task.CompletedTask;
    }

    public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.GetValueOrDefault(id));

    public Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class CountingUnitOfWork : IUnitOfWork
{
    public int Commits { get; private set; }

    public Task Commit(CancellationToken cancellationToken = default)
    {
        Commits++;
        return Task.CompletedTask;
    }
}

internal static class SampleData
{
    public static Enrollment NewEnrollment() =>
        Enrollment.Create("João da Silva Santos", "joao.silva@email.com", "11999999999", "52998224725", "Rua Exemplo, 123").Value;
}
