using Shared;

namespace TransactionApi.Domain.Events;

public sealed record TransactionCategoryUpdated(
    Guid TransactionId,
    TransactionType TransactionType,
    int PreviousTransactionCategory,
    int NewTransactionCategory,
    Money Amount,
    Money DefaultCurrencyAmount,
    DateTimeOffset OccuredAt
);