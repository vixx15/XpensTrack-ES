using Shared;

namespace TransactionApi.Domain.Events;

public sealed record TransactionTypeUpdated(
    Guid TransactionId,
    Money Amount,
    Money DefaultCurrencyAmount,
    TransactionType PreviousTransactionType,
    TransactionType NewTransactionType,
    int? PreviousTransactionCategory,
    int? NewTransactionCategory,
    DateTimeOffset OccuredAt
);