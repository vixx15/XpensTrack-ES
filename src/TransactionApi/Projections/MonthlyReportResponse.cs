using TransactionApi.Domain;
using TransactionApi.Domain.ValueObjects;

namespace TransactionApi.Projections;

public record MonthlyReportResponse(
    string Id,
    DateTimeOffset MonthStart,
    string DefaultCurrencyCode,
    Money Balance,
    int NumberOfTransactions,
    Dictionary<TransactionType, Money> Totals,
    Dictionary<TransactionType, Dictionary<int, CategoryBreakdownItem>> CategoryBreakdown,
    Dictionary<int, WeeklySummaryResponse> WeeklyBreakdown
);

public record CategoryBreakdownItem(string Name, Money Amount);

public record WeeklySummaryResponse(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string DefaultCurrencyCode,
    Dictionary<TransactionType, Money> Totals,
    Money Balance
);
