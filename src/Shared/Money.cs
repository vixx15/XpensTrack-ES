namespace Shared;

public sealed record Money
{
    public decimal Value { get; init; }
    public string CurrencyCode { get; init; }

    public Money(decimal value, string currencyCode)
    {
     /*   if (value < 0)
        {
            throw new ArgumentException("Money amount cannot be negative.", paramName: nameof(Value));
        }
*/
        if (string.IsNullOrWhiteSpace(value: currencyCode))
        {
            throw new ArgumentException("Currency code is required.", paramName: nameof(CurrencyCode));
        }

        Value = value;
        CurrencyCode = currencyCode;
    }

    public static Money Zero(string currency)
    {
        return new Money(value: 0M, currencyCode: currency);
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.CurrencyCode != b.CurrencyCode)
        {
            throw new InvalidOperationException("Nemoguće sabrati različite valute!");
        }

        return new Money(value: a.Value + b.Value, currencyCode: a.CurrencyCode);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.CurrencyCode != b.CurrencyCode)
        {
            throw new InvalidOperationException("Nemoguće oduzimati različite valute!");
        }

        return new Money(value: a.Value - b.Value, currencyCode: a.CurrencyCode);
    }

    public static Money Max(Money a, Money b)
    {
        if (a.CurrencyCode != b.CurrencyCode)
        {
            throw new InvalidOperationException("Nemoguće porediti novac različitih valuta!");
        }

        return a.Value >= b.Value ? a : b;
    }

    public static Money Min(Money a, Money b)
    {
        if (a.CurrencyCode != b.CurrencyCode)
        {
            throw new InvalidOperationException("Nemoguće porediti novac različitih valuta!");
        }

        return a.Value <= b.Value ? a : b;
    }
}