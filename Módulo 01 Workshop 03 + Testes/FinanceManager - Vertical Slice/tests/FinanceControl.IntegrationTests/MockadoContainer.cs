using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace FinanceControl.IntegrationTests
{
    public class MockacoContainer : IAsyncLifetime
    {
        private readonly IContainer _container;

        public int ContainerPort { get; }

        public string ContainerUri => $"http://localhost:{ContainerPort}";

        public MockacoContainer()
        {
            ContainerPort = Random.Shared.Next(4000, 5000);

            _container = new ContainerBuilder()
                .WithImage("natenho/mockaco")
                .WithCleanUp(true)
                .WithPortBinding(ContainerPort, 5000)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilPortIsAvailable(5000)
                    .AddCustomWaitStrategy(new MockacoContainerHealthCheck(ContainerUri))
                )
                .WithBindMount(ToAbsolute("./HttpMocks"), "/app/Mocks", AccessMode.ReadWrite)
                .Build();

        }

        public async Task InitializeAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            await _container.StartAsync(cts.Token);
        }

        public async Task DisposeAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            await _container.StopAsync(cts.Token);
        }

        private static string ToAbsolute(string path) => Path.GetFullPath(path);
    }

    public class MockacoContainerHealthCheck(string endpoint) : IWaitUntil
    {
        private readonly string _endpoint = endpoint;

        public async Task<bool> UntilAsync(IContainer container)
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(_endpoint) };
            var result = string.Empty;
            try
            {
                result = await httpClient.GetStringAsync("/_mockaco/ready");

                return result?.Equals("Healthy") ?? false;
            }
            catch
            {
                return false;
            }
        }
    }

}
