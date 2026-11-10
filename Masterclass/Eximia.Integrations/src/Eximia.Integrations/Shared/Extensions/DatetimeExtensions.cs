namespace Eximia.Integrations.Shared.Extensions;

public static class DatetimeExtensions
{
    public static DateTime? SpecifyKindUtc(this DateTime? value)
    {
        return value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
    }

    public static DateTime SpecifyKindUtc(this DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public static DateTime ConvertFromUnixTime(this long value)
    {
        var start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return start.AddMilliseconds(value);
    }

    public static long ConvertToUnixTime(this DateTime value)
    {
        return ((long)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds);
    }
}
