using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RabbitMQ.Client;
using Testcontainers.MsSql;

namespace FinanceControl.IntegrationTests
{
    public class FinanceManagerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
            .Build();

        private readonly MockacoContainer _httpMockContainer = new();

        public IChannel ChannelMock { get; private set; } = Substitute.For<IChannel>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                var connection = Substitute.For<IConnection>();
                ChannelMock = Substitute.For<IChannel>();
                connection.CreateChannelAsync().Returns(ChannelMock);

                services.AddTransient<IAuthenticationSchemeProvider, MockSchemeProvider>();
                services.AddSingleton(connection);
            });
        }

        public async Task InitializeAsync()
        {
            await _sqlContainer.StartAsync();
            await _httpMockContainer.InitializeAsync();

            Environment.SetEnvironmentVariable("ConnectionStrings:Database", _sqlContainer.GetConnectionString());
            Environment.SetEnvironmentVariable("ExchangeService:ApiUrl", _httpMockContainer.ContainerUri);
        }

        public async Task SeedDatabaseAsync()
        {
            var file = File.ReadAllText("seed.sql");
            await _sqlContainer.ExecScriptAsync(file);
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _sqlContainer.StopAsync();
            await _httpMockContainer.DisposeAsync();
        }
    }
}
