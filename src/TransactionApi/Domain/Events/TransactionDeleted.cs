using Shared;

namespace TransactionApi.Domain.Events;

public sealed record TransactionDeleted(
    Guid TransactionId,
    Guid WalletId,
    string UserId,
    Money Amount,
    TransactionType TransactionType,
    int? TransactionCategory,
    DateTimeOffset OccuredAt,
    Money DefaultCurrencyAmount,
    Guid? ToWalletId,
    Money? ToWalletAmount,
    decimal? ToWalletCurrencyExchangeRate,
    string? ToWalletCurrencyCode);