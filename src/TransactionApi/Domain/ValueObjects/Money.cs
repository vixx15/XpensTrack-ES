namespace TransactionApi.Domain.ValueObjects;

public sealed record Money
{
    public decimal Value { get; init; }
    public string CurrencyCode { get; init; }

    public Money(decimal value, string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code is required.", paramName: nameof(CurrencyCode));
        Value = value;
        CurrencyCode = currencyCode;
    }

    public static Money Zero(string currency) => new(0M, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.CurrencyCode != b.CurrencyCode)
            throw new InvalidOperationException("Nemoguće sabrati različite valute!");
        return new Money(a.Value + b.Value, a.CurrencyCode);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.CurrencyCode != b.CurrencyCode)
            throw new InvalidOperationException("Nemoguće oduzimati različite valute!");
        return new Money(a.Value - b.Value, a.CurrencyCode);
    }

    public static Money Max(Money a, Money b)
    {
        if (a.CurrencyCode != b.CurrencyCode)
            throw new InvalidOperationException("Nemoguće porediti novac različitih valuta!");
        return a.Value >= b.Value ? a : b;
    }

    public static Money Min(Money a, Money b)
    {
        if (a.CurrencyCode != b.CurrencyCode)
            throw new InvalidOperationException("Nemoguće porediti novac različitih valuta!");
        return a.Value <= b.Value ? a : b;
    }
}
