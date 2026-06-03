namespace TransactionApi.Domain.Events;

public sealed record TransactionWalletUpdated(
    Guid TransactionId,
    Guid WalletId,
    decimal WalletExchangeRate,
    string WalletCurrencyId
);