using Shared;

namespace TransactionApi.Domain.Events;

public record TransactionUpdated(
    Guid TransactionId,
    Guid OldWalletId,
    string UserId,
    Money OldAmount,
    Money OldDefaultCurrencyAmount,
    TransactionType OldTransactionType,
    int? OldTransactionCategory,
    string OldDescription,
    DateTimeOffset OldOccuredAt,
    Guid? OldToWalletId,
    Guid NewWalletId,
    decimal NewWalletExchangeRate,
    string NewWalletCurrencyCode,
    Money NewAmount,
    Money NewDefaultCurrencyAmount,
    TransactionType NewTransactionType,
    int? NewTransactionCategory,
    string NewDescription,
    DateTimeOffset NewOccurredAt,
    Guid? NewToWalletId,
    Money? NewToWalletAmount,
    decimal? NewToWalletCurrencyExchangeRate,
    string? NewToWalletCurrencyCode
);