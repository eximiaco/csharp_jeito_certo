namespace GymErp.Common;

internal static class DateTimeExtensions
{
    public static DateTime GoBackMonthsIfNeeded(this DateTime? value, int months)
    {
        if (value.HasValue && value.Value != default(DateTime))
            return value.Value;
        var lastSixMonth = DateTime.UtcNow.AddMonths(months*-1);
        return new DateTime(lastSixMonth.Year, lastSixMonth.Month, 1, lastSixMonth.Hour,
            lastSixMonth.Minute, lastSixMonth.Second, DateTimeKind.Utc);
    }
}