using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace GymErp.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainer _postgresContainer;
    protected readonly DbContext _dbContext;

    protected IntegrationTestBase()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithDatabase("gym_erp_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await SetupDatabase();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    protected abstract Task SetupDatabase();
} 