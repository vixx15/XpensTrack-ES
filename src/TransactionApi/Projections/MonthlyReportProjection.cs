using Marten.Events.Projections;
using TransactionApi.Domain.Events;

namespace TransactionApi.Projections;

public class MonthlyReportProjection : MultiStreamProjection<MonthlyReport, string>
{
    public MonthlyReportProjection()
    {
        Identity<TransactionCreated>(identityFunc: e => $"{e.OccuredAt:yyyy-MM}-{e.UserId}");
        Identity<TransactionDeleted>(identityFunc: e => $"{e.OccuredAt:yyyy-MM}-{e.UserId}");
        /*
        Identity<TransactionAmountUpdated>(identityFunc: e => $"{e.OccuredAt:yyyy-MM}");
        FanOut<TransactionOccuredAtUpdated, string>(fanOutFunc: e => new[] {
            $"{e.PreviousOccuredAt:yyyy-MM}",
            $"{e.NewOccuredAt:yyyy-MM}"
        }.Distinct());*/

        Identities<TransactionUpdated>(identitiesFunc: e => new[] {
            $"{e.OldOccuredAt:yyyy-MM}-{e.UserId}",
            $"{e.NewOccurredAt:yyyy-MM}-{e.UserId}"
        }.Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList());
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
            categoryId: @event.TransactionCategory,
            defaultCurrencyAmount: @event.DefaultCurrencyAmount,
            weekOfMonth: @event.OccuredAt.GetWeekOfMonth(),
            weekStart: @event.OccuredAt.StartOfWeek());

        return report;
    }

    public void Apply(MonthlyReport current, TransactionCreated e)
    {
        current.ApplyTransaction(
            type: e.TransactionType,
            categoryId: e.TransactionCategory,
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
                categoryId: e.OldTransactionCategory,
                defaultCurrencyAmount: e.OldDefaultCurrencyAmount,
                weekOfMonth: e.OldOccuredAt.GetWeekOfMonth());
            current.ApplyTransaction(
                type: e.NewTransactionType,
                categoryId: e.NewTransactionCategory,
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
                    categoryId: e.OldTransactionCategory,
                    defaultCurrencyAmount: e.OldDefaultCurrencyAmount,
                    weekOfMonth: e.OldOccuredAt.GetWeekOfMonth());
            }
            else if (current.Id == newMonthId)
            {
                current.ApplyTransaction(
                    type: e.NewTransactionType,
                    categoryId: e.NewTransactionCategory,
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
            @event.TransactionCategory,
            @event.DefaultCurrencyAmount,
            @event.OccuredAt.GetWeekOfMonth());
    }

    /*  public void Apply(MonthlyReport current, TransactionAmountUpdated e)
      {
          current.RevertTransaction(type: e.TransactionType, categoryId: e.TransactionSubCategory,
              defaultCurrencyAmount: e.PreviousDefaultCurrencyAmount,
              weekOfMonth: GetWeekOfMonth(date: e.OccuredAt));

          current.ApplyTransaction(type: e.TransactionType, categoryId: e.TransactionSubCategory,
              defaultCurrencyAmount: e.NewDefaultCurrencyAmount,
              weekOfMonth: GetWeekOfMonth(date: e.OccuredAt), weekStart: StartOfWeek(dt: e.OccuredAt));
      }

      public void Apply(MonthlyReport current, TransactionTypeUpdated e)
      {
          current.RevertTransaction(type: e.PreviousTransactionType, categoryId: e.PreviousTransactionCategory,
              defaultCurrencyAmount: e.DefaultCurrencyAmount,
              weekOfMonth: GetWeekOfMonth(date: e.OccuredAt));

          current.ApplyTransaction(type: e.NewTransactionType, categoryId: e.NewTransactionCategory,
              defaultCurrencyAmount: e.DefaultCurrencyAmount,
              weekOfMonth: GetWeekOfMonth(date: e.OccuredAt), weekStart: StartOfWeek(dt: e.OccuredAt));
      }

      public void Apply(MonthlyReport current, TransactionCategoryUpdated e)
      {
          current.RevertTransaction(type: e.TransactionType, categoryId: e.PreviousTransactionCategory,
              defaultCurrencyAmount: e.DefaultCurrencyAmount,
              weekOfMonth: GetWeekOfMonth(date: e.OccuredAt));

          current.ApplyTransaction(type: e.TransactionType, categoryId: e.NewTransactionCategory,
              defaultCurrencyAmount: e.DefaultCurrencyAmount,
              weekOfMonth: GetWeekOfMonth(date: e.OccuredAt), weekStart: StartOfWeek(dt: e.OccuredAt));
      }

      public void Apply(MonthlyReport current, TransactionOccuredAtUpdated e)
      {
          var previousMonth = e.PreviousOccuredAt.Month;
          var newMonth = e.NewOccuredAt.Month;

          if (previousMonth != newMonth)
          {
              if (current.MonthStart.Month == previousMonth)
              {
                  current.RevertTransaction(type: e.TransactionType, categoryId: e.TransactionCategory,
                      defaultCurrencyAmount: e.DefaultCurrencyAmount,
                      weekOfMonth: GetWeekOfMonth(date: e.PreviousOccuredAt));
              }
              else
              {
                  current.ApplyTransaction(type: e.TransactionType, categoryId: e.TransactionCategory,
                      defaultCurrencyAmount: e.DefaultCurrencyAmount,
                      weekOfMonth: GetWeekOfMonth(date: e.NewOccuredAt), weekStart: StartOfWeek(dt: e.NewOccuredAt));
              }
          }
          else
          {
              current.RevertTransaction(type: e.TransactionType, categoryId: e.TransactionCategory,
                  defaultCurrencyAmount: e.DefaultCurrencyAmount,
                  weekOfMonth: GetWeekOfMonth(date: e.PreviousOccuredAt));

              current.ApplyTransaction(type: e.TransactionType, categoryId: e.TransactionCategory,
                  defaultCurrencyAmount: e.DefaultCurrencyAmount,
                  weekOfMonth: GetWeekOfMonth(date: e.NewOccuredAt), weekStart: StartOfWeek(dt: e.NewOccuredAt));
          }
      }*/
}