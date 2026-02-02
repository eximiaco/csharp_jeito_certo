using System.Collections.Concurrent;
using GymErp.Common;

namespace GymErp.IntegrationTests.Infrastructure;

public sealed class FakeServiceBus : IServiceBus
{
    private readonly ConcurrentBag<object> _published = new();

    public IReadOnlyList<object> Published => _published.ToList();

    public Task PublishAsync(object message)
    {
        _published.Add(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<T> GetPublished<T>() where T : class
    {
        return _published.OfType<T>().ToList();
    }
}
