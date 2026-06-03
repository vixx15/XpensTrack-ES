namespace TransactionApi.Domain.Events;

public sealed record TransactionWalletToUpdated(
    Guid TransactionId,
    Guid? WalletToId,
    decimal? WalletToExchangeRate,
    string? WalletToCurrencyId
);