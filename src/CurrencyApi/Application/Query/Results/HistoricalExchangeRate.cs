namespace CurrencyApi.Application.Query.Results;

public record HistoricalExchangeRate(
    string FromCurrencyCode,
    string ToCurrencyCode,
    DateTimeOffset Date,
    decimal Rate);
