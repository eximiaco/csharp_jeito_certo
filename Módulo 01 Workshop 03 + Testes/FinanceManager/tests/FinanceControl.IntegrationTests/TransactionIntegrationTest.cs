using FinanceManager.BankAccounts;
using FinanceManager.BankAccounts.Endpoints;
using FinanceManager.Crosscutting.Database;
using FinanceManager.TransactionCategories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RabbitMQ.Client;
using System.Net.Http.Json;

namespace FinanceControl.IntegrationTests
{
    public class TransactionIntegrationTest(FinanceManagerWebApplicationFactory factory) : IClassFixture<FinanceManagerWebApplicationFactory>
    {
        [Fact]
        public async Task CreateTransaction_WithValidTransaction_ShouldCreateSucessfully()
        {
            var httpClient = factory.CreateClient();

            await factory.SeedDatabaseAsync();

            var categoryId = "2e484230-cdcc-4374-bf16-272075696744";
            var accountId = "ee544d49-9c9c-4930-b2c7-d500f093e909";

            var createTransactionRequest = new CreateTransactionRequest(150, "Grocery", categoryId);
            
            var response = await httpClient
                .PostAsJsonAsync($"api/v1/bank-accounts/{accountId}/transactions", createTransactionRequest);

            response.IsSuccessStatusCode.Should().BeTrue();

            var transactionResponse = await response
                .Content
                .ReadFromJsonAsync<CreateTransactionResponse>();

            transactionResponse.Id.Should().NotBeNull();
            transactionResponse.Value.Should().Be(150);
            transactionResponse.CategoryName.Should().Be("forTests");
            transactionResponse.Description.Should().Be("Grocery");

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();

            var createdTransaction = (await context
                .Set<Transaction>()
                .FirstOrDefaultAsync(t => t.Id == transactionResponse.Id))!;

            createdTransaction.Should().NotBeNull();
            createdTransaction.Value.Amount.Should().Be(150);
            createdTransaction.Value.Currency.Should().Be("BRL");
            createdTransaction.UsdValue.Amount.Should().Be(30);
            createdTransaction.UsdValue.Currency.Should().Be("USD");
            createdTransaction.Description.Should().Be("Grocery");
            createdTransaction.Type.Should().Be(TransactionType.Expense);
            createdTransaction.AccountId.Should().Be(accountId);
            createdTransaction.CategoryId.Should().Be(categoryId);
            createdTransaction.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
            createdTransaction.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);

            await factory
                .ChannelMock
                .Received()
                .BasicPublishAsync(
                    string.Empty,
                    "FinanceManager.BudgetAlerts.BudgetExceeded",
                    Arg.Any<bool>(),
                    Arg.Any<BasicProperties>(),
                    Arg.Any<ReadOnlyMemory<byte>>(),
                    Arg.Any<CancellationToken>()
                );
        }
    }
}
