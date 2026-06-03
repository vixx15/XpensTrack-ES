using Shared;

namespace TransactionApi.Domain.Events;

public sealed record TransactionCreated(
    Guid TransactionId,
    Guid WalletId,
    string UserId,
    Money Amount,
    TransactionType TransactionType,
    int? TransactionCategory,
    string Description,
    DateTimeOffset OccuredAt,
    Money DefaultCurrencyAmount,
    decimal DefaultCurrencyExchangeRate,
    Guid? ToWalletId,
    Money? ToWalletAmount,
    decimal? ToWalletCurrencyExchangeRate,
    string? ToWalletCurrencyCode
);