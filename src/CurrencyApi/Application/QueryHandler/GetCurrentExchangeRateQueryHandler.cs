using CurrencyApi.Application.Query;
using CurrencyApi.Application.Query.Results;
using CurrencyApi.Infrastructure.Documents;
using Marten;
using MediatR;

namespace CurrencyApi.Application.QueryHandler;

public class GetCurrentExchangeRateQueryHandler(IQuerySession session)
    : IRequestHandler<GetCurrentExchangeRateQuery, CurrentExchangeRate?>
{
    public async Task<CurrentExchangeRate?> Handle(GetCurrentExchangeRateQuery request,
        CancellationToken cancellationToken)
    {
        var from = await session.LoadAsync<RateRecord>(request.FromCurrency, cancellationToken);
        if (from == null) return null;

        if (request.ToCurrency == from.BaseCurrencyCode)
            return new CurrentExchangeRate(from.CurrencyCode, from.BaseCurrencyCode, from.UpdatedAt, 1 / from.Rate);

        var to = await session.LoadAsync<RateRecord>(request.ToCurrency, cancellationToken);
        if (to == null) return null;

        if (to.BaseCurrencyCode != from.BaseCurrencyCode)
            throw new InvalidOperationException("Cannot calculate exchange rate across different base currencies.");

        return new CurrentExchangeRate(request.FromCurrency, request.ToCurrency, from.UpdatedAt, to.Rate / from.Rate);
    }
}
