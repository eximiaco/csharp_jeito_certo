using System;
using FinanceManager.BankAccounts;
using FinanceManager.Crosscutting.Database.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Crosscutting.Database.Configurations;

public class BudgetAlertConfiguration : IEntityTypeConfiguration<BudgetAlert>
{
    public void Configure(EntityTypeBuilder<BudgetAlert> builder)
    {
        builder.ToTable("BudgetAlerts");

        builder.ComplexProperty(e => e.MaxAllowedValue);
    }
}
