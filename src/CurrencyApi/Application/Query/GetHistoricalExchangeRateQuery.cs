using CurrencyApi.Application.Query.Results;
using MediatR;

namespace CurrencyApi.Application.Query;

public record GetHistoricalExchangeRateQuery(
    string FromCurrency,
    string ToCurrency,
    DateTimeOffset ForDate) : IRequest<HistoricalExchangeRate?>;