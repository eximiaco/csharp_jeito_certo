using System.Security.Claims;
using FinanceManager.BankAccounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Crosscutting.Database.Configurations;

public class BankAccountConfiguration() : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.HasMany(b => b.Transactions)
            .WithOne();

        builder.HasMany(b => b.BudgetAlerts)
            .WithOne();

        builder.ComplexProperty(e => e.Balance);
    }
}
