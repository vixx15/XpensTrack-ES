namespace TransactionApi.Domain.ValueObjects;

public record WalletDetails(
    Guid WalletId,
    CurrencyConversion DefaultCurrencyConversion
);