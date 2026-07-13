namespace TransactionApi.Infrastructure.Extensions;

public static class DateTimeOffsetExtensions
{
    public static int GetWeekOfMonth(this DateTimeOffset date)
    {
        var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        var currentDay = 1;
        var weekNum = 1;

        while (currentDay <= daysInMonth)
        {
            var currentDate = new DateTimeOffset(date.Year, date.Month, currentDay, 0, 0, 0, date.Offset);
            var daysUntilSunday = (7 - (int)currentDate.DayOfWeek) % 7;
            var weekEndDay = Math.Min(currentDay + daysUntilSunday, daysInMonth);

            if (date.Day <= weekEndDay)
                return weekNum;

            currentDay = weekEndDay + 1;
            weekNum++;
        }

        return 1;
    }

    public static DateTimeOffset StartOfWeek(this DateTimeOffset dt)
    {
        var dayStart = new DateTimeOffset(
            year: dt.Year,
            month: dt.Month,
            day: dt.Day,
            hour: 0,
            minute: 0,
            second: 0,
            offset: dt.Offset);
        var dayOfWeek = (int)dt.DayOfWeek - 1;
        if (dayOfWeek < 0)
        {
            dayOfWeek = 6;
        }

        return dayStart.AddDays(-dayOfWeek);
    }

    public static DateTimeOffset EndOfMonth(this DateTimeOffset dt)
    {
        int daysInMonth = DateTime.DaysInMonth(dt.Year, dt.Month);

        return new DateTimeOffset(
            dt.Year,
            dt.Month,
            daysInMonth,
            23, 59, 59,
            dt.Offset
        );
    }

    public static DateTimeOffset EndOfWeekInMonth(this DateTimeOffset dt)
    {
        var dayEnd = new DateTimeOffset(
            year: dt.Year,
            month: dt.Month,
            day: dt.Day,
            hour: 23,
            minute: 59,
            second: 59,
            offset: dt.Offset);
        var dayOfWeek = (int)dt.DayOfWeek - 1;
        if (dayOfWeek < 0)
        {
            dayOfWeek = 6;
        }

        dayEnd = dayEnd.AddDays(6 - dayOfWeek);

        if (dayEnd.Month != dt.Month)
        {
            return dt.EndOfMonth();
        }
        else
        {
            return dayEnd;
        }
    }
}