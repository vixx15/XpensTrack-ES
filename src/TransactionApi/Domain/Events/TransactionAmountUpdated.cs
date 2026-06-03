using Shared;

namespace TransactionApi.Domain.Events;

public sealed record TransactionAmountUpdated(
    Guid TransactionId,
    Money PreviousAmount,
    Money PreviousDefaultCurrencyAmount,
    Money NewAmount,
    Money NewDefaultCurrencyAmount,
    TransactionType TransactionType,
    int? TransactionSubCategory,
    DateTimeOffset OccuredAt
);