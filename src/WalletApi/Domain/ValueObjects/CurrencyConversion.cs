using Shared;

namespace WalletApi.Domain.ValueObjects;

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

        return new Money(value: money.Value * ExchangeRate, currencyCode: ToCurrencyCode);
    }
}