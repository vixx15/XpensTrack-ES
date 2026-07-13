using Marten.Events.Projections;
using TransactionApi.Domain.Events;
using TransactionApi.Infrastructure.Extensions;

namespace TransactionApi.Projections;

public class MonthlyReportProjection : MultiStreamProjection<MonthlyReport, string>
{
    public MonthlyReportProjection()
    {
        Identity<TransactionCreated>(identityFunc: e => $"{e.OccuredAt:yyyy-MM}-{e.UserId}");
        Identity<TransactionDeleted>(identityFunc: e => $"{e.OccuredAt:yyyy-MM}-{e.UserId}");

        Identities<TransactionUpdated>(identitiesFunc: e => new[] {
            $"{e.OldOccuredAt:yyyy-MM}-{e.UserId}",
            $"{e.NewOccurredAt:yyyy-MM}-{e.UserId}"
        }.Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList());
    }
    
    public MonthlyReport Create(TransactionUpdated @event)
    {
        var monthStart = new DateTimeOffset(
            year: @event.NewOccurredAt.Year,
            month: @event.NewOccurredAt.Month,
            day: 1,
            hour: 0, minute: 0, second: 0,
            offset: @event.NewOccurredAt.Offset);

        var reportId = $"{@event.NewOccurredAt:yyyy-MM}-{@event.UserId}";
        var report = new MonthlyReport(
            Id: reportId,
            MonthStart: monthStart,
            DefaultCurrencyCode: @event.NewDefaultCurrencyAmount.CurrencyCode)
        {
            WeeklyBreakdown = MonthlyReport.GenerateWeeklySummaries(monthStart, @event.NewDefaultCurrencyAmount.CurrencyCode)
        };

        report.ApplyTransaction(
            type: @event.NewTransactionType,
            categoryId: (int?)@event.NewTransactionCategory,
            defaultCurrencyAmount: @event.NewDefaultCurrencyAmount,
            weekOfMonth: @event.NewOccurredAt.GetWeekOfMonth(),
            weekStart: @event.NewOccurredAt.StartOfWeek());

        return report;
    }

    public MonthlyReport Create(TransactionCreated @event)
    {
        var reportId = $"{@event.OccuredAt:yyyy-MM}-{@event.UserId}";
        var monthStart = new DateTimeOffset(
            year: @event.OccuredAt.Year,
            month: @event.OccuredAt.Month,
            day: 1,
            hour: 0,
            minute: 0,
            second: 0,
            offset: @event.OccuredAt.Offset);

        var report = new MonthlyReport(
            Id: reportId,
            MonthStart: monthStart,
            DefaultCurrencyCode: @event.DefaultCurrencyAmount.CurrencyCode
        ) {
            WeeklyBreakdown =
                MonthlyReport.GenerateWeeklySummaries(monthStart, @event.DefaultCurrencyAmount.CurrencyCode)
        };

        report.ApplyTransaction(type: @event.TransactionType,
            categoryId: (int?)@event.TransactionCategory,
            defaultCurrencyAmount: @event.DefaultCurrencyAmount,
            weekOfMonth: @event.OccuredAt.GetWeekOfMonth(),
            weekStart: @event.OccuredAt.StartOfWeek());

        return report;
    }

    public void Apply(MonthlyReport current, TransactionCreated e)
    {
        current.ApplyTransaction(
            type: e.TransactionType,
            categoryId: (int?)e.TransactionCategory,
            defaultCurrencyAmount: e.DefaultCurrencyAmount,
            weekOfMonth: e.OccuredAt.GetWeekOfMonth(),
            weekStart: e.OccuredAt.StartOfWeek()
        );
    }


    public void Apply(MonthlyReport current, TransactionUpdated e)
    {
        var oldMonthId = $"{e.OldOccuredAt:yyyy-MM}-{e.UserId}";
        var newMonthId = $"{e.NewOccurredAt:yyyy-MM}-{e.UserId}";

        if (oldMonthId == newMonthId)
        {
            current.RevertTransaction(
                type: e.OldTransactionType,
                categoryId: (int?)e.OldTransactionCategory,
                defaultCurrencyAmount: e.OldDefaultCurrencyAmount,
                weekOfMonth: e.OldOccuredAt.GetWeekOfMonth());
            current.ApplyTransaction(
                type: e.NewTransactionType,
                categoryId: (int?)e.NewTransactionCategory,
                defaultCurrencyAmount: e.NewDefaultCurrencyAmount,
                weekOfMonth: e.NewOccurredAt.GetWeekOfMonth(),
                weekStart: e.NewOccurredAt.StartOfWeek());
        }
        else
        {
            if (current.Id == oldMonthId)
            {
                current.RevertTransaction(
                    type: e.OldTransactionType,
                categoryId: (int?)e.OldTransactionCategory,
                    defaultCurrencyAmount: e.OldDefaultCurrencyAmount,
                    weekOfMonth: e.OldOccuredAt.GetWeekOfMonth());
            }
            else if (current.Id == newMonthId)
            {
                current.ApplyTransaction(
                    type: e.NewTransactionType,
                categoryId: (int?)e.NewTransactionCategory,
                    defaultCurrencyAmount: e.NewDefaultCurrencyAmount,
                    weekOfMonth: e.NewOccurredAt.GetWeekOfMonth(),
                    weekStart: e.NewOccurredAt.StartOfWeek());
            }
        }
    }

    public void Apply(MonthlyReport current, TransactionDeleted @event)
    {
        current.RevertTransaction(
            @event.TransactionType,
            (int?)@event.TransactionCategory,
            @event.DefaultCurrencyAmount,
            @event.OccuredAt.GetWeekOfMonth());
    }
}