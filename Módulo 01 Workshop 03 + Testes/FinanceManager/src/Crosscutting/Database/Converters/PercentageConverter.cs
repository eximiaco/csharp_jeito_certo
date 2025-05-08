using System;
using FinanceManager.Crosscutting.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FinanceManager.Crosscutting.Database.Converters;

public class PercentageConverter : ValueConverter<Percentage, decimal>
{
    public PercentageConverter() : base(
        v => v.Value,
        v => Percentage.Create(v).Value
    )
    {
    }
}
