using GymErp.Domain.Subscriptions.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace GymErp.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainer _postgresContainer;
    protected SubscriptionsDbContext _dbContext = null!;

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
        var options = new DbContextOptionsBuilder<SubscriptionsDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
        _dbContext = new SubscriptionsDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        await SetupDatabase();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _postgresContainer.DisposeAsync();
    }

    protected virtual Task SetupDatabase() => Task.CompletedTask;
} 