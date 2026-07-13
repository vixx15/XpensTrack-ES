using CurrencyApi.Application.Query;
using CurrencyApi.Application.Query.Results;
using CurrencyApi.Infrastructure.Documents;
using Marten;
using MediatR;

namespace CurrencyApi.Application.QueryHandler;

public class GetHistoricalExchangeRateQueryHandler(IQuerySession session)
    : IRequestHandler<GetHistoricalExchangeRateQuery, HistoricalExchangeRate?>
{
    public async Task<HistoricalExchangeRate?> Handle(GetHistoricalExchangeRateQuery request,
        CancellationToken cancellationToken)
    {
        var cutoff = request.ForDate.ToUniversalTime().Date.AddDays(1).AddTicks(-1);

        var from = await FindSnapshotAt(request.FromCurrency, cutoff, cancellationToken);
        if (from == null) return null;

        if (request.ToCurrency == from.BaseCurrencyCode)
            return new HistoricalExchangeRate(from.CurrencyCode, from.BaseCurrencyCode, from.RecordedAt, 1/from.Rate);

        var to = await FindSnapshotAt(request.ToCurrency, cutoff, cancellationToken);
        if (to == null) return null;

        return new HistoricalExchangeRate(
            request.FromCurrency, request.ToCurrency, from.RecordedAt, to.Rate / from.Rate);
    }

    private async Task<RateHistoryEntry?> FindSnapshotAt(
        string currencyCode, DateTimeOffset cutoff, CancellationToken ct)
    {
        var entry = await session.Query<RateHistoryEntry>()
            .Where(h => h.CurrencyCode == currencyCode && h.RecordedAt <= cutoff)
            .OrderByDescending(h => h.RecordedAt)
            .FirstOrDefaultAsync(ct);

        return entry ?? await session.Query<RateHistoryEntry>()
            .Where(h => h.CurrencyCode == currencyCode)
            .OrderBy(h => h.RecordedAt)
            .FirstOrDefaultAsync(ct);
    }
}
