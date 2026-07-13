using CurrencyApi.Application.Query.Results;
using MediatR;

namespace CurrencyApi.Application.Query;

public record GetCurrentExchangeRateQuery(
    string FromCurrency,
    string ToCurrency) : IRequest<CurrentExchangeRate?>;