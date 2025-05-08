using System;
using FinanceManager.Crosscutting.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FinanceManager.Crosscutting.Database.Converters;

public class MoneyConverter : ValueConverter<Money, decimal>
{
    public MoneyConverter() : base(
        v => v.Amount,
        v => Money.Create(v, "BRL").Value
    )
    {
        
    }
}
