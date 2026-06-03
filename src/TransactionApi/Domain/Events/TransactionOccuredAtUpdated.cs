using Shared;

namespace TransactionApi.Domain.Events;

public sealed record TransactionOccuredAtUpdated(
    Guid TransactionId,
    TransactionType TransactionType,
    int? TransactionCategory,
    DateTimeOffset PreviousOccuredAt,
    DateTimeOffset NewOccuredAt,
    Money Amount,
    Money DefaultCurrencyAmount
);