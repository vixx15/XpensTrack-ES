using TransactionApi.Domain;
using TransactionApi.Infrastructure.Localization;

namespace TransactionApi.Projections;

public class MonthlyReportMapper(TransactionDisplayNames displayNames)
{
    public MonthlyReportResponse ToResponse(MonthlyReport report)
    {
        var categoryBreakdown = report.CategoryBreakdown
            .ToDictionary(
                typeEntry => typeEntry.Key,
                typeEntry => typeEntry.Value.ToDictionary(
                    catEntry => catEntry.Key,
                    catEntry => new CategoryBreakdownItem(
                        Name: displayNames.GetCategoryName((TransactionCategory)catEntry.Key),
                        Amount: catEntry.Value)));

        var weeklyBreakdown = report.WeeklyBreakdown
            .ToDictionary(
                weekEntry => weekEntry.Key,
                weekEntry => new WeeklySummaryResponse(
                    StartDate: weekEntry.Value.StartDate,
                    EndDate: weekEntry.Value.EndDate,
                    DefaultCurrencyCode: weekEntry.Value.DefaultCurrencyCode,
                    Totals: weekEntry.Value.Totals,
                    Balance: weekEntry.Value.Balance));

        return new MonthlyReportResponse(
            Id: report.Id,
            MonthStart: report.MonthStart,
            DefaultCurrencyCode: report.DefaultCurrencyCode,
            Balance: report.Balance,
            NumberOfTransactions: report.NumberOfTransactions,
            Totals: report.Totals,
            CategoryBreakdown: categoryBreakdown,
            WeeklyBreakdown: weeklyBreakdown);
    }
}
