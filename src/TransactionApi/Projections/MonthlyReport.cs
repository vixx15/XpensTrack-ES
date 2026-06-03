using Shared;

namespace TransactionApi.Projections;

public record MonthlyReport(
    string Id,
    DateTimeOffset MonthStart,
    string DefaultCurrencyCode
)
{
    public Dictionary<TransactionType, Money> Totals { get; init; } = new();

    public Dictionary<TransactionType, Dictionary<int, Money>> CategoryBreakdown { get; init; } = new();
    public int NumberOfTransactions { get; private set; }
    public Dictionary<int, WeeklySummary> WeeklyBreakdown { get; init; } = new();

    public Money Balance =>
        Totals.GetValueOrDefault(key: TransactionType.Income,
            defaultValue: new Money(value: 0, currencyCode: DefaultCurrencyCode)) -
        Totals.GetValueOrDefault(key: TransactionType.Expense,
            defaultValue: new Money(value: 0, currencyCode: DefaultCurrencyCode));

    public void ApplyTransaction(TransactionType type, int? categoryId, Money defaultCurrencyAmount, int weekOfMonth,
        DateTimeOffset weekStart)
    {
        if (type == TransactionType.Transfer)
        {
            return;
        }

        NumberOfTransactions = NumberOfTransactions + 1;
        Totals[key: type] =
            Totals.GetValueOrDefault(key: type, defaultValue: new Money(value: 0, currencyCode: DefaultCurrencyCode)) +
            defaultCurrencyAmount;

        if (categoryId != null)
        {
            if (!CategoryBreakdown.ContainsKey(key: type))
            {
                CategoryBreakdown[key: type] = new Dictionary<int, Money>();
            }


            CategoryBreakdown[key: type][key: (int)categoryId] =
                CategoryBreakdown[key: type].GetValueOrDefault(key: (int)categoryId,
                    defaultValue: new Money(value: 0, currencyCode: DefaultCurrencyCode)) + defaultCurrencyAmount;
        }

        if (!WeeklyBreakdown.TryGetValue(key: weekOfMonth, value: out var week))
        {
            week = new WeeklySummary(StartDate: weekStart, EndDate: weekStart.AddDays(6),
                DefaultCurrencyCode: DefaultCurrencyCode);
            WeeklyBreakdown[key: weekOfMonth] = week;
        }

        week.Totals[key: type] =
            week.Totals.GetValueOrDefault(key: type,
                defaultValue: new Money(value: 0, currencyCode: DefaultCurrencyCode)) + defaultCurrencyAmount;
    }

    public void RevertTransaction(TransactionType type, int? categoryId, Money defaultCurrencyAmount, int weekOfMonth)
    {
        if (type == TransactionType.Transfer)
        {
            return;
        }

        NumberOfTransactions -= 1;
        if (Totals.ContainsKey(key: type))
        {
            Totals[key: type] = Money.Max(a: Money.Zero(currency: DefaultCurrencyCode),
                b: Totals[key: type] - defaultCurrencyAmount);
        }

        if (categoryId != null)
        {
            if (CategoryBreakdown.TryGetValue(key: type, value: out var catMap) &&
                catMap.ContainsKey(key: (int)categoryId))
            {
                catMap[key: (int)categoryId] = Money.Max(a: Money.Zero(currency: DefaultCurrencyCode),
                    b: catMap[key: (int)categoryId] - defaultCurrencyAmount);
                if (catMap[key: (int)categoryId].Value == 0)
                {
                    catMap.Remove(key: (int)categoryId);
                }
            }
        }

        if (WeeklyBreakdown.TryGetValue(key: weekOfMonth, value: out var week) && week.Totals.ContainsKey(key: type))
        {
            week.Totals[key: type] = Money.Max(a: Money.Zero(currency: DefaultCurrencyCode),
                b: week.Totals[key: type] - defaultCurrencyAmount);
        }
    }

    public static Dictionary<int, WeeklySummary> GenerateWeeklySummaries(DateTimeOffset monthStart,
        string defaultCurrencyCode)
    {
        var weeks = new Dictionary<int, WeeklySummary>();
        var monthEnd = monthStart.EndOfMonth();
        var currentStart = monthStart;
        var weekNum = 1;

        while (currentStart <= monthEnd)
        {
            var daysUntilSunday = (7 - (int)currentStart.DayOfWeek) % 7;
            var daysRemaining = (monthEnd - currentStart).Days;
            var weekEnd = currentStart.AddDays(Math.Min(daysUntilSunday, daysRemaining));

            weeks[weekNum] = new WeeklySummary(
                StartDate: new DateTimeOffset(currentStart.Year, currentStart.Month,
                    currentStart.Day, 0, 0, 0, currentStart.Offset),
                EndDate: new DateTimeOffset(weekEnd.Year, weekEnd.Month,
                    weekEnd.Day, 23, 59, 59, weekEnd.Offset),
                DefaultCurrencyCode: defaultCurrencyCode);

            currentStart = new DateTimeOffset(weekEnd.Year, weekEnd.Month,
                weekEnd.Day, 0, 0, 0, weekEnd.Offset).AddDays(1);
            weekNum++;
        }

        return weeks;
    }
}

public record WeeklySummary(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string DefaultCurrencyCode
)
{
    public Dictionary<TransactionType, Money> Totals { get; init; } = new();

    public Money Balance =>
        Totals.GetValueOrDefault(key: TransactionType.Income,
            defaultValue: new Money(value: 0, currencyCode: DefaultCurrencyCode)) -
        Totals.GetValueOrDefault(key: TransactionType.Expense,
            defaultValue: new Money(value: 0, currencyCode: DefaultCurrencyCode));
}