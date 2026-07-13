namespace TransactionApi.Domain.ValueObjects;

public record CurrencyConversion(
    decimal ExchangeRate,
    string FromCurrencyCode,
    string ToCurrencyCode
)
{
    public Money Convert(Money money)
    {
        if (money.CurrencyCode != FromCurrencyCode)
        {
            throw new InvalidOperationException("Currency mismatch.");
        }

        if (ExchangeRate <= 0)
        {
            throw new InvalidOperationException("Exchange rate must be positive.");
        }

        var converted = Math.Round(money.Value * ExchangeRate, 2, MidpointRounding.AwayFromZero);
        return new Money(value: converted, currencyCode: ToCurrencyCode);
    }
}