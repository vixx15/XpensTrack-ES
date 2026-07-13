namespace CurrencyApi.Application.Query.Results;

public record CurrentExchangeRate(
    string FromCurrencyCode,
    string ToCurrencyCode,
    DateTimeOffset Date,
    decimal Rate);
